using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class PeerAccountInfoPacket : SerializablePacket
    {
        public int PeerId;
        public string Username;
        public Dictionary<string, string> Properties;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(PeerId);
            writer.Write(Username);
            writer.Write(Properties);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            PeerId = reader.ReadInt32();
            Username = reader.ReadString();
            Properties = reader.ReadDictionary();
        }

        public override string ToString()
        {
            return string.Format("[Peer account info: Peer ID: {0}, Username: {1}, Properties: {2}]", PeerId, Username,
                Properties.ToReadableString());
        }
    }
}