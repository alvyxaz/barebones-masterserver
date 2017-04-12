using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.UI;

public class SpawnRequestExampleUnsafe : MonoBehaviour
{
    public Button SendRequestButton;

    void Awake()
    {
        SendRequestButton = SendRequestButton ?? GetComponent<Button>();

        // When button is clicked, invoke SendRequest method
        SendRequestButton.onClick.AddListener(SendRequest);
    }

    public void SendRequest()
    {
        var spawnOptions = new Dictionary<string, string>
        {
            {MsfDictKeys.MaxPlayers, "5"},
            {MsfDictKeys.RoomName, "Name of your Room"},
            {MsfDictKeys.MapName, "Map Name"},
            {MsfDictKeys.SceneName, "GameScene"}
        };

        Msf.Client.Spawners.RequestSpawn(spawnOptions, "", (controller, error) =>
        {
            StartCoroutine(WaitForServerToBeFinalized(controller));
        });
    }

    private IEnumerator WaitForServerToBeFinalized(SpawnRequestController request)
    {
        while (request.Status != SpawnStatus.Finalized)
            yield return null;

        request.GetFinalizationData((data, error) =>
        {
            var roomId = int.Parse(data[MsfDictKeys.RoomId]);

            GetRoomAccess(roomId);
        });
    }

    public void GetRoomAccess(int roomId)
    {
        Msf.Client.Rooms.GetAccess(roomId, (access, error) =>
        {
            // Use the access
        });
    }
}
