using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Networking;

namespace Barebones.Networking
{
    /// <summary>
    ///     Default implementation of incomming message
    /// </summary>
    public class IncommingMessage : IIncommingMessage
    {
        private readonly byte[] _data;

        public IncommingMessage(short opCode, byte flags, byte[] data, DeliveryMethod deliveryMethod, IPeer peer)
        {
            OpCode = opCode;
            Peer = peer;
            _data = data;
        
        }

        /// <summary>
        ///     Message flags
        /// </summary>
        public byte Flags { get; private set; }

        /// <summary>
        ///     Operation code (message type)
        /// </summary>
        public short OpCode { get; private set; }

        /// <summary>
        ///     Sender
        /// </summary>
        public IPeer Peer { get; private set; }

        /// <summary>
        ///     Ack id the message is responding to
        /// </summary>
        public int? AckResponseId { get; set; }

        /// <summary>
        ///     We add this to a packet to so that receiver knows
        ///     what he responds to
        /// </summary>
        public int? AckRequestId { get; set; }

        /// <summary>
        ///     Returns true, if sender expects a response to this message
        /// </summary>
        public bool IsExpectingResponse
        {
            get { return AckResponseId.HasValue; }
        }

        /// <summary>
        ///     For ordering
        /// </summary>
        public int SequenceChannel { get; set; }

        /// <summary>
        ///     Message status code
        /// </summary>
        public ResponseStatus Status { get; set; }

        /// <summary>
        ///     Respond with a message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        public void Respond(IMessage message, ResponseStatus statusCode = ResponseStatus.Default)
        {
            message.Status = statusCode;

            if (AckResponseId.HasValue)
                message.AckResponseId = AckResponseId.Value;

            Peer.SendMessage(message, DeliveryMethod.Reliable);
        }

        /// <summary>
        ///     Respond with data (message is created internally)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="statusCode"></param>
        public void Respond(byte[] data, ResponseStatus statusCode = ResponseStatus.Default)
        {
            Respond(MessageHelper.Create(OpCode, data), statusCode);
        }

        /// <summary>
        ///     Respond with data (message is created internally)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="statusCode"></param>
        public void Respond(ISerializablePacket packet, ResponseStatus statusCode = ResponseStatus.Default)
        {
            Respond(MessageHelper.Create(OpCode, packet.ToBytes()), statusCode);
        }

        public void Respond(MessageBase packet, ResponseStatus statusCode = ResponseStatus.Default)
        {
            Respond(MessageHelper.Create(OpCode, packet), statusCode);
        }

        /// <summary>
        ///     Respond with empty message and status code
        /// </summary>
        /// <param name="statusCode"></param>
        public void Respond(ResponseStatus statusCode = ResponseStatus.Default)
        {
            Respond(MessageHelper.Create(OpCode), statusCode);
        }

        public void Respond(string message, ResponseStatus statusCode = ResponseStatus.Default)
        {
            Respond(message.ToBytes(), statusCode);
        }

        public void Respond(int response, ResponseStatus statusCode = ResponseStatus.Default)
        {
            Respond(MessageHelper.Create(OpCode, response), statusCode);
        }

        /// <summary>
        ///     Returns true if message contains any data
        /// </summary>
        public bool HasData
        {
            get { return _data.Length > 0; }
        }

        /// <summary>
        ///     Returns contents of this message. Mutable
        /// </summary>
        /// <returns></returns>
        public byte[] AsBytes()
        {
            return _data;
        }

        /// <summary>
        ///     Decodes content into a string
        /// </summary>
        /// <returns></returns>
        public string AsString()
        {
            return Encoding.UTF8.GetString(_data);
        }

        /// <summary>
        ///     Decodes content into a string. If there's no content,
        ///     returns the <see cref="defaultValue"/>
        /// </summary>
        /// <returns></returns>
        public string AsString(string defaultValue)
        {
            return HasData ? AsString() : defaultValue;
        }

        /// <summary>
        ///     Decodes content into an integer
        /// </summary>
        /// <returns></returns>
        public int AsInt()
        {
            return EndianBitConverter.Big.ToInt32(_data, 0);
        }

        /// <summary>
        ///     Writes content of the message into a packet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packetToBeFilled"></param>
        /// <returns></returns>
        public T Deserialize<T>(T packetToBeFilled) where T : ISerializablePacket
        {
            return MessageHelper.Deserialize(_data, packetToBeFilled);
        }

        public T Deserialize<T>() where T : MessageBase, new()
        {
            var reader = new NetworkReader(_data);
            return reader.ReadMessage<T>();
        }

        /// <summary>
        ///     Uses content of the message to regenerate list of packets
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packetCreator"></param>
        /// <returns></returns>
        public IEnumerable<T> DeserializeList<T>(Func<T> packetCreator) where T : ISerializablePacket
        {
            return MessageHelper.DeserializeList(_data, packetCreator);
        }

        /// <summary>
        /// Deserializes a list of standard uNet messages
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> DeserializeList<T>() where T : MessageBase, new()
        {
            var reader = new NetworkReader(_data);
            var count = reader.ReadInt32();

            var list = new List<T>();

            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadMessage<T>());
            }

            return list;
        }

        public override string ToString()
        {
            return AsString(base.ToString());
        }
    }
}