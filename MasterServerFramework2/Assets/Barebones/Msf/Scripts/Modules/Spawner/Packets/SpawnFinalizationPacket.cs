using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class SpawnFinalizationPacket : SerializablePacket
    {
        public int SpawnId;
        public Dictionary<string, string> FinalizationData;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(SpawnId);
            writer.Write(FinalizationData);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            SpawnId = reader.ReadInt32();
            FinalizationData = reader.ReadDictionary();
        }
    }
}