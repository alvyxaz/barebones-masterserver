using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class SpawnStatusUpdatePacket : SerializablePacket
    {
        public int SpawnId;
        public SpawnStatus Status;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(SpawnId);
            writer.Write((int) Status);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            SpawnId = reader.ReadInt32();
            Status = (SpawnStatus)reader.ReadInt32();
        }
    }
}