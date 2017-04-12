using System;
using System.Collections;
using System.Collections.Generic;
using Barebones.Networking;
using UnityEngine;

namespace Barebones.Networking
{
    public class PeerWs : BasePeer
    {
        public const float Delay = 0.2f;

        private readonly WebSocket _socket;

        private Queue<byte[]> _delayedMessages;

        public PeerWs(WebSocket socket)
        {
            _socket = socket;

            _delayedMessages = new Queue<byte[]>();
        }

        public override bool IsConnected
        {
            get { return _socket.IsConnected; }
        }

        public IEnumerator SendDelayedMessages()
        {
            yield return new WaitForSecondsRealtime(Delay);

            if (_delayedMessages == null)
            {
                yield break;
            }

            lock (_delayedMessages)
            {
                if (_delayedMessages == null)
                    yield break;

                var copy = _delayedMessages;
                _delayedMessages = null;

                foreach (var data in copy)
                {
                    _socket.Send(data);
                }
            }
        }

        public override void SendMessage(IMessage message, DeliveryMethod deliveryMethod)
        {
            if (_delayedMessages != null)
            {
                lock (_delayedMessages)
                {
                    if (_delayedMessages != null)
                    {
                        _delayedMessages.Enqueue(message.ToBytes());
                        return;
                    }
                }
            }

            _socket.Send(message.ToBytes());
        }

        public override void Disconnect(string reason)
        {
            _socket.Close();
        }
    }

    public class PeerWsServer : BasePeer
    {
        private readonly ServerSocketWs.WsService _session;

        private bool _isConnected;

        private Queue<byte[]> _delayedMessages;

        public PeerWsServer(ServerSocketWs.WsService session)
        {
            _session = session;

            _session.OnOpenEvent += () => { _isConnected = true; };
            _session.OnCloseEvent += (msg) => { _isConnected = false; };
            _session.OnErrorEvent += (msg) => { _isConnected = false; };

            _delayedMessages = new Queue<byte[]>();

            // When we're creating a peer in server, it's considered that there's 
            // already a connection for which we're making it.
            _isConnected = true;
        }

        public IEnumerator SendDelayedMessages(float delay)
        {
            yield return new WaitForSecondsRealtime(0.2f);

            if (_delayedMessages == null)
            {
                Debug.LogError("Delayed messages wre already sent");
                yield break;
            }

            lock (_delayedMessages)
            {
                if (_delayedMessages == null)
                    yield break;

                var copy = _delayedMessages;
                _delayedMessages = null;

                foreach (var data in copy)
                {
                    _session.SendData(data);
                }
            }
        }

        public override bool IsConnected
        {
            get { return _isConnected; }
        }

        public override void SendMessage(IMessage message, DeliveryMethod deliveryMethod)
        {
            if (_delayedMessages != null)
            {
                // There's a bug in websockets
                // When server sends a message to client right after client
                // connects to it, the message is lost somewhere.
                // Sending first few messages with a small delay fixes this issue.

                lock (_delayedMessages)
                {
                    if (_delayedMessages != null)
                    {
                        _delayedMessages.Enqueue(message.ToBytes());
                        return;
                    }
                }
            }

            _session.SendData(message.ToBytes());
        }

        public override void Disconnect(string reason)
        {
            _session.Disconnect();
        }
    }
}