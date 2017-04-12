using System;
using System.Text;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Observable property of type string
    /// </summary>
    public class ObservableString : ObservableBase
    {
        private string _value;

        public ObservableString(short key, string defaultVal = "") : base(key)
        {
            _value = defaultVal;
        }

        public string Value { get { return _value; } }

        public void Set(string value)
        {
            _value = value;
            MarkDirty();
        }

        public override byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(_value);
        }

        public override void FromBytes(byte[] data)
        {
            _value = Encoding.UTF8.GetString(data);
            MarkDirty();
        }

        public override string SerializeToString()
        {
            return _value;
        }

        public override void DeserializeFromString(string value)
        {
            _value = value;
        }

        public override byte[] GetUpdates()
        {
            return ToBytes();
        }

        public override void ApplyUpdates(byte[] data)
        {
            FromBytes(data);
        }

        public override void ClearUpdates()
        {
            
        }
    }
}