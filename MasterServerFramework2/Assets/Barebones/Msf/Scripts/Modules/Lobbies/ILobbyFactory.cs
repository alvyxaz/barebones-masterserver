using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public interface ILobbyFactory
    {
        string Id { get; }

        ILobby CreateLobby(Dictionary<string, string> properties, IPeer creator);
    }
}