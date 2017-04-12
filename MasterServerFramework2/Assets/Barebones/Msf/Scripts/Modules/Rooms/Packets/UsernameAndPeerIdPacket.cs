using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class UsernameAndPeerIdPacket : SerializablePacket
    {
        public string Username = "";
        public int PeerId;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Username);
            writer.Write(PeerId);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Username = reader.ReadString();
            PeerId = reader.ReadInt32();
        }

        public override string ToString()
        {
            return string.Format("[Username: {0}, Peer ID: {1}]", Username, PeerId);
        }
    }
}