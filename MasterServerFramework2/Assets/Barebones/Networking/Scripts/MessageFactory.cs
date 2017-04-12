using System;
using UnityEngine;

namespace Barebones.Networking
{
    public class MessageFactory : IMessageFactory
    {
        public IMessage Create(short opCode)
        {
            return new Message(opCode);
        }

        public IMessage Create(short opCode, byte[] data)
        {
            return new Message(opCode, data);
        }

        /// <summary>
        ///     Used raw byte data to create an <see cref="IIncommingMessage" />
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="start"></param>
        /// <param name="peer"></param>
        /// <returns></returns>
        public IIncommingMessage FromBytes(byte[] buffer, int start, IPeer peer)
        {
            try
            {
                var converter = EndianBitConverter.Big;
                var flags = buffer[start];
                var opCode = converter.ToInt16(buffer, start + 1);
                var pointer = start + 3;

                var dataLength = converter.ToInt32(buffer, pointer);
                pointer += 4;
                var data = new byte[dataLength];
                Array.Copy(buffer, pointer, data, 0, dataLength);
                pointer += dataLength;

                var message = new IncommingMessage(opCode, flags, data, DeliveryMethod.Reliable, peer)
                {
                    SequenceChannel = 0
                };

                if ((flags & (byte) MessageFlag.AckRequest) > 0)
                {
                    // We received a message which requests a response
                    message.AckResponseId = converter.ToInt32(buffer, pointer);
                    pointer += 4;
                }

                if ((flags & (byte) MessageFlag.AckResponse) > 0)
                {
                    // We received a message which is a response to our ack request
                    var ackId = converter.ToInt32(buffer, pointer);
                    message.AckRequestId = ackId;
                    pointer += 4;

                    var statusCode = buffer[pointer];

                    message.Status = (ResponseStatus) statusCode; // TODO look into not exposing status code / ackRequestId
                    pointer++;
                }

                return message;
            }
            catch (Exception e)
            {
                Logs.Error("WS Failed parsing an incoming message " + e);
            }
            return null;
        }
    }
}