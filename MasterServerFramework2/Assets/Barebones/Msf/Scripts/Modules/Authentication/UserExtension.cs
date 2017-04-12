using Barebones.Networking;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Instance of this class will be added to 
    /// extensions of a peer who has logged in 
    /// </summary>
    public class UserExtension : IUserExtension
    {
        public IPeer Peer { get; private set; }
        public string Username { get { return AccountData.Username; } }

        public UserExtension(IPeer peer)
        {
            Peer = peer;
        }

        public AccountInfoPacket CreateInfoPacket()
        {
            return new AccountInfoPacket()
            {
                Username = AccountData.Username,
                IsAdmin = AccountData.IsAdmin,
                IsGuest = AccountData.IsGuest,
                Properties = AccountData.Properties
            };
        }

        public void Load(IAccountData accountData)
        {
            AccountData = accountData;
        }

        public IAccountData AccountData { get; set; }
    }
}