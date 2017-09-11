using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class ObservableDictionaryInt : ObservableBaseDictionary<int,int>
    {
        public ObservableDictionaryInt(short key) 
            : base(key)
        {
        }

        public ObservableDictionaryInt(short key, Dictionary<int, int> defaultValues) 
            : base(key, defaultValues)
        {
        }

        public override string SerializeToString()
        {
            var obj = new JSONObject();

            foreach (var pair in UnderlyingDictionary)
            {
                obj.AddField(pair.Key.ToString(), pair.Value);
            }

            return obj.ToString();
        }

        public override void DeserializeFromString(string value)
        {
            var parsed = new JSONObject(value);
            var keys = parsed.keys;
            var list = parsed.list;

            if (keys == null)
                return;

            for (var i = 0; i < keys.Count; i++)
            {
                UnderlyingDictionary[int.Parse(keys[i])] = (int)list[i].i;
            }
        }

        protected override void WriteValue(int value, EndianBinaryWriter writer)
        {
            writer.Write(value);
        }

        protected override int ReadValue(EndianBinaryReader reader)
        {
            return reader.ReadInt32();
        }

        protected override void WriteKey(int key, EndianBinaryWriter writer)
        {
            writer.Write(key);
        }

        protected override int ReadKey(EndianBinaryReader reader)
        {
            return reader.ReadInt32();
        }
    }
}