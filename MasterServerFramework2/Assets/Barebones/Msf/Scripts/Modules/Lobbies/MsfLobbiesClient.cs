using System;
using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class MsfLobbiesClient : MsfBaseClient
    {
        public delegate void JoinLobbyCallback(JoinedLobby lobby, string error);
        public delegate void CreateLobbyCallback(int? lobbyId, string error);

        /// <summary>
        /// Invoked, when user joins a lobby
        /// </summary>
        public event Action<JoinedLobby> LobbyJoined;

        /// <summary>
        /// Key is in format 'lobbyId:connectionPeerId' - this is to allow
        /// mocking multiple clients on the same client and same lobby
        /// </summary>
        private Dictionary<string, JoinedLobby> _joinedLobbies;

        /// <summary>
        /// Instance of a lobby that was joined the last
        /// </summary>
        public JoinedLobby LastJoinedLobby;

        public MsfLobbiesClient(IClientSocket connection) : base(connection)
        {
            _joinedLobbies = new Dictionary<string, JoinedLobby>();
        }

        /// <summary>
        /// Sends a request to create a lobby and joins it
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="properties"></param>
        /// <param name="callback"></param>
        public void CreateAndJoin(string factory, Dictionary<string, string> properties,
            JoinLobbyCallback callback)
        {
            CreateAndJoin(factory, properties, callback, Connection);
        }

        /// <summary>
        /// Sends a request to create a lobby and joins it
        /// </summary>
        public void CreateAndJoin(string factory, Dictionary<string, string> properties, 
            JoinLobbyCallback callback, IClientSocket connection)
        {
            CreateLobby(factory, properties, (id, error) =>
            {
                if (!id.HasValue)
                {
                    callback.Invoke(null, "Failed to create lobby: " + error);
                    return;
                }

                JoinLobby(id.Value, (lobby, joinError) =>
                {
                    if (lobby == null)
                    {
                        callback.Invoke(null, "Failed to join the lobby: " + joinError);
                        return;
                    }

                    callback.Invoke(lobby, null);
                });
            });
        }

        /// <summary>
        /// Sends a request to create a lobby, using a specified factory
        /// </summary>
        public void CreateLobby(string factory, Dictionary<string, string> properties,
            CreateLobbyCallback calback)
        {
            CreateLobby(factory, properties, calback, Connection);
        }

        /// <summary>
        /// Sends a request to create a lobby, using a specified factory
        /// </summary>
        public void CreateLobby(string factory, Dictionary<string, string> properties, 
            CreateLobbyCallback calback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                calback.Invoke(null, "Not connected");   
                return;
            }

            properties[MsfDictKeys.LobbyFactoryId] = factory;

            connection.SendMessage((short) MsfOpCodes.CreateLobby, properties.ToBytes(), (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    calback.Invoke(null, response.AsString("Unknown error"));
                    return;
                }

                var lobbyId = response.AsInt();

                calback.Invoke(lobbyId, null);
            });
        }

        /// <summary>
        /// Sends a request to join a lobby
        /// </summary>
        /// <param name="lobbyId"></param>
        /// <param name="callback"></param>
        public void JoinLobby(int lobbyId, JoinLobbyCallback callback)
        {
            JoinLobby(lobbyId, callback, Connection);
        }

        /// <summary>
        /// Sends a request to join a lobby
        /// </summary>
        public void JoinLobby(int lobbyId, JoinLobbyCallback callback, IClientSocket connection)
        {
            // Send the message
            connection.SendMessage((short) MsfOpCodes.JoinLobby, lobbyId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown Error"));
                    return;
                }

                var data = response.Deserialize(new LobbyDataPacket());

                var key = data.LobbyId + ":" + connection.Peer.Id;

                if (_joinedLobbies.ContainsKey(key))
                {
                    // If there's already a lobby
                    callback.Invoke(_joinedLobbies[key], null);
                    return;
                }

                var joinedLobby = new JoinedLobby(data, connection);

                LastJoinedLobby = joinedLobby;

                // Save the lobby
                _joinedLobbies[key] = joinedLobby;

                callback.Invoke(joinedLobby, null);

                if (LobbyJoined != null)
                    LobbyJoined.Invoke(joinedLobby);
            });
        }

        /// <summary>
        /// Sends a request to leave a lobby
        /// </summary>
        public void LeaveLobby(int lobbyId)
        {
            LeaveLobby(lobbyId, () => { }, Connection);
        }

        /// <summary>
        /// Sends a request to leave a lobby
        /// </summary>
        public void LeaveLobby(int lobbyId, Action callback)
        {
            LeaveLobby(lobbyId, callback, Connection);
        }

        /// <summary>
        /// Sends a request to leave a lobby
        /// </summary>
        public void LeaveLobby(int lobbyId, Action callback, IClientSocket connection)
        {
            connection.SendMessage((short)MsfOpCodes.LeaveLobby, lobbyId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                    Logs.Error(response.AsString("Something went wrong when trying to leave a lobby"));

                callback.Invoke();
            });
        }

        /// <summary>
        /// Sets a ready status of current player
        /// </summary>
        /// <param name="isReady"></param>
        /// <param name="callback"></param>
        public void SetReadyStatus(bool isReady, SuccessCallback callback)
        {
            SetReadyStatus(isReady, callback, Connection);
        }

        /// <summary>
        /// Sets a ready status of current player
        /// </summary>
        public void SetReadyStatus(bool isReady, SuccessCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            connection.SendMessage((short) MsfOpCodes.LobbySetReady, isReady ? 1 : 0, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        /// <summary>
        /// Sets lobby properties of a specified lobby id
        /// </summary>
        /// <param name="lobbyId"></param>
        /// <param name="properties"></param>
        /// <param name="callback"></param>
        public void SetLobbyProperties(int lobbyId, Dictionary<string, string> properties,
            SuccessCallback callback)
        {
            SetLobbyProperties(lobbyId, properties, callback, Connection);
        }

        /// <summary>
        /// Sets lobby properties of a specified lobby id
        /// </summary>
        public void SetLobbyProperties(int lobbyId, Dictionary<string, string> properties,
            SuccessCallback callback, IClientSocket connection)
        {
            var packet = new LobbyPropertiesSetPacket()
            {
                LobbyId = lobbyId,
                Properties = properties
            };

            connection.SendMessage((short) MsfOpCodes.SetLobbyProperties, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        /// <summary>
        /// Set's lobby user properties (current player sets his own properties,
        ///  which can be accessed by game server and etc.)
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="callback"></param>
        public void SetMyProperties(Dictionary<string, string> properties,
            SuccessCallback callback)
        {
            SetMyProperties(properties, callback, Connection);
        }

        /// <summary>
        /// Set's lobby user properties (current player sets his own properties,
        ///  which can be accessed by game server and etc.)
        /// </summary>
        public void SetMyProperties(Dictionary<string, string> properties,
            SuccessCallback callback, IClientSocket connection)
        {
            connection.SendMessage((short)MsfOpCodes.SetMyLobbyProperties, properties.ToBytes(), 
                Msf.Create.SuccessCallback(callback));
        }

        /// <summary>
        /// Current player sends a request to join a team
        /// </summary>
        /// <param name="lobbyId"></param>
        /// <param name="teamName"></param>
        /// <param name="callback"></param>
        public void JoinTeam(int lobbyId, string teamName, SuccessCallback callback)
        {
            JoinTeam(lobbyId, teamName, callback, Connection);
        }

        /// <summary>
        /// Current player sends a request to join a team
        /// </summary>
        public void JoinTeam(int lobbyId, string teamName, SuccessCallback callback, IClientSocket connection)
        {
            var packet = new LobbyJoinTeamPacket()
            {
                LobbyId = lobbyId,
                TeamName = teamName
            };

            connection.SendMessage((short)MsfOpCodes.JoinLobbyTeam, packet,
                Msf.Create.SuccessCallback(callback));
        }

        /// <summary>
        /// Current player sends a chat message to lobby
        /// </summary>
        public void SendChatMessage(string message)
        {
            SendChatMessage(message, Connection);
        }

        /// <summary>
        /// Current player sends a chat message to lobby
        /// </summary>
        public void SendChatMessage(string message, IClientSocket connection)
        {
            connection.SendMessage((short) MsfOpCodes.LobbySendChatMessage, message);
        }

        /// <summary>
        /// Sends a request to start a game
        /// </summary>
        public void StartGame(SuccessCallback callback, IClientSocket connection)
        {
            connection.SendMessage((short) MsfOpCodes.LobbyStartGame, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        /// <summary>
        /// Sends a request to get access to room, which is assigned to this lobby
        /// </summary>
        public void GetLobbyRoomAccess(Dictionary<string, string> properties, RoomAccessCallback callback)
        {
            GetLobbyRoomAccess(properties, callback, Connection);
        }

        /// <summary>
        /// Sends a request to get access to room, which is assigned to this lobby
        /// </summary>
        public void GetLobbyRoomAccess(Dictionary<string, string> properties, RoomAccessCallback callback,
            IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            connection.SendMessage((short)MsfOpCodes.GetLobbyRoomAccess, properties.ToBytes(), (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown Error"));
                    return;
                }

                var access = response.Deserialize(new RoomAccessPacket());

                Msf.Client.Rooms.TriggerAccessReceivedEvent(access);

                callback.Invoke(access, null);
            });
        }
    }
}