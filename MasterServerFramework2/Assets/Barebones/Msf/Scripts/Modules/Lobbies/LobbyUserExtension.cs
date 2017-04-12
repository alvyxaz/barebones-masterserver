using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class LobbyUserExtension
    {
        public IPeer Peer { get; private set; }

        /// <summary>
        /// Lobby, to which current peer belongs
        /// </summary>
        public ILobby CurrentLobby { get; set; }

        public LobbyUserExtension(IPeer peer)
        {
            this.Peer = peer;
        }
    }
}