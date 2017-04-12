using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Barebones.Networking
{
    public interface IIncommingMessage
    {
        /// <summary>
        ///     Message flags
        /// </summary>
        byte Flags { get; }

        /// <summary>
        ///     Operation code (message type)
        /// </summary>
        short OpCode { get; }

        /// <summary>
        ///     Sender
        /// </summary>
        IPeer Peer { get; }

        /// <summary>
        ///     Ack id the message is responding to
        /// </summary>
        int? AckResponseId { get; }

        /// <summary>
        ///     We add this to a packet to so that receiver knows
        ///     what he responds to
        /// </summary>
        int? AckRequestId { get; }

        /// <summary>
        ///     Returns true, if sender expects a response to this message
        /// </summary>
        bool IsExpectingResponse { get; }

        /// <summary>
        ///     For ordering
        /// </summary>
        int SequenceChannel { get; set; }

        /// <summary>
        ///     Message status code
        /// </summary>
        ResponseStatus Status { get; }

        /// <summary>
        ///     Returns true if message contains any data
        /// </summary>
        bool HasData { get; }

        /// <summary>
        ///     Respond with a message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        void Respond(IMessage message, ResponseStatus statusCode = ResponseStatus.Default);

        /// <summary>
        ///     Respond with data (message is created internally)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="statusCode"></param>
        void Respond(byte[] data, ResponseStatus statusCode = ResponseStatus.Default);

        /// <summary>
        ///     Respond with data (message is created internally)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="statusCode"></param>
        void Respond(ISerializablePacket packet, ResponseStatus statusCode = ResponseStatus.Default);

        /// <summary>
        /// Respons with standard Unet message
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="statusCode"></param>
        void Respond(MessageBase packet, ResponseStatus statusCode = ResponseStatus.Default);

        /// <summary>
        ///     Respond with empty message and status code
        /// </summary>
        /// <param name="statusCode"></param>
        void Respond(ResponseStatus statusCode);

        /// <summary>
        ///     Respond with string message
        /// </summary>
        void Respond(string message, ResponseStatus statusCode = ResponseStatus.Default);

        /// <summary>
        /// Respond with integer
        /// </summary>
        /// <param name="response"></param>
        /// <param name="statusCode"></param>
        void Respond(int response, ResponseStatus statusCode = ResponseStatus.Default);

        /// <summary>
        ///     Returns contents of this message. Mutable
        /// </summary>
        /// <returns></returns>
        byte[] AsBytes();

        /// <summary>
        ///     Decodes content into a string
        /// </summary>
        /// <returns></returns>
        string AsString();

        /// <summary>
        ///     Decodes content into a string. If there's no content,
        ///     returns the <see cref="defaultValue"/>
        /// </summary>
        /// <returns></returns>
        string AsString(string defaultValue);

        /// <summary>
        ///     Decodes content into an integer
        /// </summary>
        /// <returns></returns>
        int AsInt();

        /// <summary>
        ///     Writes content of the message into a packet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packetToBeFilled"></param>
        /// <returns></returns>
        T Deserialize<T>(T packetToBeFilled) where T : ISerializablePacket;

        /// <summary>
        /// Deserializes as a standard uNet message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Deserialize<T>() where T : MessageBase, new();

        /// <summary>
        ///     Uses content of the message to regenerate list of packets
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packetCreator"></param>
        /// <returns></returns>
        IEnumerable<T> DeserializeList<T>(Func<T> packetCreator) where T : ISerializablePacket;

        /// <summary>
        ///     Uses content of the message to generate a list of uNet messages
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packetCreator"></param>
        /// <returns></returns>
        IEnumerable<T> DeserializeList<T>() where T : MessageBase, new();
    }
}