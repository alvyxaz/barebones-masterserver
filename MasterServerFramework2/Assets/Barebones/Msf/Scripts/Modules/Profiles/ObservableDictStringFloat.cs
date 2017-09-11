using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class ObservableDictStringFloat : ObservableBaseDictionary<string,float>
    {
        public ObservableDictStringFloat(short key) 
            : base(key)
        {
        }

        public ObservableDictStringFloat(short key, Dictionary<string, float> defaultValues) 
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
                UnderlyingDictionary[keys[i]] = list[i].f;
            }
        }

        protected override void WriteValue(float value, EndianBinaryWriter writer)
        {
            writer.Write(value);
        }

        protected override float ReadValue(EndianBinaryReader reader)
        {
            return reader.ReadSingle();
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