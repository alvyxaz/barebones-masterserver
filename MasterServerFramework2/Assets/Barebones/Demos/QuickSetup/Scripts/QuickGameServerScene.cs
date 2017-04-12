using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuickGameServerScene : MonoBehaviour {

    public HelpBox _header  = new HelpBox("Connects to Master server, " +
                                          "and switches to an appropriate scene");

	// Use this for initialization
	void Start () {
        // Wait until we're connected to master server
        Msf.Connection.AddConnectionListener(OnConnectedToMaster, true);
	}

    private void OnConnectedToMaster()
    {
        if (!Msf.Args.IsProvided(Msf.Args.LoadScene))
        {
            Logs.Error("A scene to load was not provided");
            return;
        }

        SceneManager.LoadScene(Msf.Args.LoadScene);
    }

}
