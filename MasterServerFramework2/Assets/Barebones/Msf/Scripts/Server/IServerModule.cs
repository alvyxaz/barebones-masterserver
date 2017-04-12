using System;
using System.Collections.Generic;

namespace Barebones.MasterServer
{
    public interface IServerModule
    {
        IEnumerable<Type> Dependencies { get; }
        IEnumerable<Type> OptionalDependencies { get; }

        /// <summary>
        /// Server, which initialized this module.
        /// Will be null, until the module is initialized
        /// </summary>
        ServerBehaviour Server { get; set; }

        void Initialize(IServer server);
    }
}