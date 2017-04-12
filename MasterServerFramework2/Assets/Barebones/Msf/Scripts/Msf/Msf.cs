using System;
using System.Collections;
using System.Collections.Generic;
using Barebones.Logging;
using Barebones.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace Barebones.MasterServer
{
    /// <summary>
    /// This class is a central class, which can be used by entities (clients and servers)
    /// that need to connect to master server, and access it's functionality
    /// </summary>
    public class Msf
    {
        /// <summary>
        /// Main connection to master server
        /// </summary>
        public static IClientSocket Connection { get; private set; }

        /// <summary>
        /// Advanced master server framework settings
        /// </summary>
        public static MsfAdvancedSettings Advanced { get; private set; }

        /// <summary>
        /// Collection of methods, that can be used BY CLIENT, connected to master server
        /// </summary>
        public static MsfClient Client { get; private set; }
        
        /// <summary>
        /// Collection of methods, that can be used from your servers
        /// </summary>
        public static MsfServer Server { get; private set; }

        public static MsfConcurrency Concurrency { get; private set; }

        /// <summary>
        /// Contains methods for creating some of the common types
        /// (server sockets, messages and etc)
        /// </summary>
        public static MsfCreate Create { get; set; }

        /// <summary>
        /// Contains helper methods, that couldn't be added to any other
        /// object
        /// </summary>
        public static MsfHelper Helper { get; set; }

        /// <summary>
        /// Contains security-related stuff (encryptions, permission requests)
        /// </summary>
        public static MsfSecurity Security { get; private set; }

        /// <summary>
        /// Default events channel
        /// </summary>
        public static EventsChannel Events { get; private set; }

        /// <summary>
        /// List of event names, used within the framework
        /// </summary>
        public static MsfEventNames EventNames { get; private set; }

        /// <summary>
        /// Contains methods, that work with runtime data
        /// </summary>
        public static MsfRuntime Runtime { get; private set; }

        /// <summary>
        /// Contains command line / terminal values, which were provided
        /// when starting the process
        /// </summary>
        public static MsfArgs Args { get; private set; }

        /// <summary>
        /// Version of the framework
        /// </summary>
        public static string Version { get { return "V2.0.2"; } }

        static Msf()
        {
            // Initialize advanced settings
            Advanced = new MsfAdvancedSettings();

            Runtime = new MsfRuntime();

            Args = new MsfArgs();

            Helper = new MsfHelper();

            // Create a default connection
            Connection = Advanced.ClientSocketFactory();

            // Initialize parts of framework, that act as "clients"
            Client = new MsfClient(Connection);
            Server = new MsfServer(Connection);
            Security = new MsfSecurity(Connection);

            // Other stuff
            Create = new MsfCreate();
            Concurrency = new MsfConcurrency();
            Events = new EventsChannel("default", true);
            EventNames = new MsfEventNames();
        }

        /// <summary>
        /// Connects to server
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        public void Connect(string ip, int port, float timeout)
        {
            Connection.Connect(ip, port, (int) (timeout*1000));
        }

        /// <summary>
        /// Invokes a callback, when successfully connected to master server,
        /// or after the timeout, if connection fails
        /// </summary>
        /// <param name="connectionCallback"></param>
        /// <param name="timeout"></param>
        public void WhenConnected(Action<IClientSocket> connectionCallback, float timeout)
        {
            Connection.WaitConnection(connectionCallback, timeout);
        }

        /// <summary>
        /// List of event names, used within the framework
        /// </summary>
        public class MsfEventNames
        {
            public string ShowLoading { get { return "msf.loading"; } }
            public string ShowDialogBox { get { return "msf.showDialog"; } }
            public string RestoreLoginForm { get { return "msf.restoreLoginForm"; } }
        }

        /// <summary>
        /// Advanced settings wrapper
        /// </summary>
        public class MsfAdvancedSettings
        {
            /// <summary>
            /// Factory, used to create client sockets
            /// </summary>
            public Func<IClientSocket> ClientSocketFactory = () => new ClientSocketWs();

            /// <summary>
            /// Factory, used to create server sockets
            /// </summary>
            public Func<IServerSocket> ServerSocketFactory = () => new ServerSocketWs();

            /// <summary>
            /// Message factory
            /// </summary>
            public IMessageFactory MessageFactory = new MessageFactory();

            /// <summary>
            /// Global logging settings
            /// </summary>
            public MsfLogController Logging;

            public MsfAdvancedSettings()
            {
                Logging = new MsfLogController(LogLevel.All);
            }
        }

        /// <summary>
        /// Logging settings wrapper
        /// </summary>
        public class MsfLogController
        {
            public MsfLogController(LogLevel globalLogLevel)
            {
                // Add default appender
                var appenders = new List<LogHandler>()
                {
                    LogAppenders.UnityConsoleAppenderWithNames
                };

                // Initialize the log manager
                LogManager.Initialize(appenders, LogLevel.All);
            }

            /// <summary>
            /// Overrides log levels of all the loggers
            /// </summary>
            /// <param name="logLevel"></param>
            public void ForceLogging(LogLevel logLevel)
            {
                LogManager.ForceLogLevel = logLevel;
            }

            public LogLevel GlobalLogLevel
            {
                get { return LogManager.GlobalLogLevel; } 
                set { LogManager.GlobalLogLevel = value; }
            }
        }

        public class MsfCreate
        {
            public IServerSocket ServerSocket()
            {
                var serverSocket = Advanced.ServerSocketFactory();
                return serverSocket;
            }

            /// <summary>
            /// Creates a logger of the given name
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public BmLogger Logger(string name)
            {
                return LogManager.GetLogger(name);
            }

            /// <summary>
            /// Creates a logger of the given name, and sets its defualt log level
            /// </summary>
            /// <param name="name"></param>
            /// <param name="defaulLogLevel"></param>
            /// <returns></returns>
            public BmLogger Logger(string name, LogLevel defaulLogLevel)
            {
                var logger =  LogManager.GetLogger(name);
                logger.LogLevel = defaulLogLevel;
                return logger;
            }

            /// <summary>
            /// Creates a generic success callback (for lazy people)
            /// </summary>
            /// <param name="callback"></param>
            /// <param name="unknownErrorMsg"></param>
            /// <returns></returns>
            public ResponseCallback SuccessCallback(SuccessCallback callback, string unknownErrorMsg = "Unknown Error")
            {
                return (status, response) =>
                {
                    if (status != ResponseStatus.Success)
                    {
                        callback.Invoke(false, response.AsString(unknownErrorMsg));
                        return;
                    }

                    callback.Invoke(true, null);
                };
            }

            #region Message Creation

            /// <summary>
            /// Creates an empty message
            /// </summary>
            /// <param name="opCode"></param>
            /// <returns></returns>
            public IMessage Message(short opCode)
            {
                return MessageHelper.Create(opCode);
            }

            /// <summary>
            /// Creates a message with string content
            /// </summary>
            /// <param name="opCode"></param>
            /// <param name="message"></param>
            /// <returns></returns>
            public IMessage Message(short opCode, string message)
            {
                return MessageHelper.Create(opCode, message);
            }

            /// <summary>
            /// Creates a message with int content
            /// </summary>
            /// <param name="opCode"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public IMessage Message(short opCode, int data)
            {
                return MessageHelper.Create(opCode, data);
            }

            /// <summary>
            /// Creates a message with binary data
            /// </summary>
            /// <param name="opCode"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public IMessage Message(short opCode, byte[] data)
            {
                return MessageHelper.Create(opCode, data);
            }

            /// <summary>
            /// Creates a message by serializing a standard Unet message
            /// </summary>
            /// <param name="opCode"></param>
            /// <param name="message"></param>
            /// <returns></returns>
            public IMessage Message(short opCode, MessageBase message)
            {
                return MessageHelper.Create(opCode, message);
            }

            /// <summary>
            /// Creates a message by serializing a packet
            /// </summary>
            /// <param name="opCode"></param>
            /// <param name="packet"></param>
            /// <returns></returns>
            public IMessage Message(short opCode, ISerializablePacket packet)
            {
                return MessageHelper.Create(opCode, packet.ToBytes());
            }

            #endregion
        }

        public class MsfHelper
        {
            /// <summary>
            /// Creates a random string of a given length.
            /// Uses a substring of guid
            /// </summary>
            /// <param name="length"></param>
            /// <returns></returns>
            public string CreateRandomString(int length)
            {
                if (length < 0) throw new ArgumentOutOfRangeException("length", "length cannot be less than zero.");

                return Guid.NewGuid().ToString().Substring(0, length);
            }

            /// <summary>
            /// Retrieves current public IP
            /// </summary>
            /// <param name="callback"></param>
            public void GetPublicIp(Action<string> callback)
            {
                BTimer.Instance.StartCoroutine(GetPublicIPCoroutine(callback));
            }

            private IEnumerator GetPublicIPCoroutine(Action<string> callback)
            {
                var req = new WWW("http://checkip.dyndns.org");
                yield return req;
                var ip = req.text;
                ip = ip.Substring(ip.IndexOf(":") + 1);
                ip = ip.Substring(0, ip.IndexOf("<"));
                callback.Invoke(ip);
            }
        }
    }
}