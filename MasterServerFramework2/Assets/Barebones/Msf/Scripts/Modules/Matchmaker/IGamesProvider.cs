using System.Collections;
using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public interface IGamesProvider
    {
        IEnumerable<GameInfoPacket> GetPublicGames(IPeer peer, Dictionary<string, string> filters);
    }
}