using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class ProfileExtension
    {
        public string Username { get; private set; }
        public ObservableServerProfile Profile { get; private set; }
        public IPeer Peer { get; private set; }

        public ProfileExtension(ObservableServerProfile profile, IPeer peer)
        {
            Username = profile.Username;
            Profile = profile;
            Peer = peer;
        }
    }
}