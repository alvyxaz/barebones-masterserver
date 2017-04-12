using System;
using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public delegate void RegisterSpawnerCallback(SpawnerController spawner, string error);
    public delegate void RegisterSpawnedProcessCallback(SpawnTaskController taskController, string error);
    public delegate void CompleteSpawnedProcessCallback(bool isSuccessful, string error);

    public class MsfSpawnersServer : MsfBaseClient
    {
        private Dictionary<int, SpawnerController> _locallyCreatedSpawners;

        public int PortsStartFrom = 1500;

        private Queue<int> _freePorts;
        private int _lastPortTaken = -1;

        /// <summary>
        /// If true, this process is considered to be spawned by the spawner
        /// </summary>
        public bool IsSpawnedProccess { get; private set; }

        /// <summary>
        /// Invoked on "spawner server", when it successfully registers to master server
        /// </summary>
        public event Action<SpawnerController> SpawnerRegistered;
        
        public MsfSpawnersServer(IClientSocket connection) : base(connection)
        {
            _locallyCreatedSpawners = new Dictionary<int, SpawnerController>();
            _freePorts = new Queue<int>();

            IsSpawnedProccess = Msf.Args.IsProvided(Msf.Args.Names.SpawnCode);
        }

        /// <summary>
        /// Sends a request to master server, to register an existing spawner with given options
        /// </summary>
        /// <param name="options"></param>
        /// <param name="callback"></param>
        public void RegisterSpawner(SpawnerOptions options, RegisterSpawnerCallback callback)
        {
            RegisterSpawner(options, callback, Connection);
        }

        /// <summary>
        /// Sends a request to master server, to register an existing spawner with given options
        /// </summary>
        public void RegisterSpawner(SpawnerOptions options, RegisterSpawnerCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            connection.SendMessage((short) MsfOpCodes.RegisterSpawner, options, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown Error"));
                    return;
                }

                var spawnerId = response.AsInt();

                var controller = new SpawnerController(spawnerId, connection, options);

                // Save reference
                _locallyCreatedSpawners[spawnerId] = controller;

                callback.Invoke(controller, null);
                
                // Invoke the event
                if (SpawnerRegistered != null)
                    SpawnerRegistered.Invoke(controller);
            });
        }

        /// <summary>
        /// This method should be called, when spawn process is finalized (finished spawning).
        /// For example, when spawned game server fully starts
        /// </summary>
        /// <param name="spawnId"></param>
        /// <param name="callback"></param>
        public void FinalizeSpawnedProcess(int spawnId, CompleteSpawnedProcessCallback callback)
        {
            FinalizeSpawnedProcess(spawnId, new Dictionary<string, string>(), callback, Connection);
        }

        /// <summary>
        /// This method should be called, when spawn process is finalized (finished spawning).
        /// For example, when spawned game server fully starts
        /// </summary>
        public void FinalizeSpawnedProcess(int spawnId, Dictionary<string, string> finalizationData, CompleteSpawnedProcessCallback callback)
        {
            FinalizeSpawnedProcess(spawnId, finalizationData, callback, Connection);
        }

        /// <summary>
        /// This method should be called, when spawn process is finalized (finished spawning).
        /// For example, when spawned game server fully starts
        /// </summary>
        public void FinalizeSpawnedProcess(int spawnId, Dictionary<string, string> finalizationData, CompleteSpawnedProcessCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            var packet = new SpawnFinalizationPacket()
            {
                SpawnId = spawnId,
                FinalizationData = finalizationData
            };

            connection.SendMessage((short) MsfOpCodes.CompleteSpawnProcess, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown Error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        /// <summary>
        /// This should be called from a process, which is spawned.
        /// For example, it can be called from a game server, which is started by the spawner
        /// On successfull registration, callback contains <see cref="SpawnTaskController"/>, which 
        /// has a dictionary of properties, that were given when requesting a process to be spawned
        /// </summary>
        /// <param name="spawnId"></param>
        /// <param name="spawnCode"></param>
        /// <param name="callback"></param>
        public void RegisterSpawnedProcess(int spawnId, string spawnCode, RegisterSpawnedProcessCallback callback)
        {
            RegisterSpawnedProcess(spawnId, spawnCode, callback, Connection);
        }

        /// <summary>
        /// This should be called from a process which is spawned.
        /// For example, it can be called from a game server, which is started by the spawner
        /// On successfull registration, callback contains <see cref="SpawnTaskController"/>, which 
        /// has a dictionary of properties, that were given when requesting a process to be spawned
        /// </summary>
        public void RegisterSpawnedProcess(int spawnId, string spawnCode, RegisterSpawnedProcessCallback callback, 
            IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            var packet = new RegisterSpawnedProcessPacket()
            {
                SpawnCode = spawnCode,
                SpawnId = spawnId
            };

            connection.SendMessage((short)MsfOpCodes.RegisterSpawnedProcess, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown Error"));
                    return;
                }

                var properties = new Dictionary<string, string>().FromBytes(response.AsBytes());

                var process = new SpawnTaskController(spawnId, properties, connection);

                callback.Invoke(process, null);
            });
        }

        /// <summary>
        /// Notifies master server, how many processes are running on a specified spawner
        /// </summary>
        public void UpdateProcessesCount(int spawnerId, int count)
        {
            UpdateProcessesCount(spawnerId, count, Connection);
        }

        /// <summary>
        /// Notifies master server, how many processes are running on a specified spawner
        /// </summary>
        public void UpdateProcessesCount(int spawnerId, int count, IClientSocket connection)
        {
            var packet = new IntPairPacket()
            {
                A = spawnerId,
                B = count
            };
            connection.SendMessage((short)MsfOpCodes.UpdateSpawnerProcessesCount, packet);
        }

        /// <summary>
        /// Should be called by a spawned process, as soon as it is started
        /// 
        /// </summary>
        /// <param name="spawnId"></param>
        /// <param name="processId"></param>
        /// <param name="cmdArgs"></param>
        public void NotifyProcessStarted(int spawnId, int processId, string cmdArgs)
        {
            NotifyProcessStarted(spawnId, processId, cmdArgs, Connection);
        }

        public void NotifyProcessStarted(int spawnId, int processId, string cmdArgs, IClientSocket connection)
        {
            if (!connection.IsConnected)
                return;

            connection.SendMessage((short) MsfOpCodes.ProcessStarted, new SpawnedProcessStartedPacket()
            {
                CmdArgs = cmdArgs,
                ProcessId = processId,
                SpawnId = spawnId
            });
        }

        public void NotifyProcessKilled(int spawnId)
        {
            NotifyProcessKilled(spawnId, Connection);
        }

        public void NotifyProcessKilled(int spawnId, IClientSocket connection)
        {
            if (!connection.IsConnected)
                return;

            connection.SendMessage((short)MsfOpCodes.ProcessKilled, spawnId);
        }

        public SpawnerController GetController(int spawnerId)
        {
            SpawnerController controller;
            _locallyCreatedSpawners.TryGetValue(spawnerId, out controller);

            return controller;
        }

        public IEnumerable<SpawnerController> GetLocallyCreatedSpawners()
        {
            return _locallyCreatedSpawners.Values;
        }

        public int GetAvailablePort()
        {
            // Return a port from a list of available ports
            if (_freePorts.Count > 0)
                return _freePorts.Dequeue();

            if (_lastPortTaken < 0)
                _lastPortTaken = PortsStartFrom;

            return _lastPortTaken++;
        }

        public void ReleasePort(int port)
        {
            _freePorts.Enqueue(port);
        }

    }

    public class SpawnTaskController
    {
        private readonly IClientSocket _connection;
        public int SpawnId { get; private set; }
        public Dictionary<string, string> Properties { get; private set; }

        public SpawnTaskController(int spawnId, Dictionary<string, string> properties, IClientSocket connection)
        {
            _connection = connection;
            SpawnId = spawnId;
            Properties = properties;
        }

        public void FinalizeTask()
        {
            FinalizeTask(new Dictionary<string, string>(), () => { });
        }

        public void FinalizeTask(Dictionary<string, string> finalizationData)
        {
            FinalizeTask(finalizationData, () => { });
        }

        public void FinalizeTask(Dictionary<string, string> finalizationData, Action callback)
        {
            Msf.Server.Spawners.FinalizeSpawnedProcess(SpawnId, finalizationData, (successful, error) =>
            {
                if (error != null)
                    Logs.Error("Error while completing the spawn task: " + error);

                callback.Invoke();
            }, _connection);
        }
    }
}