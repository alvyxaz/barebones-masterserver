using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Barebones.Networking
{
    /// <summary>
    ///     Unet low level api based peer implementation
    /// </summary>
    public class PeerUnet : BasePeer
    {
        private static readonly int _reliableChannel = BarebonesTopology.ReliableChannel;
        private static readonly int _reliableSequenced = BarebonesTopology.ReliableSequencedChannel;
        private static readonly int _unreliableChannel = BarebonesTopology.UnreliableChannel;

        private readonly int _connectionId;
        private readonly int _socketId;
        private bool _isConnected;

        public PeerUnet(int connectionId, int socketId)
        {
            _connectionId = connectionId;
            _socketId = socketId;
        }

        /// <summary>
        ///     True, if connection is stil valid
        /// </summary>
        public override bool IsConnected
        {
            get { return _isConnected; }
        }

        /// <summary>
        ///     Sends a message to peer
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="deliveryMethod">Delivery method</param>
        /// <returns></returns>
        public override void SendMessage(IMessage message, DeliveryMethod deliveryMethod)
        {
            if (!IsConnected)
                return;

            // TODO update this monstrosity
            var channelId = deliveryMethod == DeliveryMethod.Reliable
                ? _reliableChannel
                : deliveryMethod == DeliveryMethod.Unreliable ? _unreliableChannel : _reliableSequenced;

            var bytes = message.ToBytes();
            byte error;
            NetworkTransport.Send(_socketId, _connectionId, channelId, bytes, bytes.Length, out error);
        }

        /// <summary>
        ///     Force disconnect
        /// </summary>
        /// <param name="reason"></param>
        public override void Disconnect(string reason)
        {
            byte error;
            NetworkTransport.Disconnect(_socketId, _connectionId, out error);
        }

        

        public void SetIsConnected(bool status)
        {
            _isConnected = status;
        }
    }
}