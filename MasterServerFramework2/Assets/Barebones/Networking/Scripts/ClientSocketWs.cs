using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.Networking
{
    /// <summary>
    /// Client for connecting to websocket server.
    /// </summary>
    public class ClientSocketWs : BaseClientSocket, IClientSocket, IUpdatable
    {
        public static bool RethrowExceptionsInEditor = true;

        private WebSocket _socket;
        private PeerWs _peer;
        private ConnectionStatus _status;
        private readonly Dictionary<short, IPacketHandler> _handlers;

        public event Action Connected;
        public event Action Disconnected;

        public event Action<ConnectionStatus> StatusChanged;

        private bool _isConnected;

        public bool IsConnected { get { return _isConnected; } }
        public bool IsConnecting { get { return _status == ConnectionStatus.Connecting; } }

        private string _ip;
        private int _port;

        public string ConnectionIp { get { return _ip; } }

        public int ConnectionPort { get {return _port;} }

        public ClientSocketWs()
        {
            Status = ConnectionStatus.Disconnected;
            _handlers = new Dictionary<short, IPacketHandler>();
        }

        /// <summary>
        /// Invokes a callback when connection is established, or after the timeout
        /// (even if failed to connect). If already connected, callback is invoked instantly
        /// </summary>
        /// <param name="connectionCallback"></param>
        /// <param name="timeoutSeconds"></param>
        public void WaitConnection(Action<IClientSocket> connectionCallback, float timeoutSeconds)
        {
            if (IsConnected)
            {
                connectionCallback.Invoke(this);
                return;
            }

            var isConnected = false;
            var timedOut = false;
            Action onConnected = null;
            onConnected = () =>
            {
                Connected -= onConnected;
                isConnected = true;

                if (!timedOut)
                {
                    connectionCallback.Invoke(this);
                }
            };

            Connected += onConnected;

            BTimer.AfterSeconds(timeoutSeconds, () =>
            {
                if (!isConnected)
                {
                    timedOut = true;
                    Connected -= onConnected;
                    connectionCallback.Invoke(this);
                }
            });
        }

        /// <summary>
        /// Invokes a callback when connection is established, or after the timeout
        /// (even if failed to connect). If already connected, callback is invoked instantly
        /// </summary>
        /// <param name="connectionCallback"></param>
        public void WaitConnection(Action<IClientSocket> connectionCallback)
        {
            WaitConnection(connectionCallback, 10);
        }

        /// <summary>
        /// Adds a listener, which is invoked when connection is established,
        /// or instantly, if already connected and  <see cref="invokeInstantlyIfConnected"/> 
        /// is true
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="invokeInstantlyIfConnected"></param>
        public void AddConnectionListener(Action callback, bool invokeInstantlyIfConnected = true)
        {
            Connected += callback;

            if (IsConnected && invokeInstantlyIfConnected)
                callback.Invoke();
        }

        /// <summary>
        /// Removes connection listener
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveConnectionListener(Action callback)
        {
            Connected -= callback;
        }

        /// <summary>
        /// Adds a packet handler, which will be invoked when a message of
        /// specific operation code is received
        /// </summary>
        public IPacketHandler SetHandler(IPacketHandler handler)
        {
            _handlers[handler.OpCode] = handler;
            return handler;
        }

        /// <summary>
        /// Adds a packet handler, which will be invoked when a message of
        /// specific operation code is received
        /// </summary>
        public IPacketHandler SetHandler(short opCode, IncommingMessageHandler handlerMethod)
        {
            var handler = new PacketHandler(opCode, handlerMethod);
            SetHandler(handler);
            return handler;
        }

        /// <summary>
        /// Removes the packet handler, but only if this exact handler
        /// was used
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveHandler(IPacketHandler handler)
        {
            IPacketHandler previousHandler;
            _handlers.TryGetValue(handler.OpCode, out previousHandler);

            if (previousHandler != handler)
                return;

            _handlers.Remove(handler.OpCode);
        }

        /// <summary>
        /// Disconnects and connects again
        /// </summary>
        public void Reconnect()
        {
            Disconnect();
            Connect(_ip, _port);
        }

        // Update is called once per frame
        public void Update()
        {
            if (_socket == null)
            {
                return;
            }

            byte[] data = _socket.Recv();

            while (data != null)
            {
                _peer.HandleDataReceived(data, 0);
                data = _socket.Recv();
            }

            var wasConnected = _isConnected;
            _isConnected = _socket.IsConnected;

            if (wasConnected != _isConnected)
            {
                // If status has changed
                SetStatus(_isConnected ? ConnectionStatus.Connected :  ConnectionStatus.Disconnected);
            }
        }

        /// <summary>
        /// Connection status
        /// </summary>
        public ConnectionStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    if (StatusChanged != null)
                        StatusChanged.Invoke(_status);
                }
            }
        }


        private void SetStatus(ConnectionStatus status)
        {
            switch (status)
            {
                case ConnectionStatus.Connecting:
                    if (Status != ConnectionStatus.Connecting)
                    {
                        Status = ConnectionStatus.Connecting;
                    }
                    break;
                case ConnectionStatus.Connected:
                    if (Status != ConnectionStatus.Connected)
                    {
                        Status = ConnectionStatus.Connected;
                        BTimer.Instance.StartCoroutine(_peer.SendDelayedMessages());
                        if (Connected != null) Connected.Invoke();
                    }
                    break;
                case ConnectionStatus.Disconnected:
                    if (Status != ConnectionStatus.Disconnected)
                    {
                        Status = ConnectionStatus.Disconnected;
                        if (Disconnected != null) Disconnected.Invoke();
                    }
                    break;
            }
        }

        /// <summary>
        /// Starts connecting to another socket
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public IClientSocket Connect(string ip, int port)
        {
            Connect(ip, port, 10000);
            return this;
        }

        /// <summary>
        /// Starts connecting to another socket
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="timeoutMillis"></param>
        /// <returns></returns>
        public IClientSocket Connect(string ip, int port, int timeoutMillis)
        {
            _ip = ip;
            _port = port;

            if (_socket != null && _socket.IsConnected)
            {
                _socket.Close();
            }

            _isConnected = false;
            Status = ConnectionStatus.Connecting;

            if (_peer != null)
            {
                _peer.MessageReceived -= HandleMessage;
                _peer.Dispose();
            }

            _socket = new WebSocket(new Uri(string.Format("ws://{0}:{1}/w", ip, port)));
            var peer = new PeerWs(_socket);
            peer.MessageReceived += HandleMessage;
            _peer = peer;
            Peer = peer;

            BmUpdateRunner.Instance.Add(this);
            BmUpdateRunner.Instance.StartCoroutine(_socket.Connect());

            return this;
        }

        public void Disconnect()
        {
            if (_socket != null)
            {
                _socket.Close();
            }

            if (_peer != null)
            {
                _peer.Dispose();
            }

            _isConnected = false; //EMIL Fix
            SetStatus(ConnectionStatus.Disconnected); // EMIL strikes again!
        }

        private void HandleMessage(IIncommingMessage message)
        {
            try
            {
                IPacketHandler handler;
                _handlers.TryGetValue(message.OpCode, out handler);

                if (handler != null)
                    handler.Handle(message);
                else if (message.IsExpectingResponse)
                {
                    Logs.Error("Connection is missing a handler. OpCode: " + message.OpCode);
                    message.Respond(ResponseStatus.Error);
                }
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                if (RethrowExceptionsInEditor)
                    throw;
#endif

                Logs.Error("Failed to handle a message. OpCode: " + message.OpCode);
                Logs.Error(e);

                if (!message.IsExpectingResponse)
                    return;

                try
                {
                    message.Respond(ResponseStatus.Error);
                }
                catch (Exception exception)
                {
                    Logs.Error(exception);
                }
            }
        }
    }

}