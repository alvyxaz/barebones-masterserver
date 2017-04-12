using Barebones.Logging;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;

public class SpawnerBehaviour : MonoBehaviour
{
    public HelpBox _headerEditor = new HelpBox()
    {
        Text = "This creates and registers a spawner, which can spawn " +
               "game servers and other processes",
        Type = HelpBoxType.Info
    };

    public HelpBox _headerWarn = new HelpBox()
    {
        Text = string.Format("It will start ONLY if '{0}' argument is found, or " +
                             "if StartSpawner() is called manually from your scripts", Msf.Args.Names.StartSpawner),
        Type = HelpBoxType.Warning
    };

    [Header("General")]
    [Tooltip("Log level of this script's logger")]
    public LogLevel LogLevel = LogLevel.Info;
    [Tooltip("Log level of internal SpawnerController logger")]
    public LogLevel SpawnControllerLogLevel = LogLevel.Warn;

    protected IClientSocket _connection;

    [Header("Spawner Settings")]
    public string DefaultMachineIp = "127.0.0.1";
    public string DefaultExePath = "";
    public bool DefaultSpawnInBatchmode = false;
    public int MaxSpawnedProcesses = 5;
    public bool SpawnWebglServers = false;

    [Tooltip("If true, and if spawner is started - this game object will not be destroyed when " +
             "switching scenes")]
    public bool DontDestroyIfStarted = true;

    public bool KillProcessesWhenSpawnerQuits = true;

    public bool IsSpawnerStarted { get; set; }
    protected SpawnerBehaviour Instance;

    public BmLogger Logger = Msf.Create.Logger(typeof(SpawnerBehaviour).Name);

    [Header("Running in Editor")]
    [Tooltip("If true, when running in editor, spawner server will start automatically (after connecting to master)")]
    public bool AutoStartInEditor = true;
    [Tooltip("If true, and if running in editor, path to executable will be overriden, and a value from 'ExePathFromEditor' " +
             "will be used.")]
    public bool OverrideExePathInEditor = true;
    public string ExePathFromEditor = "C:/Please set your own path";

    protected SpawnerController SpawnerController;

    protected virtual void Awake()
    {
        // Only allow one spawner behaviour in the scene
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    protected virtual void Start()
    {
        Logger.LogLevel = LogLevel;
        SpawnerController.Logger.LogLevel = SpawnControllerLogLevel;

        _connection = GetConnection();

        // Subscribe to connection event
        _connection.AddConnectionListener(OnConnectedToMaster, true);
    }

    protected virtual void OnConnectedToMaster()
    {
        // If we want to start a spawner (cmd argument was found)
        if (Msf.Args.IsProvided(Msf.Args.Names.StartSpawner))
        {
            StartSpawner();
            return;
        }

        if (AutoStartInEditor && Msf.Runtime.IsEditor)
        {
            StartSpawner();
        }
    }

    public virtual void StartSpawner()
    {
        // In case we went from one scene to another, but we've already started the spawner
        if (IsSpawnerStarted)
            return;

        if (DontDestroyIfStarted)
        {
            // Move to hierarchy root, so that we can't destroy it
            if (transform.parent != null)
                transform.SetParent(null, false);

            // Make sure this object is not destroyed 
            DontDestroyOnLoad(gameObject);
        }

        IsSpawnerStarted = true;

        var spawnerOptions = new SpawnerOptions();

        spawnerOptions.MaxProcesses = MaxSpawnedProcesses;
        if (Msf.Args.IsProvided(Msf.Args.Names.MaxProcesses))
            spawnerOptions.MaxProcesses = Msf.Args.MaxProcesses;

        // If we're running in editor, and we want to override the executable path
        if (Msf.Runtime.IsEditor && OverrideExePathInEditor)
            DefaultExePath = ExePathFromEditor;

        Logger.Info("Registering as a spawner with options: \n" + spawnerOptions);

        // 1. Register the spawner
        Msf.Server.Spawners.RegisterSpawner(spawnerOptions, (spawner, error) =>
        {
            if (error != null)
            {
                Logger.Error("Failed to create spawner: " + error);
                return;
            }

            SpawnerController = spawner;

            spawner.DefaultSpawnerSettings.AddWebGlFlag = Msf.Args.IsProvided(Msf.Args.Names.WebGl)
                ? Msf.Args.WebGl
                : SpawnWebglServers;

            // Set to run in batchmode
            if (DefaultSpawnInBatchmode && ! Msf.Args.DontSpawnInBatchmode)
                spawner.DefaultSpawnerSettings.SpawnInBatchmode = true;

            // 2. Set the default executable path
            spawner.DefaultSpawnerSettings.ExecutablePath = Msf.Args.IsProvided(Msf.Args.Names.ExecutablePath) ? 
                Msf.Args.ExecutablePath : DefaultExePath;

            // 3. Set the machine IP
            spawner.DefaultSpawnerSettings.MachineIp = Msf.Args.IsProvided(Msf.Args.Names.MachineIp) ?
                Msf.Args.MachineIp : DefaultMachineIp;

            // 4. (Optional) Set the method which does the spawning, if you want to
            // fully control how processes are spawned
            spawner.SetSpawnRequestHandler(HandleSpawnRequest);

            // 5. (Optional) Set the method, which kills processes when kill request is received
            spawner.SetKillRequestHandler(HandleKillRequest);

            Logger.Info("Spawner successfully created. Id: " + spawner.SpawnerId);
        });
    }

    private void HandleKillRequest(int spawnid)
    {
        SpawnerController.DefaultKillRequestHandler(spawnid);
    }

    protected virtual void HandleSpawnRequest(SpawnRequestPacket packet, IIncommingMessage message)
    {
        SpawnerController.DefaultSpawnRequestHandler(packet, message);
    }

    protected virtual IClientSocket GetConnection()
    {
        return Msf.Connection;
    }

    protected virtual void OnApplicationQuit()
    {
        if (KillProcessesWhenSpawnerQuits)
        {
            SpawnerController.KillProcessesSpawnedWithDefaultHandler();
        }
    }

    protected virtual void OnDestroy()
    {
        // Remove listener
        _connection.RemoveConnectionListener(OnConnectedToMaster);
    }
}
