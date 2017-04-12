using System;
using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class MsfBaseClient
    {
        public IClientSocket Connection { get; private set; }

        // Client handlers list
        private Dictionary<short, IPacketHandler> _handlers;

        public MsfBaseClient(IClientSocket connection)
        {
            Connection = connection;

            _handlers = new Dictionary<short, IPacketHandler>();
        }

        /// <summary>
        /// Sets a message handler to connection, which is used by this this object
        /// to communicate with server
        /// </summary>
        /// <param name="handler"></param>
        public void SetHandler(IPacketHandler handler)
        {
            _handlers[handler.OpCode] = handler;

            if (Connection != null)
            {
                Connection.SetHandler(handler);
            }
        }

        /// <summary>
        /// Sets a message handler to connection, which is used by this this object
        /// to communicate with server 
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="handler"></param>
        public void SetHandler(short opCode, IncommingMessageHandler handler)
        {
            SetHandler(new PacketHandler(opCode, handler));
        }

        /// <summary>
        /// Changes the connection object, and sets all of the message handlers of this object
        /// to new connection.
        /// </summary>
        /// <param name="socket"></param>
        public void ChangeConnection(IClientSocket socket)
        {
            Connection = socket;

            // Override packet handlers
            foreach (var packetHandler in _handlers.Values)
            {
                socket.SetHandler(packetHandler);
            }
        }
    }
}