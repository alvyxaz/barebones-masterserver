using Barebones.Networking;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Observable integer
    /// </summary>
    public class ObservableFloat : ObservableBase
    {
        private float _value;

        public ObservableFloat(short key, int defaultValue = 0) : base(key)
        {
            _value = defaultValue;
        }

        public float Value { get { return _value; } }

        public void Add(int val)
        {
            _value += val;
            MarkDirty();
        }

        public void Set(int val)
        {
            _value = val;
            MarkDirty();
        }

        public bool TryTake(int amount)
        {
            if (_value >= amount)
            {
                Add(-amount);
                return true;
            }
            return false;
        }

        public override byte[] ToBytes()
        {
            var data = new byte[4];
            EndianBitConverter.Big.CopyBytes(_value, data, 0);

            return data;
        }

        public override void FromBytes(byte[] data)
        {
            _value = EndianBitConverter.Big.ToSingle(data, 0);

            MarkDirty();
        }

        public override string SerializeToString()
        {
            return _value.ToString();
        }

        public override void DeserializeFromString(string value)
        {
            _value = float.Parse(value);
        }

        public override byte[] GetUpdates()
        {
            return ToBytes();
        }

        public override void ApplyUpdates(byte[] data)
        {
            FromBytes(data);
            MarkDirty();
        }

        public override void ClearUpdates()
        {
            
        }
    }
}