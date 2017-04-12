using Barebones.Networking;

namespace Barebones.MasterServer
{
    /// <summary>
    /// This is an interface of a user extension.
    /// Implementation of this interface will be stored in peer's extensions
    /// after he logs in
    /// </summary>
    public interface IUserExtension
    {
        IPeer Peer { get; }

        string Username { get; }

        AccountInfoPacket CreateInfoPacket();

        void Load(IAccountData accountData);

        IAccountData AccountData { get; set; }
    }
}