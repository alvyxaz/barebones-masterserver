using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public interface IServer
    {
        /// <summary>
        /// Adds a module to the server
        /// </summary>
        /// <param name="module"></param>
        void AddModule(IServerModule module);

        /// <summary>
        /// Adds a module and tries to initialize all of the uninitialized modules
        /// </summary>
        /// <param name="module"></param>
        void AddModuleAndInitialize(IServerModule module);

        /// <summary>
        /// Returns true, if this server contains a module of given type
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        bool ContainsModule(IServerModule module);
        
        /// <summary>
        /// Tries to initialize modules that were not initialized,
        /// and returns true if all of the modules are initialized successfully
        /// </summary>
        /// <returns></returns>
        bool InitializeModules();

        /// <summary>
        /// Returns a module of specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetModule<T>() where T : class, IServerModule;

        /// <summary>
        /// Returns an immutable list of initialized modules
        /// </summary>
        /// <returns></returns>
        List<IServerModule> GetInitializedModules();

        /// <summary>
        /// Returns an immutable list of initialized modules
        /// </summary>
        /// <returns></returns>
        List<IServerModule> GetUninitializedModules();

        /// <summary>
        /// Adds a message handler to the collection of handlers.
        /// It will be invoked when server receives a message with
        /// OpCode <see cref="IPacketHandler.OpCode"/>
        /// </summary>
        void SetHandler(IPacketHandler handler);

        /// <summary>
        /// Adds a message handler to the collection of handlers.
        /// It will be invoked when server receives a message with
        /// OpCode <see cref="opCode"/>
        /// </summary>
        void SetHandler(short opCode, IncommingMessageHandler handler);

        /// <summary>
        /// Returns a connected peer with a given ID
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        IPeer GetPeer(int peerId);
    }
}