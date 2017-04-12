using System;
using System.Collections;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class RoomAccessExample : MonoBehaviour
{
    private Coroutine WaitConnectionCoroutine;

    void Awake()
    {
        // Subscribe to the event
        Msf.Client.Rooms.AccessReceived += OnAccessReceived;
    }

    private void OnAccessReceived(RoomAccessPacket access)
    {
        // This will be called when you receive an access

        if (access.Properties.ContainsKey(MsfDictKeys.SceneName))
        {
            // In case we received a name of the scene in the access
            // TODO switch scenes if necessary
            Debug.LogError("We might need to change scenes");
            return;
        }

        // We can use it to connect to Unet game server (this is just an example)
        var networkManager = FindObjectOfType<NetworkManager>();
        networkManager.networkAddress = access.RoomIp;
        networkManager.networkPort = access.RoomPort;

        // Start connecting
        networkManager.StartClient();

        if (WaitConnectionCoroutine != null)
            StopCoroutine(WaitConnectionCoroutine);

        // Wait until connected to server
        WaitConnectionCoroutine = StartCoroutine(WaitForConnection(() =>
        {
            // Client connected to server
            var tokenMsg = new StringMessage(access.Token);

            // Send the token to unet server
            networkManager.client.connection.Send(777, tokenMsg);
        }));
    }

    public IEnumerator WaitForConnection(Action callback)
    {
        var networkManager = FindObjectOfType<NetworkManager>();

        // This will keep skipping frames until client connects
        while (!networkManager.IsClientConnected())
            yield return null;

        callback.Invoke();
    }

    void OnDestroy()
    {
        // Unsubscribe from event
        Msf.Client.Rooms.AccessReceived -= OnAccessReceived;
    }
}
