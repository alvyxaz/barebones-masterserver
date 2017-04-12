using System;
using System.Collections.Generic;
using System.IO;
using Barebones.Networking;
using UnityEngine;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Represents clients profile, which emits events about changes.
    /// Client, game server and master servers will create a similar
    /// object.
    /// </summary>
    public class ObservableServerProfile : ObservableProfile
    {
        /// <summary>
        /// Username of the client, who's profile this is
        /// </summary>
        public string Username { get; private set; }

        public IPeer ClientPeer { get; set; }

        public event Action<ObservableServerProfile> ModifiedInServer;
        public event Action<ObservableServerProfile> Disposed;

        public bool ShouldBeSavedToDatabase = true;

        public ObservableServerProfile(string username)
        {
            Username = username;
        }

        public ObservableServerProfile(string username, IPeer peer)
        {
            Username = username;
            ClientPeer = peer;
        }

        protected override void OnDirtyProperty(IObservableProperty property)
        {
            base.OnDirtyProperty(property);

            if (ModifiedInServer != null)
                ModifiedInServer.Invoke(this);
        }

        protected void Dispose()
        {
           if (Disposed != null)
                Dispose();

            ModifiedInServer = null;
            Disposed = null;

            UnsavedProperties.Clear();
            ClearUpdates();
        }
    }
}