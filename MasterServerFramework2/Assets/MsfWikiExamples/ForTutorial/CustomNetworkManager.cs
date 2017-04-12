using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CustomNetworkManager : NetworkManager
{

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        var player = Instantiate(playerPrefab);

        var customPlayer = playerPrefab.GetComponent<CustomPlayer>();
        customPlayer.UsernameMesh.text = "Random" + (int) (Random.value*100);

        NetworkServer.AddPlayerForConnection(conn, player.gameObject, 0);

    }
}
