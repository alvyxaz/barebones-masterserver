using System;
using Barebones.Logging;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;
using UnityEngine.Networking;

public class UnetServerStarter : MonoBehaviour
{
    #region Unity's inspector

    public HelpBox _header = new HelpBox()
    {
        Text = "Starts the Unet server if conditions are met.",
        Type = HelpBoxType.Info
    };

    public bool WarnIfNoConnectionObject = true;

    public LogLevel LogLevel = LogLevel.Info;

    public NetworkManager NetworkManager;

    [Header("If Spawned Process")]
    [Tooltip("If set to true and if this process is spawned, it will try to " +
             "automatically start the game server. ")]
    public bool AutoStartSpawned = true;

    [Header("If Testing In Editor")]
    public bool AutoStartInEditor = true;
    public bool StartServerAsHost = true;
    public bool AutoJoinRoom = true;
    public bool StartMaster = true;
    public MasterServerBehaviour MasterServerObject;

    #endregion

    protected bool IsStartingEditorServer;

    public BmLogger Logger = Msf.Create.Logger(typeof(UnetServerStarter).Name);

    protected virtual void Awake()
    {
        Logger.LogLevel = LogLevel;

        MasterServerObject = MasterServerObject ?? FindObjectOfType<MasterServerBehaviour>();
        NetworkManager = NetworkManager ?? FindObjectOfType<NetworkManager>();

        var connection = GetConnection();

        // Listen to the connected event
        connection.AddConnectionListener(OnConnectedToMaster, true);

        if (WarnIfNoConnectionObject && FindObjectOfType<ConnectionToMaster>() == null)
        {
            Logger.Warn("No connection object was found in the scene. Ignore the warning, if you're connecting " +
                        "to server manually.");
        }

        // Listen to when a room is registered
        Msf.Server.Rooms.RoomRegistered += OnRoomRegistered;
    }

    protected virtual void Start()
    {
        if (ShouldStartServerInEditor() && StartMaster)
        {
            // If we need to start the master server
            if (MasterServerObject == null)
            {
                Logger.Error("You have selected to start a master server, but there's no " +
                             "master server object in the scene");
                return;
            }

            // Enable master server object
            MasterServerObject.gameObject.SetActive(true);

            // If auto start in editor is not selected
            if (!MasterServerObject.AutoStartInEditor)
                MasterServerObject.StartServer();
        }
    }
    
    protected virtual void OnConnectedToMaster()
    {
        // Start the server if it's a spawned process
        if (AutoStartSpawned && Msf.Server.Spawners.IsSpawnedProccess)
        {
            // If this is a spawned process, and we want the game server to be started automatically
            StartSpawned();
            return;
        }
        
        if (ShouldStartServerInEditor())
        {
            IsStartingEditorServer = true;

            // If we haven't received any access yet, we assume that we wan't to start a game server
            Logs.Info("AutoStartInEditor set to true and there's no Room access - " +
                      "we're assuming that you want to start a game server in editor.");

            StartInEditor();
            return;
        }
    }

    protected virtual bool ShouldStartServerInEditor()
    {
        return !Msf.Client.Rooms.ForceClientMode 
            && Msf.Runtime.IsEditor
               && AutoStartInEditor
               && Msf.Client.Rooms.LastReceivedAccess == null;
    }

    protected virtual void StartSpawned()
    {
        Msf.Server.Spawners.RegisterSpawnedProcess(Msf.Args.SpawnId, Msf.Args.SpawnCode, (controller, error) =>
        {
            if (controller == null)
            {
                Logger.Error("Failed to register a spawned process: " + error);
                throw new Exception("Failed to register a spawned process: " + error);
            }

            // Set the static object, so that it can be used when creating a room
            UnetGameRoom.SpawnTaskController = controller;

            if (Msf.Args.IsProvided(Msf.Args.Names.WebGl))
                NetworkManager.useWebSockets = true;

            // Use the assigned port from cmd args
            NetworkManager.networkPort = Msf.Args.AssignedPort;

            // Start the server
            NetworkManager.StartServer();
        });
        return;
    }

    protected virtual void StartInEditor()
    {
        // Connections will be managed by room accesses
        // Maximize this just in case
        NetworkManager.maxConnections = 999;

        if (StartServerAsHost)
        {
            NetworkManager.StartHost();
        }
        else
        {
            NetworkManager.StartServer();
        }
    }

    /// <summary>
    /// This will be called when a room is created and registered to "master"
    /// </summary>
    protected virtual void OnRoomRegistered(RoomController controller)
    {
        if (IsStartingEditorServer && AutoJoinRoom)
        {
            //-------------------------
            // 1. Log into the server
            Logger.Debug("Logging in as guest...");
            Msf.Client.Auth.LogInAsGuest((accInfo, loginError) =>
            {
                if (accInfo == null)
                {
                    Logger.Error("Failed to log in: " + loginError);
                    return;
                }

                Logger.Debug("Logged in successfully");

                //-------------------------
                // 2. Get access to join the room
                Logger.Debug("Retrieving room access ...");
                Msf.Client.Rooms.GetAccess(controller.RoomId, (access, accessError) =>
                {
                    if (access == null)
                    {
                        Logger.Error("Failed to get the access to server: " + accessError);
                        return;
                    }

                    // We have the access, try to connect to room
                    Logger.Debug("Access received: " + access);

                    if (RoomConnector.Instance == null)
                    {
                        Logger.Warn("RoomConnector was not found in the scene. Hopefully, " +
                                    "you handle the  'Msf.Client.Rooms.AccessReceived' " +
                                    "event manually.");
                    }

                });
            });
        }
    }

    protected virtual IClientSocket GetConnection()
    {
        return Msf.Connection;
    }

    void OnDestroy()
    {
        Msf.Server.Rooms.RoomRegistered -= OnRoomRegistered;

        // Remove listeners
        GetConnection().RemoveConnectionListener(OnConnectedToMaster);
    }
}
