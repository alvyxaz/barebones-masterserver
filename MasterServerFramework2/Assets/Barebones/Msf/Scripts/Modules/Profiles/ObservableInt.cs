using System;
using Barebones.Networking;
using UnityEngine;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Observable integer
    /// </summary>
    public class ObservableInt : ObservableBase
    {
        private int _value;

        public ObservableInt(short key, int defaultValue = 0) : base(key)
        {
            _value = defaultValue;
        }

        public int Value { get { return _value; } }

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
            _value = EndianBitConverter.Big.ToInt32(data, 0);
        }

        public override string SerializeToString()
        {
            return _value.ToString();
        }

        public override void DeserializeFromString(string value)
        {
            _value = int.Parse(value);
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