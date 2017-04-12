using System;
using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Represents a spawn request, and manages the state of request
    /// from start to finalization
    /// </summary>
    public class SpawnTask
    {
        public RegisteredSpawner Spawner { get; private set; }
        public Dictionary<string, string> Properties { get; private set; }
        public string CustomArgs { get; private set; }

        public int SpawnId { get; private set; }
        public event Action<SpawnStatus> StatusChanged;

        private SpawnStatus _status;

        public string UniqueCode { get; private set; }

        public SpawnFinalizationPacket FinalizationPacket { get; private set; }

        protected List<Action<SpawnTask>> WhenDoneCallbacks;

        public SpawnTask(int spawnId, RegisteredSpawner spawner, 
            Dictionary<string, string> properties, string customArgs) {

            SpawnId = spawnId;

            Spawner = spawner;
            Properties = properties;
            CustomArgs = customArgs;

            UniqueCode = Msf.Helper.CreateRandomString(6);
            WhenDoneCallbacks = new List<Action<SpawnTask>>();
        }

        public bool IsAborted { get { return _status < SpawnStatus.None; } }

        public bool IsDoneStartingProcess { get { return IsAborted || IsProcessStarted; } }

        public bool IsProcessStarted { get { return Status >= SpawnStatus.WaitingForProcess; } }

        public SpawnStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;

                if (StatusChanged != null)
                    StatusChanged.Invoke(_status);

                if (_status >= SpawnStatus.Finalized || _status < SpawnStatus.None)
                    NotifyDoneListeners();
            }
        }

        /// <summary>
        /// Peer, who registered a started process for this task
        /// (for example, a game server)
        /// </summary>
        public IPeer RegisteredPeer { get; private set; }

        /// <summary>
        /// Who requested to spawn
        /// (most likely clients peer)
        /// Can be null
        /// </summary>
        public IPeer Requester { get; set; }

        public void OnProcessStarted()
        {
            if (!IsAborted && Status < SpawnStatus.WaitingForProcess)
            {
                Status = SpawnStatus.WaitingForProcess;
            }
        }

        public void OnProcessKilled()
        {
            Status = SpawnStatus.Killed;
        }

        public void OnRegistered(IPeer peerWhoRegistered)
        {
            RegisteredPeer = peerWhoRegistered;

            if (!IsAborted && Status < SpawnStatus.ProcessRegistered)
            {
                Status = SpawnStatus.ProcessRegistered;
            }
        }

        public void OnFinalized(SpawnFinalizationPacket finalizationPacket)
        {
            FinalizationPacket = finalizationPacket;
            if (!IsAborted && Status < SpawnStatus.Finalized)
            {
                Status = SpawnStatus.Finalized;
            }
        }

        public override string ToString()
        {
            return string.Format("[SpawnTask: id - {0}]", SpawnId);
        }

        protected void NotifyDoneListeners()
        {
            foreach (var callback in WhenDoneCallbacks)
            {
                callback.Invoke(this);
            }

            WhenDoneCallbacks.Clear();
        }

        /// <summary>
        /// Callback will be called when spawn task is aborted or completed 
        /// (game server is opened)
        /// </summary>
        /// <param name="callback"></param>
        public SpawnTask WhenDone(Action<SpawnTask> callback)
        {
            WhenDoneCallbacks.Add(callback);
            return this;
        }

        public void Abort()
        {
            if (Status >= SpawnStatus.Finalized)
                return;

            Status = SpawnStatus.Aborting;

            KillSpawnedProcess();
        }

        public void KillSpawnedProcess()
        {
            Spawner.SendKillRequest(SpawnId, killed =>
            {
                Status = SpawnStatus.Aborted;

                if (!killed)
                    Logs.Warn("Spawned Process might not have been killed");
            });
        }
        
    }
}