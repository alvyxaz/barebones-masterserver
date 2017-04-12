using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Barebones.Logging;
using Barebones.Networking;
using UnityEngine;

namespace Barebones.MasterServer
{
    public class ServerBehaviour : MonoBehaviour, IServer
    {
        #region Unity Inspector Properties

        [Header("Server Behaviour")]
        [Tooltip("If true, will look for game objects with modules in scene, and initialize them")]
        public bool LookForModules = true;

        [Tooltip("If true, will go through children of this GameObject, and initialize " +
                 "modules that are found on the way")]
        public bool LookInChildrenOnly = true;

        public List<PermissionEntry> Permissions;

        #endregion

        protected IServerSocket Socket;

        public event PeerActionHandler PeerConnected;
        public event PeerActionHandler PeerDisconnected;
        public event Action Started;

        public Dictionary<int, IPeer> ConnectedPeers;
        public Dictionary<Guid, IPeer> PeersByGuidLookup;

        private Dictionary<Type, IServerModule> _modules;
        private HashSet<Type> _initializedModules;

        protected Dictionary<short, IPacketHandler> Handlers;

        public bool IsRunning { get; protected set; }

        private BmLogger Logger = Msf.Create.Logger(typeof(ServerBehaviour).Name);

        protected string InternalServerErrorMessage = "Internal Server Error";
        protected bool RethrowExceptionsInEditor = true;

        protected virtual void Awake()
        {
            ConnectedPeers = new Dictionary<int, IPeer>();
            _modules = new Dictionary<Type, IServerModule>();
            _initializedModules = new HashSet<Type>();
            Handlers = new Dictionary<short, IPacketHandler>();
            PeersByGuidLookup = new Dictionary<Guid, IPeer>();

            // Create the server 
            Socket = Msf.Create.ServerSocket();

            Socket.Connected += Connected;
            Socket.Disconnected += Disconnected;

            // AesKey handler
            SetHandler((short)MsfOpCodes.AesKeyRequest, HandleAesKeyRequest);
            SetHandler((short)MsfOpCodes.RequestPermissionLevel, HandlePermissionLevelRequest);
            SetHandler((short)MsfOpCodes.PeerGuidRequest, HandleGetPeerGuidRequest);
        }

        public virtual void StartServer(int port)
        {
            Socket.Listen(port);

            IsRunning = true;

            if (Started != null)
                Started.Invoke();

            if (LookForModules)
            {
                // Find modules
                var modules = LookInChildrenOnly ? GetComponentsInChildren<ServerModuleBehaviour>() :
                    FindObjectsOfType<ServerModuleBehaviour>();

                // Add modules
                foreach (var module in modules)
                    AddModule(module);

                // Initialize modules
                InitializeModules();
            }
        }

        public virtual void StopServer()
        {
            IsRunning = false;
            Socket.Stop();
        }

        private void Connected(IPeer peer)
        {
            // Listen to messages
            peer.MessageReceived += OnMessageReceived;

            // Save the peer
            ConnectedPeers[peer.Id] = peer;

            // Create the security extension
            var extension = peer.AddExtension(new PeerSecurityExtension());

            // Set default permission level
            extension.PermissionLevel = 0;

            // Create a unique peer guid
            extension.UniqueGuid = Guid.NewGuid();
            PeersByGuidLookup[extension.UniqueGuid] = peer;

            // Invoke the event
            if (PeerConnected != null)
                PeerConnected(peer);

            OnPeerConnected(peer);
        }

        private void Disconnected(IPeer peer)
        {
            // Remove listener to messages
            peer.MessageReceived -= OnMessageReceived;

            // Remove the peer
            ConnectedPeers.Remove(peer.Id);

            var extension = peer.GetExtension<PeerSecurityExtension>();
            if (extension != null)
            {
                // Remove from guid lookup
                PeersByGuidLookup.Remove(extension.UniqueGuid);
            }

            // Invoke the event
            if (PeerDisconnected != null)
                PeerDisconnected(peer);

            OnPeerDisconnected(peer);
        }

        protected virtual void OnDestroy()
        {
            Socket.Connected -= Connected;
            Socket.Disconnected -= Disconnected;
        }

        #region Message Handlers

        protected virtual void HandleGetPeerGuidRequest(IIncommingMessage message)
        {
            var extension = message.Peer.GetExtension<PeerSecurityExtension>();
            message.Respond(extension.UniqueGuid.ToByteArray(), ResponseStatus.Success);
        }

        protected virtual void HandlePermissionLevelRequest(IIncommingMessage message)
        {
            var key = message.AsString();

            var extension = message.Peer.GetExtension<PeerSecurityExtension>();

            var currentLevel = extension.PermissionLevel;
            var newLevel = currentLevel;

            var permissionClaimed = false;

            foreach (var entry in Permissions)
            {
                if (entry.Key == key)
                {
                    newLevel = entry.PermissionLevel;
                    permissionClaimed = true;
                }
            }

            extension.PermissionLevel = newLevel;

            if (!permissionClaimed && !string.IsNullOrEmpty(key))
            {
                // If we didn't claim a permission
                message.Respond("Invalid permission key", ResponseStatus.Unauthorized);
                return;
            }

            message.Respond(newLevel, ResponseStatus.Success);
        }

        protected virtual void HandleAesKeyRequest(IIncommingMessage message)
        {
            var extension = message.Peer.GetExtension<PeerSecurityExtension>();

            var encryptedKey = extension.AesKeyEncrypted;

            if (encryptedKey != null)
            {
                // There's already a key generated
                message.Respond(encryptedKey, ResponseStatus.Success);
                return;
            }

            // Generate a random key
            var aesKey = Msf.Helper.CreateRandomString(8);

            var clientsPublicKeyXml = message.AsString();

            // Deserialize public key
            var sr = new System.IO.StringReader(clientsPublicKeyXml);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            var clientsPublicKey = (RSAParameters)xs.Deserialize(sr);

            using (var csp = new RSACryptoServiceProvider())
            {
                csp.ImportParameters(clientsPublicKey);
                var encryptedAes = csp.Encrypt(Encoding.Unicode.GetBytes(aesKey), false);

                // Save keys for later use
                extension.AesKeyEncrypted = encryptedAes;
                extension.AesKey = aesKey;

                message.Respond(encryptedAes, ResponseStatus.Success);
            }
        }

        #endregion

        #region Virtual methods

        protected virtual void OnPeerConnected(IPeer peer)
        {
        }

        protected virtual  void OnPeerDisconnected(IPeer peer)
        {
        }

        protected virtual void OnMessageReceived(IIncommingMessage message)
        {
            try
            {
                IPacketHandler handler;
                Handlers.TryGetValue(message.OpCode, out handler);

                if (handler == null)
                {
                    Logger.Warn(string.Format("Handler for OpCode {0} does not exist", message.OpCode));

                    if (message.IsExpectingResponse)
                    {
                        message.Respond(InternalServerErrorMessage, ResponseStatus.NotHandled);
                        return;
                    }
                    return;
                }

                handler.Handle(message);
            }
            catch (Exception e)
            {
                if (Msf.Runtime.IsEditor)
                    throw;

                Logger.Error("Error while handling a message from Client. OpCode: " + message.OpCode);
                Logger.Error(e);

                if (!message.IsExpectingResponse)
                    return;

                try
                {
                    message.Respond(InternalServerErrorMessage, ResponseStatus.Error);
                }
                catch (Exception exception)
                {
                    Logs.Error(exception);
                }
            }
        }

        protected virtual void OnServerStopped()
        {
            
        }

        #endregion

        #region IServer

        public void AddModule(IServerModule module)
        {
            if (_modules.ContainsKey(module.GetType()))
            {
                throw new Exception("A module already exists in the server: " + module.GetType());
            }

            _modules[module.GetType()] = module;
        }

        public void AddModuleAndInitialize(IServerModule module)
        {
            AddModule(module);
            InitializeModules();
        }

        public bool ContainsModule(IServerModule module)
        {
            return _modules.ContainsKey(module.GetType());
        }

        public bool InitializeModules()
        {
            var checkOptional = true;

            // Initialize modules
            while (true)
            {
                var changed = false;
                foreach (var entry in _modules)
                {
                    // Module is already initialized
                    if (_initializedModules.Contains(entry.Key))
                        continue;

                    // Not all dependencies have been initialized
                    if (!entry.Value.Dependencies.All(d => _initializedModules.Any(d.IsAssignableFrom)))
                        continue;

                    // Not all OPTIONAL dependencies have been initialized
                    if (checkOptional && !entry.Value.OptionalDependencies.All(d => _initializedModules.Any(d.IsAssignableFrom)))
                        continue;

                    // If we got here, we can initialize our module
                    entry.Value.Server = this;
                    entry.Value.Initialize(this);
                    _initializedModules.Add(entry.Key);

                    // Keep checking optional if something new was initialized
                    checkOptional = true;

                    changed = true;
                }

                // If we didn't change anything, and initialized all that we could
                // with optional dependencies in mind
                if (!changed && checkOptional)
                {
                    // Initialize everything without checking optional dependencies
                    checkOptional = false;
                    continue;
                }

                // If we can no longer initialize anything
                if (!changed)
                    return !GetUninitializedModules().Any();
            }
        }

        public T GetModule<T>() where T : class, IServerModule
        {
            IServerModule module;
            _modules.TryGetValue(typeof(T), out module);

            if (module == null)
            {
                // Try to find an assignable module
                module = _modules.Values.FirstOrDefault(m => m is T);
            }

            return module as T;
        }

        public List<IServerModule> GetInitializedModules()
        {
            return _modules
                .Where(m => _initializedModules.Contains(m.Key))
                .Select(m => m.Value)
                .ToList();
        }

        public List<IServerModule> GetUninitializedModules()
        {
            return _modules
                .Where(m => !_initializedModules.Contains(m.Key))
                .Select(m => m.Value)
                .ToList();
        }

        public void SetHandler(IPacketHandler handler)
        {
            Handlers[handler.OpCode] = handler;
        }

        public void SetHandler(short opCode, IncommingMessageHandler handler)
        {
            Handlers[opCode] = new PacketHandler(opCode, handler);
        }

        public IPeer GetPeer(int peerId)
        {
            IPeer peer;
            ConnectedPeers.TryGetValue(peerId, out peer);
            return peer;
        }

        #endregion


        [Serializable]
        public class PermissionEntry
        {
            public string Key;
            public int PermissionLevel;
        }
    }
}