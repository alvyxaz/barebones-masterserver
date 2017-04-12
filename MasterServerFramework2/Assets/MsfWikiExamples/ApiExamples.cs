using System;
using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This script contains examples that I paste into the wiki docs.
/// Don't try to call the methods, they are not supposed to be working (at least not in the 
/// order I've written them)
/// </summary>
public class ApiExamples {

    public void ChatApi()
    {
        // Pick a username for the chat
        Msf.Client.Chat.PickUsername("MyUsername", (successful, error) =>
        {
            if (!successful) Logs.Error(error);
        });

        // Join a channel
        Msf.Client.Chat.JoinChannel("ChannelName", (successful, error) => { });

        // Leave a channel
        Msf.Client.Chat.LeaveChannel("ChannelName", (successful, error) => { });

        // Get a list of channels a user has joined
        Msf.Client.Chat.GetMyChannels((channels, error) =>
        {
            if (channels == null)
            {
                Debug.LogError(error);
                return;
            }

            Debug.Log("User channels: " + string.Join(", ", channels.ToArray()));
        });

        // Get a list of users in a specific channel
        Msf.Client.Chat.GetUsersInChannel("ChannelName", (users, error) =>
        {
            if (users == null)
            {
                Debug.LogError(error);
                return;
            }

            Debug.Log("Users in channel: " + string.Join(", ", users.ToArray()));
        });

        // Sets a default chat channel (if no channel name is given when sending a message,
        // this message will be sent to default channel)
        Msf.Client.Chat.SetDefaultChannel("ChannelName", (successful, error) => { });

        // Send a message to default channel
        Msf.Client.Chat.SendToDefaultChannel("This is a message", (successful, error) => { });

        // Send a private message
        Msf.Client.Chat.SendPrivateMessage("ReceiverUsername", "your message", (successful, error) => { });

        // Send a message to a specific channel
        Msf.Client.Chat.SendChannelMessage("ChannelName", "message", (successful, error) => { });
    }

    public void RoomsApi()
    {
        // This will register a room to the master server, so that
        // master server would know about it's existance
        // This is a minimal example
        Msf.Server.Rooms.RegisterRoom((controller, error) =>
        {
            if (controller == null)
                Logs.Error(error);
        });

        var roomOptions = new RoomOptions()
        {
            IsPublic = false,
            MaxPlayers = 5,
            Name = "My super room",
            Password = "pssw",
            Properties = new Dictionary<string, string>()
            {
                {"CustomProperty", "Some extra stuff" }
            },
            RoomIp = "127.0.0.1",
            RoomPort = 777
        };

        // More customization options
        Msf.Server.Rooms.RegisterRoom(roomOptions, (controller, error) =>
        {
            // Edit the options, to make the room public
            controller.Options.IsPublic = true;

            // Save the options
            controller.SaveOptions(controller.Options);

            // When player sends us an access token, we can confirm if the token is valid
            controller.ValidateAccess("token..", (playerPeerId, confirmationError) =>
            {
                if (playerPeerId == null)
                {
                    Logs.Error("Player provided an invalid token");
                    return;
                }
                    
                // Player provided a valid token
                // TODO Get account info by peer id
                // TODO Spawn a player to the game
            });

            // If we want to handle who gets access, and who doesn't
            controller.SetAccessProvider((requester, giveAccess) =>
            {
                // TODO use the peerId to retrieve account data
                // TODO check if, for example, the username is banned in this room
                
                // If user is allowed, create a new room access
                giveAccess(new RoomAccessPacket()
                {
                    RoomIp = controller.Options.RoomIp,
                    RoomPort = controller.Options.RoomPort,
                    Properties = new Dictionary<string, string>()
                    {
                        // Custom properties
                        {"Color", "#ffffff" }
                    },
                    RoomId = controller.RoomId,
                    SceneName = SceneManager.GetActiveScene().name,
                    Token = Guid.NewGuid().ToString()
                }, null);

                // If user is not allowed
                giveAccess(null, "You're not allowed to play!");
            });
        });


        var roomId = 5;

        // Getting access from client
        Msf.Client.Rooms.GetAccess(roomId, (access, error) =>
        {
            if (access == null)
            {
                Debug.LogError(error);
                return;
            }

            // We've received the access
            Debug.Log(access);

            // TODO use ip and port from access to connect to game server
            // TODO send the token to game server  (this will depend on game server technologies used)
        });
    }

    public void SpawnerApi()
    {
        var defaultOptions = new SpawnerOptions();

        // Registers a spawner to master server, so that master server knows about it's existance.
        // Your spawner will receive requests to spawn processes (or something else,
        // if necessary)
        Msf.Server.Spawners.RegisterSpawner(defaultOptions, (spawner, error) =>
        {
            if (spawner == null)
                Logs.Error(error);
        });

        var spawnerOptions = new SpawnerOptions()
        {
            MaxProcesses = 0, // Unlimited,
            MachineIp = "127.0.0.1", // IP address of this machine, will be passed to game servers too
            Region = "US", // Region identifier, can be anything
            Properties = new Dictionary<string, string>()
            {
                // If you need spawner to have some extra properties
                {"ExtraProperty", "Whatever" }
            }
        };

        // Example of a more customized approach
        Msf.Server.Spawners.RegisterSpawner(spawnerOptions, (spawner, error) =>
        {
            if (spawner == null)
            {
                Logs.Error(error);
                return;
            }
            
            // Set the build path (default ''(empty string))
            spawner.DefaultSpawnerSettings.ExecutablePath = "C:/Win/Build.exe";

            // Change whether or not the spawner process should run in batchmode
            spawner.DefaultSpawnerSettings.SpawnInBatchmode = false;

            // (Optional) If you want to handle spawn requests manually
            spawner.SetSpawnRequestHandler((packet, message) =>
            {
                // We've got a request to spawn a new process
                // packet - contains spawn info
                // message - the original message of the request. You'll need to respond to it
                // with ResponseStatus.Success, if process started successfully
                var hasError = false;
                if (hasError)
                {
                    // Example on how to handle errors
                    message.Respond("A mysterious error", ResponseStatus.Failed);
                    return;
                }

                // TODO Start a process or a virtual game server

                // Respond with success
                message.Respond(ResponseStatus.Success);
            });

            // (Optional) If you want to handle kill requests manually
            spawner.SetKillRequestHandler(spawnId =>
            {
                // This is a request to kill a spawned process
                // TODO Find a process by spawnId and kill it
            });
        });
    }

    public void AuthApi()
    {
        // ----------------------------------------
        // Get data from remote server

        // This is a peer id, which was given to client when he connected to master server
        var peerId = 5; 
        Msf.Server.Auth.GetPeerAccountInfo(peerId, (info, error) =>
        {
            if (info == null)
                Debug.LogError(error);

            Debug.Log(info);
        });

        // Check if client is logged in
        if (Msf.Client.Auth.IsLoggedIn)
            Debug.Log("Client is logged in");

        // Access info of user who is logged in
        var accountInfo = Msf.Client.Auth.AccountInfo;
        Debug.Log("Logged in as: " + accountInfo.Username);

        //--------------------------------------
        // Log in with credentials
        Msf.Client.Auth.LogIn("username", "password", (successful, error) =>
        {
            Debug.Log("Is successful: " + successful + "; Error (if exists): " + error);
        });

        //--------------------------------------
        // Guest login
        Msf.Client.Auth.LogInAsGuest((successful, error) =>
        {
            Debug.Log("Is successful: " + successful + "; Error (if exists): " + error);
        });

        //--------------------------------------
        // Generic login (if you need more data)
        var data = new Dictionary<string, string>
        {
            {"username", "Username"},
            {"password", "Password123"}
        };

        Msf.Client.Auth.LogIn(data, (successful, error) =>
        {
            Debug.Log("Is successful: " + successful + "; Error (if exists): " + error);
        });

        //--------------------------------------
        // Register a new account
        var registrationData = new Dictionary<string, string>
            {
                {"username", "Super Username"},
                {"password", "Super Password"},
                {"email", "myEmail@email.com"}
            };

        Msf.Client.Auth.Register(registrationData, (successful, error) =>
        {
            Debug.Log("Is successful: " + successful + "; Error (if exists): " + error);
        });
    }
}
