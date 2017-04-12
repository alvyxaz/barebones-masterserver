using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Data about a specific lobby property
    /// </summary>
    public class LobbyPropertyData : SerializablePacket
    {
        /// <summary>
        /// Type of the property. Should be useful if you're making controls
        /// for different types of inputs (radio buttons, drop downs, text fields and etc.)
        /// </summary>
        public byte Type;

        /// <summary>
        /// Label of the property
        /// </summary>
        public string Label = "";

        /// <summary>
        /// Key of the exact property
        /// </summary>
        public string PropertyKey;

        /// <summary>
        /// List of property options (useful for dropdowns)
        /// </summary>
        public List<string> Options;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(Label);
            writer.Write(PropertyKey);

            writer.Write(Options != null ? Options.Count : 0);

            if (Options != null)
            {
                foreach (var option in Options)
                {
                    writer.Write(option);
                }
            }
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Type = reader.ReadByte();
            Label = reader.ReadString();
            PropertyKey = reader.ReadString();

            var optionsCount = reader.ReadInt32();
            Options = new List<string>();

            for (var i = 0; i < optionsCount; i++)
            {
                Options.Add(reader.ReadString());
            }
        }
    }
}