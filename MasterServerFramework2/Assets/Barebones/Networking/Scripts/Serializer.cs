using System;

namespace Barebones.Networking
{
    /// <summary>
    /// Quick, universal serializer
    /// Not sure if it works, but it should :D
    /// </summary>
    public class Serializer : SerializablePacket
    {
        private Action<EndianBinaryWriter> _writeAction;
        private Action<EndianBinaryReader> _readAction;

        public static SerializablePacket Write(Action<EndianBinaryWriter> writeAction)
        {
            return new Serializer()
            {
                _writeAction = writeAction
            };
        }

        public static SerializablePacket Read(Action<EndianBinaryReader> readAction)
        {
            return new Serializer()
            {
                _readAction = readAction
            };
        }

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            _writeAction.Invoke(writer);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            _readAction.Invoke(reader);
        }
    }
}