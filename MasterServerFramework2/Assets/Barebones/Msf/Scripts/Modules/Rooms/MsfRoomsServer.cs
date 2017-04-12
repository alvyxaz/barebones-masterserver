using System;
using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public delegate void RoomCreationCallback(RoomController controller, string error);
    public delegate void RoomAccessValidateCallback(UsernameAndPeerIdPacket usernameAndPeerId, string error);

    public class MsfRoomsServer : MsfBaseClient
    {
        private static Dictionary<int, RoomController> _localCreatedRooms;

        /// <summary>
        /// Maximum time the master server can wait for a response from game server
        /// to see if it can give access to a peer
        /// </summary>
        public float AccessProviderTimeout = 3;

        /// <summary>
        /// Event, invoked when a room is registered
        /// </summary>
        public event Action<RoomController> RoomRegistered; 

        /// <summary>
        /// Event, invoked when a room is destroyed
        /// </summary>
        public event Action<RoomController> RoomDestroyed;

        public MsfRoomsServer(IClientSocket connection) : base(connection)
        {
            _localCreatedRooms = new Dictionary<int, RoomController>();
        }

        /// <summary>
        /// Sends a request to register a room to the master server,
        /// uses default room options <see cref="RoomOptions"/>
        /// </summary>
        /// <param name="callback"></param>
        public void RegisterRoom(RoomCreationCallback callback)
        {
            RegisterRoom(new RoomOptions(), callback);
        }

        /// <summary>
        /// Sends a request to register a room to master server
        /// </summary>
        /// <param name="options"></param>
        /// <param name="callback"></param>
        public void RegisterRoom(RoomOptions options, RoomCreationCallback callback)
        {
            RegisterRoom(options, callback, Connection);
        }

        /// <summary>
        /// Sends a request to register a room to master server
        /// </summary>
        public void RegisterRoom(RoomOptions options, RoomCreationCallback callback, IClientSocket connection)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            Connection.SendMessage((short) MsfOpCodes.RegisterRoom, options, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    // Failed to register room
                    callback.Invoke(null, response.AsString("Unknown Error"));
                    return;
                }

                var roomId = response.AsInt();

                var controller = new RoomController(roomId, Connection, options);

                // Save the reference
                _localCreatedRooms[roomId] = controller;

                callback.Invoke(controller, null);

                // Invoke event
                if (RoomRegistered != null)
                    RoomRegistered.Invoke(controller);
            });
        }

        /// <summary>
        /// Sends a request to destroy a room of a given room id
        /// </summary>
        public void DestroyRoom(int roomId, SuccessCallback callback)
        {
            DestroyRoom(roomId, callback, Connection);
        }

        /// <summary>
        /// Sends a request to destroy a room of a given room id
        /// </summary>
        public void DestroyRoom(int roomId, SuccessCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            connection.SendMessage((short)MsfOpCodes.DestroyRoom, roomId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown Error"));
                    return;
                }

                RoomController destroyedRoom;
                _localCreatedRooms.TryGetValue(roomId, out destroyedRoom);
                _localCreatedRooms.Remove(roomId);
                
                callback.Invoke(true, null);

                // Invoke event
                if (destroyedRoom != null && RoomDestroyed != null)
                    RoomDestroyed.Invoke(destroyedRoom);
            });
        }

        /// <summary>
        /// Sends a request to master server, to see if a given token is valid
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="token"></param>
        /// <param name="callback"></param>
        public void ValidateAccess(int roomId, string token, RoomAccessValidateCallback callback)
        {
            ValidateAccess(roomId, token, callback, Connection);
        }

        /// <summary>
        /// Sends a request to master server, to see if a given token is valid
        /// </summary>
        public void ValidateAccess(int roomId, string token, RoomAccessValidateCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            var packet = new RoomAccessValidatePacket()
            {
                RoomId = roomId,
                Token = token
            };

            connection.SendMessage((short)MsfOpCodes.ValidateRoomAccess, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown Error"));
                    return;
                }

                callback.Invoke(response.Deserialize(new UsernameAndPeerIdPacket()), null);
            });
        }

        /// <summary>
        /// Updates the options of the registered room
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="options"></param>
        /// <param name="callback"></param>
        public void SaveOptions(int roomId, RoomOptions options, SuccessCallback callback)
        {
            SaveOptions(roomId, options, callback, Connection);
        }

        /// <summary>
        /// Updates the options of the registered room
        /// </summary>
        public void SaveOptions(int roomId, RoomOptions options, SuccessCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            var changePacket = new SaveRoomOptionsPacket()
            {
                Options = options,
                RoomId =  roomId
            };

            connection.SendMessage((short) MsfOpCodes.SaveRoomOptions, changePacket, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown Error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        /// <summary>
        /// Notifies master server that a user with a given peer id has left the room
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="peerId"></param>
        /// <param name="callback"></param>
        public void NotifyPlayerLeft(int roomId, int peerId, SuccessCallback callback)
        {
            NotifyPlayerLeft(roomId, peerId, callback, Connection);
        }

        /// <summary>
        /// Notifies master server that a user with a given peer id has left the room
        /// </summary>
        public void NotifyPlayerLeft(int roomId, int peerId, SuccessCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(false, null);
                return;
            }

            var packet = new PlayerLeftRoomPacket()
            {
                PeerId = peerId,
                RoomId = roomId
            };

            connection.SendMessage((short) MsfOpCodes.PlayerLeftRoom, packet, (status, response) =>
            {
                callback.Invoke(status == ResponseStatus.Success, null);
            });
        }

        /// <summary>
        /// Get's a room controller (of a registered room, which was registered in current process)
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public RoomController GetRoomController(int roomId)
        {
            RoomController controller;
            _localCreatedRooms.TryGetValue(roomId, out controller);
            return controller;
        }

        /// <summary>
        /// Retrieves all of the locally created rooms (their controllers)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RoomController> GetLocallyCreatedRooms()
        {
            return _localCreatedRooms.Values;
        }
        
    }
}