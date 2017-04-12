using System;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Base observable value class, which should help out with some things
    /// </summary>
    public abstract class ObservableBase : IObservableProperty
    {
        protected ObservableBase(short key)
        {
            Key = key;
        }

        /// <summary>
        /// Property key
        /// </summary>
        public short Key { get; private set; }

        /// <summary>
        /// Invoked, when value gets dirty
        /// </summary>
        public event Action<IObservableProperty> OnDirty;

        /// <summary>
        /// Sets current observable as dirty
        /// </summary>
        public void MarkDirty()
        {
            if (OnDirty != null)
                OnDirty.Invoke(this);
        }

        /// <summary>
        /// Should serialize the whole value to bytes
        /// </summary>
        public abstract byte[] ToBytes();

        /// <summary>
        /// Should deserialize value from bytes. 
        /// This is not necessarily the whole value. It might be a small update
        /// </summary>
        /// <param name="data"></param>
        public abstract void FromBytes(byte[] data);

        /// <summary>
        /// Should serialize a value to string
        /// </summary>
        public abstract string SerializeToString();

        /// <summary>
        /// Should deserialize a value from string
        /// </summary>
        public abstract void DeserializeFromString(string value);

        /// <summary>
        /// Retrieves updates that happened from the last time
        /// this method was called. 
        /// </summary>
        /// <returns></returns>
        public abstract byte[] GetUpdates();

        /// <summary>
        /// Updates value according to given data
        /// </summary>
        /// <param name="data"></param>
        public abstract void ApplyUpdates(byte[] data);

        /// <summary>
        /// Clears information about accumulated updates.
        /// This is called after property changes are broadcasted to listeners
        /// </summary>
        public abstract void ClearUpdates();
    }
}