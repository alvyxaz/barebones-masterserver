using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class SpawnedProcessStartedPacket : SerializablePacket
    {
        public int SpawnId;
        public int ProcessId;
        public string CmdArgs;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(SpawnId);
            writer.Write(ProcessId);
            writer.Write(CmdArgs);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            SpawnId = reader.ReadInt32();
            ProcessId = reader.ReadInt32();
            CmdArgs = reader.ReadString();
        }
    }
}