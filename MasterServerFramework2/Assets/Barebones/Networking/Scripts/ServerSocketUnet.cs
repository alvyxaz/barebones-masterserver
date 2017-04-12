using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Barebones.Networking
{
    /// <summary>
    ///     Represents a socket, which listen to a port, and to which
    ///     other clients can connect
    /// </summary>
    public class ServerSocketUnet : IServerSocket, IUpdatable
    {
        private readonly HostTopology _topology;

        private readonly Dictionary<int, PeerUnet> _connectedPeers;

        private readonly byte[] _msgBuffer;

        private int _socketId = -1;

        public event PeerActionHandler Connected;
        public event PeerActionHandler Disconnected;

        public ServerSocketUnet() : this(BarebonesTopology.Topology)
        {
        }

        public ServerSocketUnet(HostTopology topology)
        {
            _connectedPeers = new Dictionary<int, PeerUnet>();
            _topology = topology;

            _msgBuffer = new byte[NetworkMessage.MaxMessageSize];
        }

        public void Listen(int port)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            NetworkTransport.Init();
            _socketId = NetworkTransport.AddHost(_topology, port);
            BmUpdateRunner.Instance.Add(this);
#endif
        }

        public event PeerActionHandler OnConnected
        {
            add { Connected += value; }
            remove { Connected -= value; }
        }

        public event PeerActionHandler OnDisconnected
        {
            add { Disconnected += value; }
            remove { Disconnected -= value; }
        }

        public void Update()
        {
            if (_socketId == -1)
                return;

            NetworkEventType networkEvent;

            do
            {
                int connectionId;
                int channelId;
                int receivedSize;
                byte error;

                networkEvent = NetworkTransport.ReceiveFromHost(_socketId, out connectionId, out channelId, _msgBuffer,
                    _msgBuffer.Length, out receivedSize, out error);

                switch (networkEvent)
                {
                    case NetworkEventType.ConnectEvent:
                        HandleConnect(connectionId, error);
                        break;
                    case NetworkEventType.DataEvent:
                        HandleData(connectionId, channelId, receivedSize, error);
                        break;
                    case NetworkEventType.DisconnectEvent:
                        HandleDisconnect(connectionId, error);
                        break;
                    case NetworkEventType.Nothing:
                        break;
                    default:
                        Logs.Error("Unknown network message type received: " + networkEvent);
                        break;
                }
            } while (networkEvent != NetworkEventType.Nothing);
        }

        private void HandleDisconnect(int connectionId, byte error)
        {
            PeerUnet peer;
            _connectedPeers.TryGetValue(connectionId, out peer);

            if (peer == null)
                return;

            peer.Dispose();

            _connectedPeers.Remove(connectionId);

            peer.SetIsConnected(false);
            peer.NotifyDisconnectEvent();

            if (Disconnected != null)
                Disconnected.Invoke(peer);
        }

        private void HandleData(int connectionId, int channelId, int receivedSize, byte error)
        {
            PeerUnet peer;
            _connectedPeers.TryGetValue(connectionId, out peer);

            if (peer != null)
                peer.HandleDataReceived(_msgBuffer, 0);
        }

        private void HandleConnect(int connectionId, byte error)
        {
            if (error != 0)
            {
                Logs.Error(string.Format("Error on ConnectEvent. ConnectionId: {0}, error: {1}", connectionId, error));
                return;
            }

            var peer = new PeerUnet(connectionId, _socketId);
            peer.SetIsConnected(true);
            _connectedPeers.Add(connectionId, peer);

            peer.SetIsConnected(true);

            if (Connected != null)
                Connected.Invoke(peer);
        }

        public void Stop()
        {
            BmUpdateRunner.Instance.Remove(this);

#if !UNITY_WEBGL || UNITY_EDITOR
            NetworkTransport.RemoveHost(_socketId);
#endif
        }
    }
}