using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.UI;

public class SpawnRequestExample : MonoBehaviour
{
    /// <summary>
    /// Set this button through inspector,
    /// or add this script on the button itself
    /// </summary>
    public Button SendRequestButton;

    void Awake()
    {
        SendRequestButton = SendRequestButton ?? GetComponent<Button>();

        // When button is clicked, invoke SendRequest method
        SendRequestButton.onClick.AddListener(SendRequest);
    }

    /// <summary>
    /// This method will be called when you click on a button
    /// </summary>
    public void SendRequest()
    {
        if (!Msf.Connection.IsConnected)
        {
            Debug.LogError("Not connected to master server");
            return;
        }

        if (!Msf.Client.Auth.IsLoggedIn)
        {
            Debug.LogError("You're not logged in");
            return;
        }

        // These options will be send to spawner, and then passed
        // to spawned process, so that it knows what kind of game server to start.
        // You can add anything to this dictionary
        var spawnOptions = new Dictionary<string, string>
        {
            {MsfDictKeys.MaxPlayers, "5"},
            {MsfDictKeys.RoomName, "Name of your Room"},
            {MsfDictKeys.MapName, "Map Name"},

            // Make sure you set this right, and that this scene
            // is added to the build of your game server
            {MsfDictKeys.SceneName, "GameScene"}
        };

        // We don't care about the region
        var region = "";

        // Send the request to spawn a game server
        Msf.Client.Spawners.RequestSpawn(spawnOptions, region, (controller, error) =>
        {
            if (controller == null)
            {
                Debug.LogError("Failed: " + error);
                return;
            }

            // If we got here, the request is being handled, but we need
            // to wait until it's done

            // We'll start a coroutine for that (they are perfect for waiting ^_^)
            StartCoroutine(WaitForServerToBeFinalized(controller));
        });
    }

    /// <summary>
    /// I hope you know how coroutines work. If you don't - it's worth checking it out
    /// </summary>
    private IEnumerator WaitForServerToBeFinalized(SpawnRequestController request)
    {
        var currentStatus = request.Status;

        // Keep looping until spawn request is finalized
        // (if spawn request is aborted, this will loop infinitely, 
        // because request will never be finalized, but I think you'll know how to
        // handle it)
        while (request.Status != SpawnStatus.Finalized)
        {
            // Skip a frame, if it's still not finalized
            yield return null;

            // If status has changed
            if (currentStatus != request.Status)
            {
                Debug.Log("Status changed to: " + request.Status);
                currentStatus = request.Status;
            }
        }

        // If we got here, the spawn request has been finalized

        // When spawned process finalizes, it gives master server some,
        // information about itself, which includes room id

        // We can retrieve this data from master server:
        // This method will be renamed to `GetFinalizationData`
        request.GetFinalizationData((data, error) =>
        {
            if (data == null)
            {
                Debug.LogError("Failed to get finalization data: " + error);
                return;
            }

            if (!data.ContainsKey(MsfDictKeys.RoomId))
            {
                Debug.LogError("Spawned server didn't add a room ID to finalization data");
                return;
            }

            // So we've received the roomId of the game server that
            // we've requested to spawn
            var roomId = int.Parse(data[MsfDictKeys.RoomId]);

            GetRoomAccess(roomId);
        }); 
    }

    /// <summary>
    /// Retrieves an access to a specified room
    /// </summary>
    public void GetRoomAccess(int roomId)
    {
        Msf.Client.Rooms.GetAccess(roomId, (access, error) =>
        {
            if (access == null)
            {
                Debug.LogError("Failed to get room access: " + error);
                return;
            }

            // We have the access, and we can use it to access the game server
            //var sceneName = access.SceneName;
            //var ipAddress = access.RoomIp;
            //var port = access.RoomPort;
            //var token = access.Token;
            // (There's more data available in the access object)

            // It's now up to you to switch to the specified scene,
            // connect to the game server and send it the access
        });
    }
}
