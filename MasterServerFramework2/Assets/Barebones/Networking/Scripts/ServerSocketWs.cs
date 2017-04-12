using System;
using System.Collections.Generic;
using Barebones.Networking;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Barebones.Networking
{
    /// <summary>
    /// Server socket, which accepts websocket connections
    /// </summary>
    public class ServerSocketWs : IServerSocket, IUpdatable
    {
        public event PeerActionHandler Connected;
        public event PeerActionHandler Disconnected;

        private WebSocketServer _server;

        private Queue<Action> _executeOnUpdate;

        private event Action OnUpdate;

        private float _initialDelay = 0;

        public ServerSocketWs()
        {
            _executeOnUpdate = new Queue<Action>();
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

        /// <summary>
        /// Opens the socket and starts listening to a given port
        /// </summary>
        /// <param name="port"></param>
        public void Listen(int port)
        {
            // Stop listening when application closes
            BTimer.Instance.ApplicationQuit += Stop;

            _server = new WebSocketServer(port);
            SetupService(_server);

            _server.Stop();
            _server.Start();

            BmUpdateRunner.Instance.Add(this);
        }

        /// <summary>
        /// Stops listening
        /// </summary>
        public void Stop()
        {
            BmUpdateRunner.Instance.Remove(this);
            _server.Stop(CloseStatusCode.Normal, "Stopping");
        }

        public void ExecuteOnUpdate(Action action)
        {
            lock (_executeOnUpdate)
            {
                _executeOnUpdate.Enqueue(action);
            }
        }

        private void SetupService(WebSocketServer server)
        {
            server.AddWebSocketService<WsService>("/w", () =>
            {
                var service = new WsService(this);
                var peer = new PeerWsServer(service);

                service.OnMessageEvent += (data) =>
                {
                    peer.HandleDataReceived(data, 0);
                };

                ExecuteOnUpdate(() =>
                {
                    BTimer.Instance.StartCoroutine(peer.SendDelayedMessages(_initialDelay));

                    if (Connected != null)
                        Connected.Invoke(peer);
                });

                peer.Disconnected += Disconnected;

                service.OnCloseEvent += reason =>
                {
                    peer.NotifyDisconnectEvent();
                };

                service.OnErrorEvent += reason =>
                {
                    //Logs.Error(reason);
                    peer.NotifyDisconnectEvent();
                };

                return service;
            });
        }

        public void Update()
        {
            if (OnUpdate != null)
                OnUpdate.Invoke();

            lock (_executeOnUpdate)
            {
                while (_executeOnUpdate.Count > 0)
                {
                    _executeOnUpdate.Dequeue().Invoke();
                }
            }
        }

        /// <summary>
        /// Web socket service, designed to work with unitys main thread
        /// </summary>
        public class WsService : WebSocketBehavior
        {
            private readonly ServerSocketWs _serverSocket;

            public event Action OnOpenEvent;
            public event Action<string> OnCloseEvent;
            public event Action<string> OnErrorEvent;
            public event Action<byte[]> OnMessageEvent;

            private Queue<byte[]> _messageQueue;

            public WsService(ServerSocketWs serverSocket)
            {
                IgnoreExtensions = true;
                _serverSocket = serverSocket;
                _serverSocket.OnUpdate += Update;
                _messageQueue = new Queue<byte[]>();
            }

            private void Update()
            {
                if (_messageQueue.Count <= 0)
                    return;

                lock (_messageQueue)
                {
                    // Notify about new messages
                    while (_messageQueue.Count > 0)
                    {
                        if (OnMessageEvent != null)
                        {
                            OnMessageEvent.Invoke(_messageQueue.Dequeue());
                        }
                    }
                }
            }

            protected override void OnOpen()
            {
                _serverSocket.ExecuteOnUpdate(() =>
                {
                    if (OnOpenEvent != null)
                        OnOpenEvent.Invoke();
                });
            }

            protected override void OnClose(CloseEventArgs e)
            {
                _serverSocket.OnUpdate -= Update;

                _serverSocket.ExecuteOnUpdate(() =>
                {
                    if (OnCloseEvent != null)
                        OnCloseEvent.Invoke(e.Reason);
                });
            }

            protected override void OnError(ErrorEventArgs e)
            {
                _serverSocket.OnUpdate -= Update;

                _serverSocket.ExecuteOnUpdate(() =>
                {
                    if (OnErrorEvent != null)
                        OnErrorEvent.Invoke(e.Message);
                });
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                lock (_messageQueue)
                {
                    _messageQueue.Enqueue(e.RawData);
                }
            }

            public void SendData(byte[] data)
            {
                Send(data);
            }

            public void Disconnect()
            {
                // Hope this works
                Sessions.CloseSession(ID);
            }
        }
    }
}