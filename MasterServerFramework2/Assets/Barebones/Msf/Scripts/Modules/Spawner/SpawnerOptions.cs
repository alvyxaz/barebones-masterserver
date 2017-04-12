using System.Collections.Generic;
using System.Linq;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class SpawnerOptions : SerializablePacket
    {
        /// <summary>
        /// Public IP address of the machine, on which the spawner is running
        /// </summary>
        public string MachineIp = "xxx.xxx.xxx.xxx";

        /// <summary>
        /// Max number of processes that this spawner can handle. If 0 - unlimited
        /// </summary>
        public int MaxProcesses = 0;

        /// <summary>
        /// Spawner properties
        /// </summary>
        public Dictionary<string, string> Properties;

        /// <summary>
        /// Region, to which the spawner belongs
        /// </summary>
        public string Region = "International";

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(MachineIp);
            writer.Write(MaxProcesses);
            writer.Write(Properties);
            writer.Write(Region);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            MachineIp = reader.ReadString();
            MaxProcesses = reader.ReadInt32();
            Properties = reader.ReadDictionary();
            Region = reader.ReadString();
        }

        public override string ToString()
        {
            var properties = "none";

            if (Properties != null && Properties.Count > 0)
                properties = string.Join("; ", Properties.Select(p => p.Key + " : " + p.Value).ToArray());

            properties = "[" + properties + "]";

            return string.Format("PublicIp: {0}, MaxProcesses: {1}, Region: {2}, Properties: {3}",
                MachineIp, MaxProcesses, Region, properties);
        }
    }
}