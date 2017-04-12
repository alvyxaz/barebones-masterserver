using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class ChatMessagePacket : SerializablePacket
    {
        public const byte Unknown = 0;
        public const byte PrivateMessage = 1;
        public const byte ChannelMessage = 2;

        public byte Type;

        /// <summary>
        /// Represents receiver username if it's a private message,
        /// or channel name, if channel message
        /// </summary>
        public string Receiver;
        public string Sender = "";
        public string Message;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(Receiver);
            writer.Write(Sender);
            writer.Write(Message);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Type = reader.ReadByte();
            Receiver = reader.ReadString();
            Sender = reader.ReadString();
            Message = reader.ReadString();
        }
    }
}