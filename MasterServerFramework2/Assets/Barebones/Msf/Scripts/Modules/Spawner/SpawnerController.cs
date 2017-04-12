using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Barebones.Logging;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class SpawnerController
    {
        public delegate void SpawnRequestHandler(SpawnRequestPacket packet, IIncommingMessage message);
        public delegate void KillSpawnedProcessHandler(int spawnId);

        public readonly IClientSocket Connection;

        public int SpawnerId { get; set; }
        public SpawnerOptions Options { get; private set; }

        private SpawnRequestHandler _spawnRequestHandler;
        private KillSpawnedProcessHandler _killRequestHandler;

        /// <summary>
        /// Settings, which are used by the default spawn handler
        /// </summary>
        [Obsolete("Use DefaultSpawnerSettings")]
        public DefaultSpawnerConfig Settings { get { return DefaultSpawnerSettings; } }

        /// <summary>
        /// Settings, which are used by the default spawn handler
        /// </summary>
        public DefaultSpawnerConfig DefaultSpawnerSettings { get; private set; }

        #region Default process spawn handling

        public static BmLogger Logger = Msf.Create.Logger(typeof(SpawnerController).Name, LogLevel.Warn);
        private static object _processLock = new object();
        private static Dictionary<int, Process> _processes = new Dictionary<int, Process>();

        #endregion

        public SpawnerController(int spawnerId, IClientSocket connection, SpawnerOptions options)
        {
            Connection = connection;
            SpawnerId = spawnerId;
            Options = options;

            DefaultSpawnerSettings = new DefaultSpawnerConfig()
            {
                MasterIp = connection.ConnectionIp,
                MasterPort = connection.ConnectionPort,
                MachineIp = options.MachineIp,
                SpawnInBatchmode = Msf.Args.IsProvided("-batchmode")
            };

            // Add handlers
            connection.SetHandler((short) MsfOpCodes.SpawnRequest, HandleSpawnRequest);
            connection.SetHandler((short) MsfOpCodes.KillSpawnedProcess, HandleKillSpawnedProcessRequest);
        }

        public void SetSpawnRequestHandler(SpawnRequestHandler handler)
        {
            _spawnRequestHandler = handler;
        }

        public void SetKillRequestHandler(KillSpawnedProcessHandler handler)
        {
            _killRequestHandler = handler;
        }

        public void NotifyProcessStarted(int spawnId, int processId, string cmdArgs)
        {
            Msf.Server.Spawners.NotifyProcessStarted(spawnId, processId, cmdArgs, Connection);
        }

        public void NotifyProcessKilled(int spawnId)
        {
            Msf.Server.Spawners.NotifyProcessKilled(spawnId);
        }

        public void UpdateProcessesCount(int count)
        {
            Msf.Server.Spawners.UpdateProcessesCount(SpawnerId, count, Connection);
        }

        private void HandleSpawnRequest(SpawnRequestPacket packet, IIncommingMessage message)
        {
            if (_spawnRequestHandler == null)
            {
                DefaultSpawnRequestHandler(packet, message);
                return;
            }

            _spawnRequestHandler.Invoke(packet, message);
        }

        private void HandleKillSpawnedProcessRequest(int spawnId)
        {
            if (_killRequestHandler == null)
            {
                DefaultKillRequestHandler(spawnId);
                return;
            }

            _killRequestHandler.Invoke(spawnId);
        }

        private static void HandleSpawnRequest(IIncommingMessage message)
        {
            var data = message.Deserialize(new SpawnRequestPacket());

            var controller = Msf.Server.Spawners.GetController(data.SpawnerId);

            if (controller == null)
            {
                if (message.IsExpectingResponse) 
                    message.Respond("Couldn't find a spawn controller", ResponseStatus.NotHandled);
                return;
            }

            // Pass the request to handler
            controller.HandleSpawnRequest(data, message);
        }

        private static void HandleKillSpawnedProcessRequest(IIncommingMessage message)
        {
            var data = message.Deserialize(new KillSpawnedProcessPacket());

            var controller = Msf.Server.Spawners.GetController(data.SpawnerId);

            if (controller == null)
            {
                if (message.IsExpectingResponse)
                    message.Respond("Couldn't find a spawn controller", ResponseStatus.NotHandled);
                return;
            }

            controller.HandleKillSpawnedProcessRequest(data.SpawnId);
        }

        #region Default handlers

        public static void DefaultKillRequestHandler(int spawnId)
        {
            Logger.Debug("Default kill request handler started handling a request to kill a process");

            try
            {
                Process process;

                lock (_processLock)
                {
                    _processes.TryGetValue(spawnId, out process);
                    _processes.Remove(spawnId);
                }

                if (process != null)
                    process.Kill();
            }
            catch (Exception e)
            {
                Logger.Error("Got error while killing a spawned process");
                Logger.Error(e);
            }
        }

        public void DefaultSpawnRequestHandler(SpawnRequestPacket packet, IIncommingMessage message)
        {
            Logger.Debug("Default spawn handler started handling a request to spawn process");

            var controller = Msf.Server.Spawners.GetController(packet.SpawnerId);

            if (controller == null)
            {
                message.Respond("Failed to spawn a process. Spawner controller not found", ResponseStatus.Failed);
                return;
            }

            var port = Msf.Server.Spawners.GetAvailablePort();

            // Check if we're overriding an IP to master server
            var masterIp = string.IsNullOrEmpty(controller.DefaultSpawnerSettings.MasterIp) ?
                controller.Connection.ConnectionIp : controller.DefaultSpawnerSettings.MasterIp;

            // Check if we're overriding a port to master server
            var masterPort = controller.DefaultSpawnerSettings.MasterPort < 0 ?
                controller.Connection.ConnectionPort : controller.DefaultSpawnerSettings.MasterPort;

            // Machine Ip
            var machineIp = controller.DefaultSpawnerSettings.MachineIp; 

            // Path to executable
            var path = controller.DefaultSpawnerSettings.ExecutablePath;
            if (string.IsNullOrEmpty(path))
            {
                path = File.Exists(Environment.GetCommandLineArgs()[0]) 
                    ? Environment.GetCommandLineArgs()[0] 
                    : Process.GetCurrentProcess().MainModule.FileName;
            }

            // In case a path is provided with the request
            if (packet.Properties.ContainsKey(MsfDictKeys.ExecutablePath))
                path = packet.Properties[MsfDictKeys.ExecutablePath];

            // Get the scene name
            var sceneNameArgument = packet.Properties.ContainsKey(MsfDictKeys.SceneName)
                ? string.Format("{0} {1} ", Msf.Args.Names.LoadScene, packet.Properties[MsfDictKeys.SceneName])
                : "";

            if (!string.IsNullOrEmpty(packet.OverrideExePath))
            {
                path = packet.OverrideExePath;
            }

            // If spawn in batchmode was set and `DontSpawnInBatchmode` arg is not provided
            var spawnInBatchmode = controller.DefaultSpawnerSettings.SpawnInBatchmode
                                   && !Msf.Args.DontSpawnInBatchmode;

            var startProcessInfo = new ProcessStartInfo(path)
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                Arguments = " " +
                    (spawnInBatchmode ? "-batchmode -nographics " : "") +
                    (controller.DefaultSpawnerSettings.AddWebGlFlag ? Msf.Args.Names.WebGl+" " : "") +
                    sceneNameArgument +
                    string.Format("{0} {1} ", Msf.Args.Names.MasterIp, masterIp) +
                    string.Format("{0} {1} ", Msf.Args.Names.MasterPort, masterPort) +
                    string.Format("{0} {1} ", Msf.Args.Names.SpawnId, packet.SpawnId) +
                    string.Format("{0} {1} ", Msf.Args.Names.AssignedPort, port) +
                    string.Format("{0} {1} ", Msf.Args.Names.MachineIp, machineIp) +
                    (Msf.Args.DestroyUi ? Msf.Args.Names.DestroyUi + " " : "") +
                    string.Format("{0} \"{1}\" ", Msf.Args.Names.SpawnCode, packet.SpawnCode) +
                    packet.CustomArgs
            };

            Logger.Debug("Starting process with args: " + startProcessInfo.Arguments);

            var processStarted = false;

            try
            {
                new Thread(() =>
                {
                    try
                    {
                        Logger.Debug("New thread started");

                        using (var process = Process.Start(startProcessInfo))
                        {
                            Logger.Debug("Process started. Spawn Id: " + packet.SpawnId + ", pid: " + process.Id);
                            processStarted = true;

                            lock (_processLock)
                            {
                                // Save the process
                                _processes[packet.SpawnId] = process;
                            }

                            var processId = process.Id;

                            // Notify server that we've successfully handled the request
                            BTimer.ExecuteOnMainThread(() =>
                            {
                                message.Respond(ResponseStatus.Success);
                                controller.NotifyProcessStarted(packet.SpawnId, processId, startProcessInfo.Arguments);
                            });

                            process.WaitForExit();
                        }
                    }
                    catch (Exception e)
                    {
                        if (!processStarted)
                            BTimer.ExecuteOnMainThread(() => { message.Respond(ResponseStatus.Failed); });

                        Logger.Error("An exception was thrown while starting a process. Make sure that you have set a correct build path. " +
                                     "We've tried to start a process at: '" + path+"'. You can change it at 'SpawnerBehaviour' component");
                        Logger.Error(e);
                    }
                    finally
                    {
                        lock (_processLock)
                        {
                            // Remove the process
                            _processes.Remove(packet.SpawnId);
                        }

                        BTimer.ExecuteOnMainThread(() =>
                        {
                            // Release the port number
                            Msf.Server.Spawners.ReleasePort(port);

                            Logger.Debug("Notifying about killed process with spawn id: " + packet.SpawnerId);
                            controller.NotifyProcessKilled(packet.SpawnId);
                        });
                    }

                }).Start();
            }
            catch (Exception e)
            {
                message.Respond(e.Message, ResponseStatus.Error);
                Logs.Error(e);
            }
        }

        public static void KillProcessesSpawnedWithDefaultHandler()
        {
            var list = new List<Process>();
            lock (_processLock)
            {
                foreach (var process in _processes.Values)
                {
                    list.Add(process);
                }
            }

            foreach (var process in list)
            {
                process.Kill();
            }
        }

        public class DefaultSpawnerConfig
        {
            public string MachineIp = "127.0.0.1";
            public bool SpawnInBatchmode = Msf.Args.IsProvided("-batchmode");

            [Obsolete("Use `SpawnInBatchmode`")]
            public bool RunInBatchmode {get { return SpawnInBatchmode; } set { SpawnInBatchmode = value; } }
            public string MasterIp = "";
            public int MasterPort = -1;
            public string ExecutablePath = "";
            public bool AddWebGlFlag = false;
        }

        #endregion
    }
}