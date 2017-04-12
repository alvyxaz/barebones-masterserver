using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldDemoMainScene : MonoBehaviour
{
    public SceneField LoadingScene;

    public AuthUiController AuthUi;

	// Use this for initialization
	void Start ()
	{

	    AuthUi = AuthUi ?? FindObjectOfType<AuthUiController>();

        // If argument to switch scenes is provided - switch scenes
        if (Msf.Args.IsProvided(Msf.Args.Names.LoadScene))
	        SceneManager.LoadScene(Msf.Args.LoadScene);
	}

    public void OnEnterWorldClick()
    {
        Msf.Connection.SendMessage(WorldDemoOpCodes.EnterWorldRequest, (status, response) =>
        {
            if (status != ResponseStatus.Success)
            {
                Logs.Error("Failed to enter the world: " + response.AsString("Unknown error"));
                return;
            }

            // Move to loading scene, where we will request an access to the zone,
            // in which we are supposed to be added
            SceneManager.LoadScene(LoadingScene.SceneName);
        });
    }

    void Update()
    {
        if (!Msf.Runtime.IsEditor)
        {
            AuthUi.gameObject.SetActive(!MasterServerBehaviour.IsMasterRunning);
        }
    }

}
