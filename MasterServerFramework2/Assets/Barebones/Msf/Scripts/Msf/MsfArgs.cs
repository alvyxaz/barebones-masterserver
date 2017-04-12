using System;
using System.Linq;

namespace Barebones.MasterServer
{
    public class MsfArgs
    {
        private readonly string[] _args;

        public MsfArgNames Names;

        public MsfArgs()
        {
            _args = Environment.GetCommandLineArgs();

            // Android fix
            if (_args == null)
                _args = new string[0];

            Names = new MsfArgNames();

            StartMaster = IsProvided(Names.StartMaster);
            MasterPort = ExtractValueInt(Names.MasterPort, 5000);
            MasterIp = ExtractValue(Names.MasterIp);
            MachineIp = ExtractValue(Names.MachineIp);
            DestroyUi = IsProvided(Names.DestroyUi);

            SpawnId = ExtractValueInt(Names.SpawnId, -1);
            AssignedPort = ExtractValueInt(Names.AssignedPort, -1);
            SpawnCode = ExtractValue(Names.SpawnCode);
            ExecutablePath = ExtractValue(Names.ExecutablePath);
            DontSpawnInBatchmode = IsProvided(Names.DontSpawnInBatchmode);
            MaxProcesses = ExtractValueInt(Names.MaxProcesses, 0);

            LoadScene = ExtractValue(Names.LoadScene);

            DbConnectionString = ExtractValue(Names.DbConnectionString);

            LobbyId = ExtractValueInt(Names.LobbyId);
            WebGl = IsProvided(Names.WebGl);
            
        }

        #region Arguments

        /// <summary>
        /// If true, master server should be started
        /// </summary>
        public bool StartMaster { get; private set; }

        /// <summary>
        /// Port, which will be open on the master server
        /// </summary>
        public int MasterPort { get; private set; }

        /// <summary>
        /// Ip address to the master server
        /// </summary>
        public string MasterIp { get; private set; }

        /// <summary>
        /// Public ip of the machine, on which the process is running
        /// </summary>
        public string MachineIp { get; private set; }

        /// <summary>
        /// If true, some of the Ui game objects will be destroyed.
        /// (to avoid memory leaks)
        /// </summary>
        public bool DestroyUi { get; private set; }


        /// <summary>
        /// SpawnId of the spawned process
        /// </summary>
        public int SpawnId { get; private set; }

        /// <summary>
        /// Port, assigned to the spawned process (most likely a game server)
        /// </summary>
        public int AssignedPort { get; private set; }

        /// <summary>
        /// Code, which is used to ensure that there's no tampering with 
        /// spawned processes
        /// </summary>
        public string SpawnCode { get; private set; }

        /// <summary>
        /// Path to the executable (used by the spawner)
        /// </summary>
        public string ExecutablePath { get; private set; }

        /// <summary>
        /// If true, will make sure that spawned processes are not spawned in batchmode
        /// </summary>
        public bool DontSpawnInBatchmode { get; private set; }

        /// <summary>
        /// Max number of processes that can be spawned by a spawner
        /// </summary>
        public int MaxProcesses { get; private set; }

        /// <summary>
        /// Name of the scene to load
        /// </summary>
        public string LoadScene { get; private set; }

        /// <summary>
        /// Database connection string (user by some of the database implementations)
        /// </summary>
        public string DbConnectionString { get; private set; }
        
        /// <summary>
        /// LobbyId, which is assigned to a spawned process
        /// </summary>
        public int LobbyId { get; private set; }

        /// <summary>
        /// If true, it will be considered that we want to start server to
        /// support webgl clients
        /// </summary>
        public bool WebGl { get; private set; }

        #endregion

        #region Helper methods

        /// <summary>
        ///     Extracts a value for command line arguments provided
        /// </summary>
        /// <param name="argName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string ExtractValue(string argName, string defaultValue = null)
        {
            if (!_args.Contains(argName))
                return defaultValue;

            var index = _args.ToList().FindIndex(0, a => a.Equals(argName));
            return _args[index + 1];
        }

        public int ExtractValueInt(string argName, int defaultValue = -1)
        {
            var number = ExtractValue(argName, defaultValue.ToString());
            return Convert.ToInt32(number);
        }

        public bool IsProvided(string argName)
        {
            return _args.Contains(argName);
        }

        #endregion

        public class MsfArgNames
        {
            public string StartMaster { get { return "-msfStartMaster"; } }
            public string MasterPort { get { return "-msfMasterPort"; } }
            public string MasterIp { get { return "-msfMasterIp"; } }

            public string StartSpawner { get { return "-msfStartSpawner"; } }

            public string SpawnId { get { return "-msfSpawnId"; } }
            public string SpawnCode { get { return "-msfSpawnCode"; } }
            public string AssignedPort { get { return "-msfAssignedPort"; } }
            public string LoadScene { get { return "-msfLoadScene"; } }
            public string MachineIp { get { return "-msfMachineIp"; } }
            public string ExecutablePath { get { return "-msfExe"; } }
            public string DbConnectionString { get { return "-msfDbConnectionString"; } }
            public string LobbyId { get { return "-msfLobbyId"; } }
            public string DontSpawnInBatchmode { get { return "-msfDontSpawnInBatchmode"; } }
            public string MaxProcesses { get { return "-msfMaxProcesses"; } }
            public string DestroyUi { get { return "-msfDestroyUi"; } }
            public string WebGl { get { return "-msfWebgl"; } }
        }
    }
}