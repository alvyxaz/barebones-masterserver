using System;
using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class StartSpawnedServerUnsafe : MonoBehaviour
{
    void Awake()
    {
        if (Msf.Args.IsProvided(Msf.Args.Names.LoadScene))
        {
            SceneManager.LoadScene(Msf.Args.LoadScene);
            return;
        }

        Msf.Connection.AddConnectionListener(OnConnectionEstablished, true);
    }

    public void OnConnectionEstablished()
    {
        Msf.Server.Spawners.RegisterSpawnedProcess(Msf.Args.SpawnId, Msf.Args.SpawnCode, (controller, error) =>
        {

            StartGameServer(controller);
        });
    }

    public void StartGameServer(SpawnTaskController controller)
    {
        var networkManager = FindObjectOfType<NetworkManager>();

        if (Msf.Args.IsProvided(Msf.Args.Names.WebGl))
            networkManager.useWebSockets = true;

        networkManager.networkPort = Msf.Args.AssignedPort;
        networkManager.StartServer();

        RegisterRoomAndFinalize(networkManager, controller);
    }

    public void RegisterRoomAndFinalize(NetworkManager networkManager, SpawnTaskController controller)
    {
        // Create room options
        var options = new RoomOptions()
        {
            IsPublic = true,
            Properties = new Dictionary<string, string>(),
            RoomPort = networkManager.networkPort, // or Msf.Args.AssignedPort
            RoomIp = Msf.Args.MachineIp // Spawner should have passed us his own IP
        };

        var prop = controller.Properties;

        if (prop.ContainsKey(MsfDictKeys.RoomName))
            options.Name = prop[MsfDictKeys.RoomName];

        if (prop.ContainsKey(MsfDictKeys.MaxPlayers))
            options.MaxPlayers = int.Parse(prop[MsfDictKeys.MaxPlayers]);

        options.Properties[MsfDictKeys.SceneName] = SceneManager.GetActiveScene().name;

        Msf.Server.Rooms.RegisterRoom(options, (roomController, error) =>
        {
            controller.FinalizeTask(new Dictionary<string, string>()
            {
                {MsfDictKeys.RoomId, roomController.RoomId.ToString()}
            });
        });
    }
}
