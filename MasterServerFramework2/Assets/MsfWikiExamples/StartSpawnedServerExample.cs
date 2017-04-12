using System;
using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// Add it to the starting scene, and to game server scenes
/// </summary>
public class StartSpawnedServerExample : MonoBehaviour
{
    void Awake()
    {
        if (Msf.Args.IsProvided(Msf.Args.Names.LoadScene))
        {
            // If we are supposed to go to another scene first
            // (used to start servers on different scenes)
            SceneManager.LoadScene(Msf.Args.LoadScene);
            return;
        }

        // We need to make sure that we are connected to master server first

        // If second parameter is true and we're already connected,
        // the event will be invoked instantly
        Msf.Connection.AddConnectionListener(OnConnectionEstablished, true);
    }

    /// <summary>
    /// This will be called only if we're connected to master server
    /// </summary>
    public void OnConnectionEstablished()
    {
        if (!Msf.Server.Spawners.IsSpawnedProccess)
        {
            Logs.Warn("This is not a spawned instance");
            return;
        }

        // We need to register to master server first
        // to notify it that the process was started
        Msf.Server.Spawners.RegisterSpawnedProcess(Msf.Args.SpawnId, Msf.Args.SpawnCode, (controller, error) =>
        {
            if (controller == null)
            {
                Debug.LogError("Failed to register a spawned process: " + error);
                return;
            }

            // We've successfully registered, and we can use the data, which 
            // was provided by the client to start our game server
            StartGameServer(controller);

        });
    }

    public void StartGameServer(SpawnTaskController controller)
    {
        // We can access data, which Client sent to spawner
        var data = controller.Properties;

        // You can start any kind of server here.
        // For example, we'll start Unet HLAPI server

        var networkManager = FindObjectOfType<NetworkManager>();

        // If an arg was provided to use websockets
        if (Msf.Args.IsProvided(Msf.Args.Names.WebGl))
            networkManager.useWebSockets = true;

        // Spawner added an argument with assigned port number that we can use
        networkManager.networkPort = Msf.Args.AssignedPort;

        // Start the server
        networkManager.StartServer();

        // We could finalize it now, but we haven't registered the room yet
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

        // We can read some of the options from what player provided
        // when he sent a request
        var prop = controller.Properties;

        if (prop.ContainsKey(MsfDictKeys.RoomName))
            options.Name = prop[MsfDictKeys.RoomName];

        if (prop.ContainsKey(MsfDictKeys.MaxPlayers))
            options.MaxPlayers = int.Parse(prop[MsfDictKeys.MaxPlayers]);

        if (prop.ContainsKey(MsfDictKeys.RoomPassword))
            options.Password = prop[MsfDictKeys.RoomPassword];

        if (prop.ContainsKey(MsfDictKeys.MapName))
            options.Properties[MsfDictKeys.MapName] = prop[MsfDictKeys.MapName];

        // Also, add the scene name
        options.Properties[MsfDictKeys.SceneName] = SceneManager.GetActiveScene().name;

        // Register the room to master server
        Msf.Server.Rooms.RegisterRoom(options, (roomController, error) =>
        {
            if (roomController == null)
            {
                Debug.LogError(error);
                return;
            }

            // So the room was registered successfully, we can now finalize the 
            // spawn request
            controller.FinalizeTask(new Dictionary<string, string>()
            {
                // Add our room id to finalization data
                {MsfDictKeys.RoomId, roomController.RoomId.ToString()} 
            });
        });
    }
}
