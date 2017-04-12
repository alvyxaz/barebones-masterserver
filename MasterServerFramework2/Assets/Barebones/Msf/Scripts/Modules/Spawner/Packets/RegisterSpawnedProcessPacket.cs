using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class RegisterSpawnedProcessPacket : SerializablePacket
    {
        public int SpawnId;
        public string SpawnCode;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(SpawnId);
            writer.Write(SpawnCode);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            SpawnId = reader.ReadInt32();
            SpawnCode = reader.ReadString();
        }
    }
}