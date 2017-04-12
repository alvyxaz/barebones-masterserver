using System;
using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public interface ILobby
    {
        int Id { get; }
        string Type { get; set; }

        string GameIp { get; }
        int GamePort { get; }
        int MaxPlayers { get; }
        string Name { get; set; }
        int PlayerCount { get; }
        Dictionary<string, string> GetPublicProperties(IPeer peer);

        event Action<ILobby> Destroyed;
        bool AddPlayer(LobbyUserExtension playerExt, out string error);
        void RemovePlayer(LobbyUserExtension playerExt);

        bool SetProperty(LobbyUserExtension setter, string key, string value);
        bool SetProperty(string key, string value);

        LobbyMember GetMember(LobbyUserExtension playerExt);
        LobbyMember GetMember(string username);
        LobbyMember GetMemberByPeerId(int peerId);

        void SetReadyState(LobbyMember member, bool state);

        bool SetPlayerProperty(LobbyMember player, string key, string value);
        bool TryJoinTeam(string teamName, LobbyMember player);

        LobbyDataPacket GenerateLobbyData(LobbyUserExtension user);
        LobbyDataPacket GenerateLobbyData();
        bool StartGameManually(LobbyUserExtension user);

        void HandleChatMessage(LobbyMember member, IIncommingMessage message);
        void HandleGameAccessRequest(IIncommingMessage message);
    }
}