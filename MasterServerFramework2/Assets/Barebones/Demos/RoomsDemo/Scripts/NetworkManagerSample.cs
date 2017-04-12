using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// This is a small network manager example, which shows you how you can use the
/// <see cref="UnetGameRoom"/>
/// </summary>
public class NetworkManagerSample : NetworkManager
{
    public UnetGameRoom GameRoom;

    void Awake()
    {
        if (GameRoom == null)
        {
            Debug.LogError("Game Room property is not set on NetworkManager");
            return;
        }

        // Subscribe to events
        GameRoom.PlayerJoined += OnPlayerJoined;
        GameRoom.PlayerLeft += OnPlayerLeft;
    }

    /// <summary>
    /// Regular Unet method, which get's called when client disconnects from game server
    /// </summary>
    /// <param name="conn"></param>
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);

        // Don't forget to notify the room that a player disconnected
        GameRoom.ClientDisconnected(conn);
    }

    /// <summary>
    /// Invoked, when client provides a successful access token and enters the room
    /// </summary>
    /// <param name="player"></param>
    private void OnPlayerJoined(UnetMsfPlayer player)
    {
        // -----------------------------------
        // IF all you want to do is spawn a player:
        //
        // MiniNetworkManager.SpawnPlayer(player.Connection, player.Username, "carrot");
        // return;

        // -----------------------------------
        // If you want to use player profile

        // Create an "empty" (default) player profile
        var defaultProfile = DemoPlayerProfiles.CreateProfileInServer(player.Username);

        // Get coins property from profile
        var coinsProperty = defaultProfile.GetProperty<ObservableInt>(DemoPlayerProfiles.CoinsKey);

        // Fill the profile with values from master server
        Msf.Server.Profiles.FillProfileValues(defaultProfile, (successful, error) =>
        {
            if (!successful)
                Logs.Error("Failed to retrieve profile values: " + error);

            // We can still allow players to play with default profile ^_^

            // Let's spawn the player character
            var playerObject = MiniNetworkManager.SpawnPlayer(player.Connection, player.Username, "carrot");

            // Set coins value from profile
            playerObject.Coins = coinsProperty.Value;

            playerObject.CoinsChanged += () =>
            {
                // Every time player coins change, update the profile with new value
                coinsProperty.Set(playerObject.Coins);
            };
        });
    }

    private void OnPlayerLeft(UnetMsfPlayer player)
    {
        
    }
}
