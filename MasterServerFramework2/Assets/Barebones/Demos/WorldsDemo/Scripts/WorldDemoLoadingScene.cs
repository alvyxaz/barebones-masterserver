using UnityEngine;
using System.Collections;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// In this demo, loading screen acts as "transferer" from
/// one game server to another.
/// </summary>
public class WorldDemoLoadingScene : MonoBehaviour
{
    public SceneField FailureScene;

	// Use this for initialization
	void Start () {

        if (FailureScene == null)
            Logs.Error("Failure scene is not set");

	    if (!Msf.Client.Connection.IsConnected)
	    {
	        // If we're not connected to master, jump back to maion screen
            SceneManager.LoadScene(FailureScene.SceneName);
	        return;
	    }

        // Get access to the zone we are supposed to be in.
        Msf.Client.Connection.SendMessage(WorldDemoOpCodes.GetCurrentZoneAccess, (status, response) =>
        {
            if (status != ResponseStatus.Success)
            {
                // If we've failed to request a teleport
                Logs.Warn("Teleport request failed. Reason: " + response.AsString() + "." +
                                 "This might be intentional(when quitting a game)");
                SceneManager.LoadScene(FailureScene.SceneName);
                return;
            }

            var access = response.Deserialize(new RoomAccessPacket());

            // Make sure that we won't try to start a game server
            // on the game scene
            Msf.Client.Rooms.ForceClientMode = true;

            RoomConnector.Connect(access);
        });
	}
}
