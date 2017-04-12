using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Lobby factory implementation, which simply invokes
    /// an anonymous method
    /// </summary>
    public class LobbyFactoryAnonymous : ILobbyFactory
    {
        private LobbiesModule _module;
        private readonly LobbyCreationFactory _factory;

        public delegate ILobby LobbyCreationFactory(LobbiesModule module, Dictionary<string, string> properties, IPeer creator);

        public LobbyFactoryAnonymous(string id, LobbiesModule module, LobbyCreationFactory factory)
        {
            Id = id;
            _factory = factory;
            _module = module;
        }

        public ILobby CreateLobby(Dictionary<string, string> properties, IPeer creator)
        {
            var lobby = _factory.Invoke(_module, properties, creator);

            // Add the lobby type if it's not set by the factory method
            if (lobby != null && lobby.Type == null)
                lobby.Type = Id;

            return lobby;
        }

        public string Id { get; private set; }
    }
}


