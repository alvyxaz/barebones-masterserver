using System;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class PeerSecurityExtension
    {
        public int PermissionLevel;
        public string AesKey;
        public byte[] AesKeyEncrypted;
        public Guid UniqueGuid;

        public PeerSecurityExtension()
        {
            
        }
    }
}