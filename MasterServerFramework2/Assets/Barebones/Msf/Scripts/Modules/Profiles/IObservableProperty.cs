using System;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Represents basic functionality of observable property
    /// </summary>
    public interface IObservableProperty
    {
        /// <summary>
        /// Property key
        /// </summary>
        short Key { get; }

        /// <summary>
        /// Invoked, when value gets dirty
        /// </summary>
        event Action<IObservableProperty> OnDirty;

        /// <summary>
        /// Should serialize the whole value to bytes
        /// </summary>
        byte[] ToBytes();

        /// <summary>
        /// Should deserialize value from bytes. 
        /// </summary>
        /// <param name="data"></param>
        void FromBytes(byte[] data);

        /// <summary>
        /// Should serialize a value to string
        /// </summary>
        string SerializeToString();

        /// <summary>
        /// Should deserialize a value from string
        /// </summary>
        void DeserializeFromString(string value);

        /// <summary>
        /// Retrieves updates that happened from the last time
        /// this method was called. If no updates happened - returns null;
        /// </summary>
        byte[] GetUpdates();

        /// <summary>
        /// Updates value according to given data
        /// </summary>
        /// <param name="data"></param>
        void ApplyUpdates(byte[] data);

        /// <summary>
        /// Clears information about accumulated updates.
        /// This is called after property changes are broadcasted to listeners
        /// </summary>
        void ClearUpdates();
    }
}