using System.Collections.Generic;
using System.Linq;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.Networking;

public class MiniNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        SpawnPlayer(conn, "Player", "carrot");
    }

    /// <summary>
    ///     Spawns a character for connected client, and assigns it to connection
    /// </summary>
    /// <returns></returns>
    public static MiniPlayerController SpawnPlayer(NetworkConnection connection, string playerName, 
        string weaponSprite, Transform position = null)
    {
        // Create an instance
        var player = Instantiate(Resources.Load<MiniPlayerController>("Prefabs/MiniPlayer"));

        if (position == null)
        {
            // Nove to random position, of no position was given
            player.MoveToRandomSpawnPoint();
        }
        else
        {
            player.transform.position = position.position;
        }

        NetworkServer.AddPlayerForConnection(connection, player.gameObject, 0);
        player.SetWeapon(weaponSprite);
        player.Setup(playerName);

        return player;
    }

    public void DisconnectAllPlayers()
    {
        foreach (var player in FindObjectsOfType<MiniPlayerController>())
        {
            player.connectionToClient.Disconnect();
        }
    }
}