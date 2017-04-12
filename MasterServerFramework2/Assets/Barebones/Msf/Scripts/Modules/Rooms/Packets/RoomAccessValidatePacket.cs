using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class RoomAccessValidatePacket : SerializablePacket
    {
        public string Token;
        public int RoomId;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Token);
            writer.Write(RoomId);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Token = reader.ReadString();
            RoomId = reader.ReadInt32();
        }
    }
}