using System.Collections.Generic;
using System.Linq;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class RegisteredSpawner
    {
        public delegate void KillRequestCallback(bool isKilled);

        private readonly SpawnersModule _module;
        public static int MaxConcurrentRequests = 8;

        public int SpawnerId { get; set; }
        public IPeer Peer { get; set; }
        public SpawnerOptions Options { get; set; }

        private readonly Queue<SpawnTask> _queue;
        private readonly HashSet<SpawnTask> _startingProcesses;

        public int ProcessesRunning { get; private set; }

        private HashSet<SpawnTask> _beingSpawned;

        public RegisteredSpawner(int spawnerId, IPeer peer, SpawnerOptions options)
        {
            SpawnerId = spawnerId;
            Peer = peer;
            Options = options;

            _queue = new Queue<SpawnTask>();
            _beingSpawned = new HashSet<SpawnTask>();
        }

        public int CalculateFreeSlotsCount()
        {
            return Options.MaxProcesses - _queue.Count - ProcessesRunning;
        }

        public bool CanSpawnAnotherProcess()
        {
            // Unlimited
            if (Options.MaxProcesses == 0)
                return true;

            // Spawner is busy
            if (_queue.Count + ProcessesRunning >= Options.MaxProcesses)
                return false;

            return true;
        }

        public void AddTaskToQueue(SpawnTask task)
        {
            _queue.Enqueue(task);
        }

        public void UpdateQueue()
        {
            // Ignore if there's no connection with the peer
            if (!Peer.IsConnected)
                return;

            // Ignore if nothing's in the queue
            if (_queue.Count == 0)
                return;

            if (_beingSpawned.Count >= MaxConcurrentRequests)
            {
                // If we're currently at the maximum available concurrent spawn count
                var finishedSpawns = _beingSpawned.Where(s => s.IsDoneStartingProcess);

                // Remove finished spawns
                foreach (var finishedSpawn in finishedSpawns)
                    _beingSpawned.Remove(finishedSpawn);
            }

            // If we're still at the maximum concurrent requests
            if (_beingSpawned.Count >= MaxConcurrentRequests)
                return;

            var task = _queue.Dequeue();

            var data = new SpawnRequestPacket()
            {
                SpawnerId = SpawnerId,
                CustomArgs = task.CustomArgs,
                Properties = task.Properties,
                SpawnId = task.SpawnId,
                SpawnCode = task.UniqueCode
            };

            var msg = Msf.Create.Message((short) MsfOpCodes.SpawnRequest, data);
            Peer.SendMessage(msg, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    task.Abort();
                    Logs.Error("Spawn request was not handled. Status: " + status + " | " + response.AsString("Unknown Error"));
                    return;
                }
            });
        }

        public void SendKillRequest(int spawnId, KillRequestCallback callback)
        {
            var packet = new KillSpawnedProcessPacket()
            {
                SpawnerId = SpawnerId,
                SpawnId = spawnId
            };
            Peer.SendMessage((short) MsfOpCodes.KillSpawnedProcess, packet, (status, response) =>
            {
                callback.Invoke(status == ResponseStatus.Success);
            });
        }

        public void UpdateProcessesCount(int packetB)
        {
            ProcessesRunning = packetB;
        }

        public void OnProcessKilled()
        {
            ProcessesRunning -= 1;
        }

        public void OnProcessStarted()
        {
            ProcessesRunning += 1;
        }
    }
}