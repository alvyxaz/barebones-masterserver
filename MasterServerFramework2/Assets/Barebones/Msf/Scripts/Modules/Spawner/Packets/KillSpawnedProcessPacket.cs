using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class KillSpawnedProcessPacket : SerializablePacket
    {
        public int SpawnerId;
        public int SpawnId;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(SpawnerId);
            writer.Write(SpawnId);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            SpawnerId = reader.ReadInt32();
            SpawnId = reader.ReadInt32();
        }
    }
}