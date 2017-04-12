using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class AccountInfoPacket : SerializablePacket
    {
        public string Username;
        public bool IsAdmin;
        public bool IsGuest;
        public Dictionary<string, string> Properties;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Username);
            writer.Write(IsAdmin);
            writer.Write(IsGuest);
            writer.Write(Properties);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Username = reader.ReadString();
            IsAdmin = reader.ReadBoolean();
            IsGuest = reader.ReadBoolean();
            Properties = reader.ReadDictionary();
        }
    }
}