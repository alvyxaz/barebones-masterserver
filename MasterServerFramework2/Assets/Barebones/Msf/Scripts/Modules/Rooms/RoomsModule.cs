using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;

namespace Barebones.MasterServer
{
    public class RoomsModule : ServerModuleBehaviour, IGamesProvider
    {
        #region Unity Inspector

        [Header("Permissions")]
        [Tooltip("Minimal permission level, necessary to register a room")]
        public int RegisterRoomPermissionLevel = 0;

        #endregion

        protected Dictionary<int, RegisteredRoom> Rooms;

        private int _roomIdGenerator = 0;

        public event Action<RegisteredRoom> RoomRegistered; 
        public event Action<RegisteredRoom> RoomDestroyed;

        protected virtual void Awake()
        {
            Rooms = new Dictionary<int, RegisteredRoom>();
        }

        public override void Initialize(IServer server)
        {
            base.Initialize(server);

            // Add handlers
            server.SetHandler((short)MsfOpCodes.RegisterRoom, HandleRegisterRoom);
            server.SetHandler((short)MsfOpCodes.DestroyRoom, HandleDestroyRoom);
            server.SetHandler((short)MsfOpCodes.SaveRoomOptions, HandleSaveRoomOptions);
            server.SetHandler((short)MsfOpCodes.GetRoomAccess, HandleGetRoomAccess);
            server.SetHandler((short)MsfOpCodes.ValidateRoomAccess, HandleValidateRoomAccess);
            server.SetHandler((short)MsfOpCodes.PlayerLeftRoom, HandlePlayerLeftRoom);

            // Maintain unconfirmed accesses
            StartCoroutine(CleanUnconfirmedAccesses());
        }

        /// <summary>
        /// Returns true, if peer has permissions to register a game server
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        protected virtual bool HasRoomRegistrationPermissions(IPeer peer)
        {
            var extension = peer.GetExtension<PeerSecurityExtension>();

            return extension.PermissionLevel >= RegisterRoomPermissionLevel;
        }

        public int GenerateRoomId()
        {
            return _roomIdGenerator++;
        }

        /// <summary>
        /// Registers a room to the server
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public virtual RegisteredRoom RegisterRoom(IPeer peer, RoomOptions options)
        {
            // Create the object
            var room = new RegisteredRoom(GenerateRoomId(), peer, options);

            var peerRooms = peer.GetProperty((int) MsfPropCodes.RegisteredRooms) as Dictionary<int, RegisteredRoom>;

            if (peerRooms == null)
            {
                // If this is the first time creating a room

                // Save the dictionary
                peerRooms = new Dictionary<int, RegisteredRoom>();
                peer.SetProperty((int)MsfPropCodes.RegisteredRooms, peerRooms);

                // Listen to disconnect event
                peer.Disconnected += OnRegisteredPeerDisconnect;
            }

            // Add a new room to peer
            peerRooms[room.RoomId] = room;

            // Add the room to a list of all rooms
            Rooms[room.RoomId] = room;

            // Invoke the event
            if (RoomRegistered != null)
                RoomRegistered.Invoke(room);

            return room;
        }

        /// <summary>
        /// Unregisters a room from a server
        /// </summary>
        /// <param name="room"></param>
        public virtual void DestroyRoom(RegisteredRoom room)
        {
            var peer = room.Peer;

            if (peer != null)
            {
                var peerRooms = peer.GetProperty((int)MsfPropCodes.RegisteredRooms) as Dictionary<int, RegisteredRoom>;

                // Remove the room from peer
                if (peerRooms != null)
                    peerRooms.Remove(room.RoomId);
            }

            // Remove the room from all rooms
            Rooms.Remove(room.RoomId);

            room.Destroy();

            // Invoke the event
            if (RoomDestroyed != null)
                RoomDestroyed.Invoke(room);
        }

        private void OnRegisteredPeerDisconnect(IPeer peer)
        {
            var peerRooms = peer.GetProperty((int)MsfPropCodes.RegisteredRooms) as Dictionary<int, RegisteredRoom>;

            if (peerRooms == null)
                return;

            // Create a copy so that we can iterate safely
            var registeredRooms = peerRooms.Values.ToList();

            foreach (var registeredRoom in registeredRooms)
            {
                DestroyRoom(registeredRoom);
            }
        }

        public virtual void ChangeRoomOptions(RegisteredRoom room, RoomOptions options)
        {
            room.ChangeOptions(options);
        }

        private IEnumerator CleanUnconfirmedAccesses()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                foreach (var registeredRoom in Rooms.Values)
                {
                    registeredRoom.ClearTimedOutAccesses();
                }
            }
        }

        public IEnumerable<GameInfoPacket> GetPublicGames(IPeer peer, Dictionary<string, string> filters)
        {
            return Rooms.Values.Where(r => r.Options.IsPublic).Select(r => new GameInfoPacket()
            {
                Id = r.RoomId,
                Address = r.Options.RoomIp + ":" + r.Options.RoomPort,
                MaxPlayers = r.Options.MaxPlayers,
                Name = r.Options.Name,
                OnlinePlayers = r.OnlineCount,
                Properties = GetPublicRoomProperties(peer, r, filters),
                IsPasswordProtected = !string.IsNullOrEmpty(r.Options.Password),
                Type = GameInfoType.Room
            });
        }

        public virtual Dictionary<string, string> GetPublicRoomProperties(IPeer player, RegisteredRoom room, 
            Dictionary<string, string> playerFilters)
        {
            return room.Options.Properties;
        }

        public RegisteredRoom GetRoom(int roomId)
        {
            RegisteredRoom room;
            Rooms.TryGetValue(roomId, out room);
            return room;
        }

        public IEnumerable<RegisteredRoom> GetAllRooms()
        {
            return Rooms.Values;
        }

        #region Message Handlers

        private void HandleRegisterRoom(IIncommingMessage message)
        {
            if (!HasRoomRegistrationPermissions(message.Peer))
            {
                message.Respond("Insufficient permissions", ResponseStatus.Unauthorized);
                return;
            }

            var options = message.Deserialize(new RoomOptions());

            var room = RegisterRoom(message.Peer, options);

            // Respond with a room id
            message.Respond(room.RoomId, ResponseStatus.Success);
        }

        protected virtual void HandleDestroyRoom(IIncommingMessage message)
        {
            var roomId = message.AsInt();

            RegisteredRoom room;
            Rooms.TryGetValue(roomId, out room);

            if (room == null)
            {
                message.Respond("Room does not exist", ResponseStatus.Failed);
                return;
            }

            if (message.Peer != room.Peer)
            {
                // Wrong peer unregistering the room
                message.Respond("You're not the creator of the room", ResponseStatus.Unauthorized);
                return;
            }

            DestroyRoom(room);

            message.Respond(ResponseStatus.Success);
        }

        private void HandleValidateRoomAccess(IIncommingMessage message)
        {
            var data = message.Deserialize(new RoomAccessValidatePacket());

            RegisteredRoom room;
            Rooms.TryGetValue(data.RoomId, out room);

            if (room == null)
            {
                message.Respond("Room does not exist", ResponseStatus.Failed);
                return;
            }

            if (message.Peer != room.Peer)
            {
                // Wrong peer unregistering the room
                message.Respond("You're not the creator of the room", ResponseStatus.Unauthorized);
                return;
            }

            IPeer playerPeer;
            if (!room.ValidateAccess(data.Token, out playerPeer))
            {
                message.Respond("Failed to confirm the access", ResponseStatus.Unauthorized);
                return;
            }

            var packet = new UsernameAndPeerIdPacket()
            {
                PeerId =  playerPeer.Id
            };

            // Add username if available
            var userExt = playerPeer.GetExtension<IUserExtension>();
            if (userExt != null)
            {
                packet.Username = userExt.Username ?? "";
            }

            // Respond with success and player's peer id
            message.Respond(packet, ResponseStatus.Success);
        }

        protected virtual void HandleSaveRoomOptions(IIncommingMessage message)
        {
            var data = message.Deserialize(new SaveRoomOptionsPacket());

            RegisteredRoom room;
            Rooms.TryGetValue(data.RoomId, out room);

            if (room == null)
            {
                message.Respond("Room does not exist", ResponseStatus.Failed);
                return;
            }

            if (message.Peer != room.Peer)
            {
                // Wrong peer unregistering the room
                message.Respond("You're not the creator of the room", ResponseStatus.Unauthorized);
                return;
            }

            ChangeRoomOptions(room, data.Options);

            message.Respond(ResponseStatus.Success);
        }

        protected virtual void HandleGetRoomAccess(IIncommingMessage message)
        {
            var data = message.Deserialize(new RoomAccessRequestPacket());

            RegisteredRoom room;
            Rooms.TryGetValue(data.RoomId, out room);

            if (room == null)
            {
                message.Respond("Room does not exist", ResponseStatus.Failed);
                return;
            }

            if (!string.IsNullOrEmpty(room.Options.Password) && room.Options.Password != data.Password)
            {
                message.Respond("Invalid password", ResponseStatus.Unauthorized);
                return;
            }

            // Send room access request to peer who owns it
            room.GetAccess(message.Peer, data.Properties, (packet, error) =>
            {
                if (packet == null)
                {
                    message.Respond(error, ResponseStatus.Unauthorized);
                    return;
                }

                message.Respond(packet, ResponseStatus.Success);
            });
        }

        private void HandlePlayerLeftRoom(IIncommingMessage message)
        {
            var data = message.Deserialize(new PlayerLeftRoomPacket());

            RegisteredRoom room;
            Rooms.TryGetValue(data.RoomId, out room);

            if (room == null)
            {
                message.Respond("Room does not exist", ResponseStatus.Failed);
                return;
            }

            if (message.Peer != room.Peer)
            {
                // Wrong peer unregistering the room
                message.Respond("You're not the creator of the room", ResponseStatus.Unauthorized);
                return;
            }

            room.OnPlayerLeft(data.PeerId);

            message.Respond(ResponseStatus.Success);
        }

        #endregion

        
    }
}


