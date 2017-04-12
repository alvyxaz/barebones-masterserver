using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Barebones.Networking
{
    /// <summary>
    ///     Contains functions to help easily serialize / deserialize some common types
    /// </summary>
    public static class SerializationExtensions
    {
        public static byte[] ToBytes(this IEnumerable<string> list)
        {
            byte[] b;
            using (var ms = new MemoryStream())
            {
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
                {
                    writer.Write(list.Count());

                    foreach (var item in list)
                        writer.Write(item);
                }

                b = ms.ToArray();
            }
            return b;
        }

        public static List<string> FromBytes(this List<string> list, byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new EndianBinaryReader(EndianBitConverter.Big, ms))
                {
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; i++)
                    {
                        list.Add(reader.ReadString());
                    }
                }
            }

            return list;
        }

        public static byte[] ToBytes(this IEnumerable<ISerializablePacket> list)
        {
            byte[] b;
            using (var ms = new MemoryStream())
            {
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
                {
                    writer.Write(list.Count());

                    foreach (var item in list)
                        item.ToBinaryWriter(writer);
                }

                b = ms.ToArray();
            }
            return b;
        }

        public static List<ISerializablePacket> FromBytes(this List<ISerializablePacket> list, byte[] data, Func<ISerializablePacket> factory)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new EndianBinaryReader(EndianBitConverter.Big, ms))
                {
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; i++)
                    {
                        var item = factory.Invoke();
                        item.FromBinaryReader(reader);
                        list.Add(item);
                    }
                }
            }
            return list;
        }

        public static byte[] ToBytes(this Dictionary<int, int> dictionary)
        {
            byte[] b;
            using (var ms = new MemoryStream())
            {
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
                {
                    writer.Write(dictionary.Count);

                    foreach (var item in dictionary)
                    {
                        writer.Write(item.Key);
                        writer.Write(item.Value);
                    }
                }

                b = ms.ToArray();
            }
            return b;
        }

        public static Dictionary<int, int> FromBytes(this Dictionary<int, int> dictionary, byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new EndianBinaryReader(EndianBitConverter.Big, ms))
                {
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; i++)
                    {
                        var key = reader.ReadInt32();
                        var value = reader.ReadInt32();

                        if (dictionary.ContainsKey(key))
                        {
                            dictionary[key] = value;
                        }
                        else
                        {
                            dictionary.Add(key, value);
                        }
                    }
                }
            }
            return dictionary;
        }

        public static byte[] ToBytes(this Dictionary<string, int> dictionary)
        {
            byte[] b;
            using (var ms = new MemoryStream())
            {
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
                {
                    writer.Write(dictionary.Count);

                    foreach (var item in dictionary)
                    {
                        writer.Write(item.Key);
                        writer.Write(item.Value);
                    }
                }

                b = ms.ToArray();
            }
            return b;
        }

        public static Dictionary<string, int> FromBytes(this Dictionary<string, int> dictionary, byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new EndianBinaryReader(EndianBitConverter.Big, ms))
                {
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; i++)
                    {
                        var key = reader.ReadString();
                        var value = reader.ReadInt32();

                        if (dictionary.ContainsKey(key))
                        {
                            dictionary[key] = value;
                        }
                        else
                        {
                            dictionary.Add(key, value);
                        }
                    }
                }
            }
            return dictionary;
        }

        public static byte[] ToBytes(this Dictionary<string, float> dictionary)
        {
            byte[] b;
            using (var ms = new MemoryStream())
            {
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
                {
                    writer.Write(dictionary.Count);

                    foreach (var item in dictionary)
                    {
                        writer.Write(item.Key);
                        writer.Write(item.Value);
                    }
                }

                b = ms.ToArray();
            }
            return b;
        }

        public static Dictionary<string, float> FromBytes(this Dictionary<string, float> dictionary, byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new EndianBinaryReader(EndianBitConverter.Big, ms))
                {
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; i++)
                    {
                        var key = reader.ReadString();
                        var value = reader.ReadSingle();

                        if (dictionary.ContainsKey(key))
                        {
                            dictionary[key] = value;
                        }
                        else
                        {
                            dictionary.Add(key, value);
                        }
                    }
                }
            }
            return dictionary;
        }

        public static byte[] ToBytes(this Dictionary<string, string> dictionary)
        {
            byte[] b;
            using (var ms = new MemoryStream())
            {
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
                {
                    dictionary.ToWriter(writer);
                }

                b = ms.ToArray();
            }
            return b;
        }

        public static void ToWriter(this Dictionary<string, string> dictionary, EndianBinaryWriter writer)
        {
            if (dictionary == null)
            {
                writer.Write(0);
                return;
            }

            writer.Write(dictionary.Count);

            foreach (var item in dictionary)
            {
                writer.Write(item.Key);
                writer.Write(item.Value);
            }
        }

        public static Dictionary<string, string> FromReader(this Dictionary<string, string> dictionary,
            EndianBinaryReader reader)
        {
            var count = reader.ReadInt32();

            for (var i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var value = reader.ReadString();
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key] = value;
                }
                else
                {
                    dictionary.Add(key, value);
                }
            }

            return dictionary;
        }

        public static Dictionary<string, string> FromBytes(this Dictionary<string, string> dictionary, byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new EndianBinaryReader(EndianBitConverter.Big, ms))
                {
                    dictionary.FromReader(reader);
                }
            }
            return dictionary;
        }

        public static byte[] ToBytes(this string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        public static void Write(this EndianBinaryWriter writer, ISerializablePacket packet)
        {
            packet.ToBinaryWriter(writer);
            //var bytes = packet != null ? packet.ToBytes() : new byte[0];
            //writer.Write(bytes.Length);
            //writer.Write(bytes);
        }

        public static T ReadPacket<T>(this EndianBinaryReader reader, T packet) where T: ISerializablePacket, new()
        {
            packet.FromBinaryReader(reader);

            //var length = reader.ReadInt32();

            //if (length > 0)
            //    return packet;

            //packet.FromBinaryReader(reader);

            return packet;
        }

        public static void Write(this EndianBinaryWriter writer, Dictionary<string, string> dictionary)
        {
            WriteDictionary(writer, dictionary);
        }

        public static void WriteDictionary(this EndianBinaryWriter writer, Dictionary<string, string> dictionary)
        {
            var bytes = dictionary != null ? dictionary.ToBytes() : new byte[0];
            writer.Write(bytes.Length);

            writer.Write(bytes);
        }

        public static Dictionary<string, string> ReadDictionary(this EndianBinaryReader reader)
        {
            // Additional dictionary
            var length = reader.ReadInt32();

            if (length > 0)
                return new Dictionary<string, string>()
                    .FromBytes(reader.ReadBytes(length));

            return new Dictionary<string, string>();
        }

        public static string ToReadableString(this Dictionary<string, string> dictionary)
        {
            var readableString = "none";

            if (dictionary != null && dictionary.Count > 0)
                readableString = string.Join("; ", dictionary.Select(p => p.Key + " : " + p.Value).ToArray());

            readableString = "[" + readableString + "]";
            return readableString;
        }
    }
}