using System;
using System.Collections.Generic;
using System.Linq;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;

/// <summary>
/// This is the main module of the World demo.
/// </summary>
public class WorldDemoModule : ServerModuleBehaviour
{
    public HelpBox _header = new HelpBox()
    {
        Text = "This component is a custom server module, " +
               "which will be added to master server"
    };

    public const string ZoneNameKey = "ZoneName";
    public const string ZonePosition = "ZonePos";

    protected AuthModule AuthModule;
    protected SpawnersModule SpawnersModule;
    protected RoomsModule RoomsModule;

    private bool _areZonesSpawned;

    /// <summary>
    /// If this is set to true, master server on editor will not spawn game zones
    /// </summary>
    public bool SpawnZonesInEditor = false;

    public List<string> ZonesToSpawn = new List<string>()
    {
        {"WorldDemo-ZoneMain" },
        {"WorldDemo-ZoneSecondary" },
    };

    void Awake()
    {
        // Destroy this game object if it already exists
        if (DestroyIfExists())
        {
            Destroy(gameObject);
            return;
        };

        // Don't destroy the module on load
        DontDestroyOnLoad(gameObject);

        // Register dependencies
        AddDependency<AuthModule>();
        AddDependency<SpawnersModule>();

        // Add an optional dependency to profiles module (if we're going to use it)
        AddOptionalDependency<ProfilesModule>();

    }

    public override void Initialize(IServer server)
    {
        AuthModule = server.GetModule<AuthModule>();
        RoomsModule = server.GetModule<RoomsModule>();
        SpawnersModule = server.GetModule<SpawnersModule>();

        // Add game server handlers
        server.SetHandler(WorldDemoOpCodes.TeleportRequest, HandleTeleportRequest);

        // Add client handlers
        server.SetHandler(WorldDemoOpCodes.EnterWorldRequest, HandleEnterWorldRequest);
        server.SetHandler(WorldDemoOpCodes.GetCurrentZoneAccess, HandleGetZoneAccess);

        //----------------------------------------------
        // Spawn game servers (zones)

        // Find a spawner 
        var spawner = SpawnersModule.GetSpawners().FirstOrDefault();

        if (spawner != null)
        {
            // We found a spawner we can use
            SpawnZoneServers(spawner);
        }
        else
        {
            // Spawners are not yet registered to the master, 
            // so let's listen to an event and wait for them
            SpawnersModule.SpawnerRegistered += registeredSpawner =>
            {
                    // Ignore if zones are already spawned
                    if (_areZonesSpawned) return;

                    // Spawn the zones
                    SpawnZoneServers(registeredSpawner);

                _areZonesSpawned = true;
            };
        }
    }

    public virtual void SetupProfileFactory(ProfilesModule profilesModule)
    {
        profilesModule.ProfileFactory = (username, peer) => new ObservableServerProfile(username)
        {
            new ObservableInt(0, 5),
            new ObservableString(1, "TestingString")
        };
    }

    /// <summary>
    /// Spawns all of the zones for the demo
    /// </summary>
    /// <param name="spawner"></param>
    public virtual void SpawnZoneServers(RegisteredSpawner spawner)
    {
        Logs.Warn("Spawning zones");

#if UNITY_EDITOR
        if (!SpawnZonesInEditor)
            return;
#endif

#if !UNITY_EDITOR
        // Server will start 
        if (!Environment.GetCommandLineArgs().Contains("-spawnZones"))
            return;
#endif

        foreach (var zone in ZonesToSpawn)
        {
            var sceneName = zone;
            SpawnersModule.Spawn(GenerateZoneSpawnInfo(sceneName))
                .WhenDone(task => Logs.Info(sceneName + " zone spawn status: " + task.Status));
        }
    }

    /// <summary>
    /// Helper method, which generates a settings dictionary for our zones
    /// </summary>
    /// <param name="sceneName"></param>
    /// <returns></returns>
    public Dictionary<string, string> GenerateZoneSpawnInfo(string sceneName)
    {
        return new Dictionary<string, string>()
        {
            {MsfDictKeys.SceneName, sceneName },
            {MsfDictKeys.IsPublic, "false" },
            {MsfDictKeys.RoomName, sceneName}
        };
    }

    #region Message handlers

    /// <summary>
    /// Handles client's request to get access to the zone
    /// he is supposed to be in
    /// </summary>
    /// <param name="message"></param>
    public virtual void HandleGetZoneAccess(IIncommingMessage message)
    {
        var access = message.Peer.GetProperty(WorldDemoPropCodes.ZoneAccess) as RoomAccessPacket;

        if (access == null)
        {
            message.Respond("No access found", ResponseStatus.Failed);
            return;
        }

        message.Respond(access, ResponseStatus.Success);

        // Delete the access (making it usable only once)
        message.Peer.SetProperty(WorldDemoPropCodes.ZoneAccess, null);
    }

    /// <summary>
    /// Handles a request from game server to teleport
    /// user to another game server / zone.
    /// </summary>
    /// <param name="message"></param>
    public virtual void HandleTeleportRequest(IIncommingMessage message)
    {
        var request = message.Deserialize(new TeleportRequestPacket());

        var user = AuthModule.GetLoggedInUser(request.Username);
        var peer = user.Peer;

        // Find the room which represents the zone we need
        var room = RoomsModule.GetAllRooms()
            .Where(s => s.Options.Properties.ContainsKey(ZoneNameKey))
            .FirstOrDefault(s => s.Options.Properties[ZoneNameKey] == request.ZoneName);

        if (room == null)
        {
            // If no room with that zone name was found
            message.Respond("Zone was not found", ResponseStatus.Failed);
            return;
        }

        var accessRequestProperties = new Dictionary<string, string>()
        {
            // Add the new position to the request
            // So that new server knows where exactly to position the player
            {ZonePosition, request.Position }
        };

        // Request an access to room
        room.GetAccess(peer, accessRequestProperties, (access, error) =>
        {
            if (access == null)
            {
                // We didn't get the access
                message.Respond("Failed to get access to the zone: " + error, ResponseStatus.Failed);
                return;
            }

            // We have the access to new zone, let's store it
            // so player can request it when he's on the loading screen
            peer.SetProperty(WorldDemoPropCodes.ZoneAccess, access);

            // Notify game server that access was received
            message.Respond(ResponseStatus.Success);
        });
    }

    /// <summary>
    /// Handles users request to join the game world.
    /// It picks a random (*first) game server
    /// </summary>
    /// <param name="message"></param>
    public virtual void HandleEnterWorldRequest(IIncommingMessage message)
    {
        var user = message.Peer.GetExtension<IUserExtension>();

        if (user == null)
        {
            // Invalid player session
            message.Respond("Not logged in", ResponseStatus.Unauthorized);
            return;
        }

        // Get world servers. We can filter world zones by checking
        // if a game server has a zone name key
        var worldServers = RoomsModule.GetAllRooms()
            .Where(s => s.Options.Properties.ContainsKey(ZoneNameKey));

        // Find which zone we should be getting into.

        // You'd probably want to load the name of the zone
        // the user was in before quitting the game, but to keep this
        // example simple, we'll just take the first zone from the list
        var gameServer = worldServers.FirstOrDefault();

        if (gameServer == null)
        {
            message.Respond("Zone not found", ResponseStatus.Failed);
            return;
        }

        // Request an access
        gameServer.GetAccess(user.Peer, (access, error) =>
        {
            if (access == null)
            {
                // We didn't get the access
                message.Respond("Failed to get access to the zone: " + error, ResponseStatus.Failed);
                return;
            }

            // We have the access to new zone, let's store it
            // so player can request it when he's on the loading screen
            message.Peer.SetProperty(WorldDemoPropCodes.ZoneAccess, access);

            // Notify client that he's ready to enter the zone
            message.Respond(access, ResponseStatus.Success);
        });
    }

    #endregion

    public class WorldDemoPropCodes
    {
        public const int ZoneAccess = 101;
    }
}
