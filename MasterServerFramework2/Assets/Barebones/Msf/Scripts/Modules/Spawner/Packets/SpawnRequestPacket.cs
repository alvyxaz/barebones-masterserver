using System;
using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class SpawnRequestPacket : SerializablePacket
    {
        public int SpawnerId;
        public int SpawnId;
        public string SpawnCode = "";
        public string CustomArgs = "";
        public string OverrideExePath = "";
        public Dictionary<string, string> Properties;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(SpawnerId);
            writer.Write(SpawnId);
            writer.Write(SpawnCode);
            writer.Write(CustomArgs);
            writer.Write(OverrideExePath);
            writer.Write(Properties);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            SpawnerId = reader.ReadInt32();
            SpawnId = reader.ReadInt32();
            SpawnCode = reader.ReadString();
            CustomArgs = reader.ReadString();
            OverrideExePath = reader.ReadString();
            Properties = reader.ReadDictionary();
        }
    }
}