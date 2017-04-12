using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;

public class WorldDemoZoneRoom : UnetGameRoom {

    private HashSet<string> _pendingTeleportationRequests;

    [Header("Zone Room")]
    public HelpBox _zonesHeader = new HelpBox()
    {
        Text = "Settings bellow ",
        Type = HelpBoxType.Warning
    };

    [Tooltip("If room is not registered within a given amount of seconds, " +
             "it will be terminated")]
    public float RegisterRoomTimeout = 10f;

    [Tooltip("This should be a unique identifier of the zone")]
    public string ZoneId;

    protected override void Awake()
    {
        base.Awake();

        _pendingTeleportationRequests = new HashSet<string>();

        StartCoroutine(HandleShutdown());
    }

    /// <summary>
    /// Takes care of shutting down game server (zone) when it's 
    /// necessary
    /// </summary>
    /// <returns></returns>
    private IEnumerator HandleShutdown()
    {
        var connection = Msf.Server.Rooms.Connection;

        Msf.Server.Rooms.Connection.Disconnected += () =>
        {
            // Terminate application, when connection with master is lost
            Application.Quit();
        };

        yield return new WaitForSeconds(RegisterRoomTimeout);

        if (Controller == null)
            Application.Quit();

        if (!connection.IsConnected)
            Application.Quit();
    }

    /// <summary>
    /// Before registering a room, add a property with zone name,
    /// so that master server can lookup zones
    /// </summary>
    /// <param name="options"></param>
    protected override void BeforeSendingRegistrationOptions(RoomOptions options)
    {
        base.BeforeSendingRegistrationOptions(options);

        if (options.Properties == null)
            options.Properties = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(ZoneId))
        {
            Logs.Error("Invalid zone name");
            if (QuitAppIfDisconnected)
            {
                Application.Quit();
            }
            return;
        }

        options.Properties[WorldDemoModule.ZoneNameKey] = ZoneId;
    }

    public virtual void TeleportPlayerToAnotherZone(UnetMsfPlayer player, string zoneName, Vector3 newPos)
    {
        // Ignore if there's already a request pending
        if (_pendingTeleportationRequests.Contains(player.Username))
            return;

        var packet = new TeleportRequestPacket()
        {
            Username = player.Username,
            ZoneName = zoneName,
            Position = newPos.x + "," + newPos.y + "," + newPos.z
        };

        Controller.Connection.SendMessage(WorldDemoOpCodes.TeleportRequest, packet, (status, response) =>
        {
            // Remove from the list of pending requests
            _pendingTeleportationRequests.Remove(packet.Username);

            if (status != ResponseStatus.Success)
            {
                Logs.Error(string.Format("Failed to teleport user '{0}' to zone '{1}': " +
                    response.AsString(), player.Username, zoneName));
                return;
            }

            // At this point, we are certain that player got access to another zone,
            // so we can force disconnect the player. After that, player will enter the loading screen,
            // from which he will connect to another zone
            player.Connection.Disconnect();
        });
    }
}
