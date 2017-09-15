using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class ObservableDictionary : ObservableBaseDictionary<string,string>
    {
        public ObservableDictionary(short key)
            : base(key)
        {
        }

        public ObservableDictionary(short key, Dictionary<string, string> defaultValues) 
            : base(key, defaultValues)
        {
        }

        public override string SerializeToString()
        {
            var obj = new JSONObject();

            foreach (var pair in UnderlyingDictionary)
            {
                obj.AddField(pair.Key, pair.Value);
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
                UnderlyingDictionary[keys[i]] = list[i].str;
            }
        }

        protected override void WriteValue(string value, EndianBinaryWriter writer)
        {
            writer.Write(value);
        }

        protected override string ReadValue(EndianBinaryReader reader)
        {
            return reader.ReadString();
        }

        protected override void WriteKey(string key, EndianBinaryWriter writer)
        {
            writer.Write(key);
        }

        protected override string ReadKey(EndianBinaryReader reader)
        {
            return reader.ReadString();
        }
    }
}