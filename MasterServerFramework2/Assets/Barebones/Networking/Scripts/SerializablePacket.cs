using System.IO;

namespace Barebones.Networking
{
    /// <summary>
    ///     Base class for serializable packets
    /// </summary>
    public abstract class SerializablePacket : ISerializablePacket
    {
        public byte[] ToBytes()
        {
            byte[] b;
            using (var ms = new MemoryStream())
            {
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
                {
                    ToBinaryWriter(writer);
                }

                b = ms.ToArray();
            }
            return b;
        }

        public abstract void ToBinaryWriter(EndianBinaryWriter writer);
        public abstract void FromBinaryReader(EndianBinaryReader reader);

        public static T FromBytes<T>(byte[] data, T packet) where T : ISerializablePacket
        {
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new EndianBinaryReader(EndianBitConverter.Big, ms))
                {
                    packet.FromBinaryReader(reader);
                    return packet;
                }
            }
        }

        /// <summary>
        ///     Write an array whichs length is lower than byte value
        /// </summary>
        /// <param name="data"></param>
        /// <param name="writer"></param>
        public void WriteSmallArray(float[] data, EndianBinaryWriter writer)
        {
            writer.Write((byte) (data != null ? data.Length : 0));

            if (data != null)
                foreach (var val in data)
                    writer.Write(val);
        }

        /// <summary>
        ///     Read an array whichs length is lower than byte value
        /// </summary>
        /// <param name="reader"></param>
        public float[] ReadSmallArray(EndianBinaryReader reader)
        {
            var length = reader.ReadByte();

            // If we have no data
            if (length == 0) return null;

            var result = new float[length];
            for (var i = 0; i < length; i++)
                result[i] = reader.ReadSingle();
            return result;
        }
    }
}