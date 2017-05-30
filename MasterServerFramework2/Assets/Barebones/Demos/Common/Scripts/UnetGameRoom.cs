using System;
using System.Collections;
using System.Collections.Generic;
using Barebones.Logging;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// This script automatically creates a room in "master" server,
/// when <see cref="OnStartServer"/> is called (most likely by Network Manager
/// , when server is started).
/// 
/// After room is created, it also checks if this game server was "spawned", and 
/// if so - it finalizes the spawn task
/// </summary>
public class UnetGameRoom : NetworkBehaviour
{
    public static SpawnTaskController SpawnTaskController;

    /// <summary>
    /// Unet msg type 
    /// </summary>
    public static short AccessMsgType = 3000;
    
    public HelpBox _header = new HelpBox()
    {
        Text = "Waits for the Unet game server to start," +
               "and then automatically creates a Room for it " +
               "(registers server to 'Master').",
        Type = HelpBoxType.Info
    };

    [Header("General")]
    public LogLevel LogLevel = LogLevel.Warn;

    [Header("Room options")]
    [Tooltip("This address will be sent to clients with an access token")]
    public string PublicIp = "xxx.xxx.xxx.xxx";
    public string Name = "Room Name";
    public int MaxPlayers = 5;
    public bool IsPublic = true;
    public string Password = "";
    public bool AllowUsersRequestAccess = true;

    [Header("Room properties")]
    public string MapName = "Amazing Map";

    [Header("Other")]
    public bool QuitAppIfDisconnected = true;

    public BmLogger Logger = Msf.Create.Logger(typeof(UnetGameRoom).Name);

    protected Dictionary<int, UnetMsfPlayer> PlayersByPeerId;
    protected Dictionary<string, UnetMsfPlayer> PlayersByUsername;
    protected Dictionary<int, UnetMsfPlayer> PlayersByConnectionId;

    public event Action<UnetMsfPlayer> PlayerJoined;
    public event Action<UnetMsfPlayer> PlayerLeft;

    public NetworkManager NetworkManager;

    public RoomController Controller;

    protected virtual void Awake()
    {
        NetworkManager = NetworkManager ?? FindObjectOfType<NetworkManager>();

        Logger.LogLevel = LogLevel;

        PlayersByPeerId = new Dictionary<int, UnetMsfPlayer>();
        PlayersByUsername = new Dictionary<string, UnetMsfPlayer>();
        PlayersByConnectionId = new Dictionary<int, UnetMsfPlayer>();

        NetworkServer.RegisterHandler(AccessMsgType, HandleReceivedAccess);

        Msf.Server.Rooms.Connection.Disconnected += OnDisconnectedFromMaster;
    }

    public bool IsRoomRegistered { get; protected set; }

    /// <summary>
    /// This will be called, when game server starts
    /// </summary>
    public override void OnStartServer()
    {
        // Find the manager, in case it was inaccessible on awake
        NetworkManager = NetworkManager ?? FindObjectOfType<NetworkManager>();

        // The Unet server is started, we need to register a Room
        BeforeRegisteringRoom();
        RegisterRoom();
    }

    /// <summary>
    /// This method is called before creating a room. It can be used to
    /// extract some parameters from cmd args or from span task properties
    /// </summary>
    protected virtual void BeforeRegisteringRoom()
    {
        if (SpawnTaskController != null)
        {
            Logger.Debug("Reading spawn task properties to override some of the room options");

            // If this server was spawned, try to read some of the properties
            var prop = SpawnTaskController.Properties;

            // Room name
            if (prop.ContainsKey(MsfDictKeys.RoomName))
                Name = prop[MsfDictKeys.RoomName];

            if (prop.ContainsKey(MsfDictKeys.MaxPlayers))
                MaxPlayers = int.Parse(prop[MsfDictKeys.MaxPlayers]);

            if (prop.ContainsKey(MsfDictKeys.RoomPassword))
                Password = prop[MsfDictKeys.RoomPassword];

            if (prop.ContainsKey(MsfDictKeys.MapName))
                MapName = prop[MsfDictKeys.MapName];
        }

        // Override the public address
        if (Msf.Args.IsProvided(Msf.Args.Names.MachineIp) && NetworkManager != null)
        {
            PublicIp = Msf.Args.MachineIp;
            Logger.Debug("Overriding rooms public IP address to: " + PublicIp);
        }
    }

    public virtual void RegisterRoom()
    {
        var isUsingLobby = Msf.Args.IsProvided(Msf.Args.Names.LobbyId);

        var properties = SpawnTaskController != null 
            ? SpawnTaskController.Properties 
            : new Dictionary<string, string>();

        if (!properties.ContainsKey(MsfDictKeys.MapName))
            properties[MsfDictKeys.MapName] = MapName;

        properties[MsfDictKeys.SceneName] = SceneManager.GetActiveScene().name;

        // 1. Create options object
        var options = new RoomOptions()
        {
            RoomIp = PublicIp,
            RoomPort = NetworkManager.networkPort,
            Name = Name,
            MaxPlayers = MaxPlayers,

            // Lobby rooms should be private, because they are accessed differently
            IsPublic = isUsingLobby ? false : IsPublic,
            AllowUsersRequestAccess = isUsingLobby ? false : AllowUsersRequestAccess,

            Password = Password,

            Properties = new Dictionary<string, string>()
            {
                {MsfDictKeys.MapName, MapName }, // Show the name of the map
                {MsfDictKeys.SceneName, SceneManager.GetActiveScene().name} // Add the scene name
            }
        };

        BeforeSendingRegistrationOptions(options, properties);

        // 2. Send a request to create a room
        Msf.Server.Rooms.RegisterRoom(options, (controller, error) =>
        {
            if (controller == null)
            {
                Logger.Error("Failed to create a room: " + error);
                return;
            }

            // Save the controller
            Controller = controller;

            Logger.Debug("Room Created successfully. Room ID: " + controller.RoomId);

            OnRoomRegistered(controller);
        });
    }

    /// <summary>
    /// Override this method, if you want to make some changes to registration options
    /// </summary>
    /// <param name="options">Room options, before sending them to register a room</param>
    /// <param name="spawnProperties">Properties, which were provided when spawning the process</param>
    protected virtual void BeforeSendingRegistrationOptions(RoomOptions options, 
        Dictionary<string, string> spawnProperties)
    {
        // You can override this method, and modify room registration options

        // For example, you could copy some of the properties from spawn request,
        // like this:
        if (spawnProperties.ContainsKey("magicProperty"))
            options.Properties["magicProperty"] = spawnProperties["magicProperty"];
    }

    /// <summary>
    /// Called when room is registered to the "master server"
    /// </summary>
    /// <param name="roomController"></param>
    public void OnRoomRegistered(RoomController roomController)
    {
        IsRoomRegistered = true;

        // Set access provider (Optional)
        roomController.SetAccessProvider(CreateAccess);

        // If this room was spawned
        if (SpawnTaskController != null)
            SpawnTaskController.FinalizeTask(CreateSpawnFinalizationData());
    }

    /// <summary>
    /// Override, if you want to manually handle creation of access'es
    /// </summary>
    /// <param name="callback"></param>
    public virtual void CreateAccess(UsernameAndPeerIdPacket requester, RoomAccessProviderCallback callback)
    {
        callback.Invoke(new RoomAccessPacket()
        {
            RoomIp = Controller.Options.RoomIp,
            RoomPort = Controller.Options.RoomPort,
            Properties = Controller.Options.Properties,
            RoomId = Controller.RoomId,
            SceneName = SceneManager.GetActiveScene().name,
            Token = Guid.NewGuid().ToString()
        }, null);
    }

    /// <summary>
    /// This dictionary will be sent to "master server" when we want 
    /// notify "master" server that Spawn Process is completed
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, string> CreateSpawnFinalizationData()
    {
        return new Dictionary<string, string>()
        {
            // Add room id, so that whoever requested to spawn this game server,
            // knows which rooms access to request
            {MsfDictKeys.RoomId, Controller.RoomId.ToString()},

            // Add room password, so that creator can request an access to a 
            // password-protected room
            {MsfDictKeys.RoomPassword, Controller.Options.Password}
        };
    }

    /// <summary>
    /// This should be called when client leaves the game server.
    /// This method will remove player object from lookups
    /// </summary>
    /// <param name="connection"></param>
    public void ClientDisconnected(NetworkConnection connection)
    {
        UnetMsfPlayer player;
        PlayersByConnectionId.TryGetValue(connection.connectionId, out player);

        if (player == null)
            return;

        OnPlayerLeft(player);
    }

    protected virtual void HandleReceivedAccess(NetworkMessage netmsg)
    {
        var token = netmsg.ReadMessage<StringMessage>().value;

        Controller.ValidateAccess(token, (validatedAccess, error) =>
        {
            if (validatedAccess == null)
            {
                Logger.Error("Failed to confirm access token:" + error);
                // Confirmation failed, disconnect the user
                netmsg.conn.Disconnect();
                return;
            }

            Logger.Debug("Confirmed token access for peer: " + validatedAccess);

            // Get account info
            Msf.Server.Auth.GetPeerAccountInfo(validatedAccess.PeerId, (info, errorMsg) =>
            {
                if (info == null)
                {
                    Logger.Error("Failed to get account info of peer " + validatedAccess.PeerId + "" +
                                 ". Error: " + errorMsg);
                    return;
                }

                Logger.Debug("Got peer account info: " + info);

                var player = new UnetMsfPlayer(netmsg.conn, info);

                OnPlayerJoined(player);
            });
        });
    }

    protected virtual void OnPlayerJoined(UnetMsfPlayer player)
    {
        // Add to lookups
        PlayersByPeerId[player.PeerId] = player;
        PlayersByUsername[player.Username] = player;
        PlayersByConnectionId[player.Connection.connectionId] = player;

        if (PlayerJoined != null)
            PlayerJoined.Invoke(player);
    }

    protected virtual void OnPlayerLeft(UnetMsfPlayer player)
    {
        // Remove from lookups
        PlayersByPeerId.Remove(player.PeerId);
        PlayersByUsername.Remove(player.Username);
        PlayersByConnectionId.Remove(player.Connection.connectionId);
        
        if (PlayerLeft != null)
            PlayerLeft.Invoke(player);

        // Notify controller that the player has left
        Controller.PlayerLeft(player.PeerId);
    }

    private void OnDisconnectedFromMaster()
    {
        if (QuitAppIfDisconnected)
            Application.Quit();
    }

    public UnetMsfPlayer GetPlayer(string username)
    {
        UnetMsfPlayer player;
        PlayersByUsername.TryGetValue(username, out player);
        return player;
    }

    public Dictionary<string, UnetMsfPlayer> GetPlayers()
    {
        return PlayersByUsername;
    }

    protected virtual void OnDestroy()
    {
        Msf.Server.Rooms.Connection.Disconnected -= OnDisconnectedFromMaster;
    }
}