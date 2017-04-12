using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.UI;

public class BasicsSpawnerStarterUi : MonoBehaviour
{
    public InputField MachineIp;
    public InputField ExecutablePath;

    public Button RegisterSpawnerBtn;

    public SpawnerBehaviour SpawnerBehaviour;

	// Use this for initialization
	void Start () {
		RegisterSpawnerBtn.onClick.AddListener(OnRegisterSpawnerClick);

	    SpawnerBehaviour = SpawnerBehaviour ?? FindObjectOfType<SpawnerBehaviour>();
	}
	
	// Update is called once per frame
	void Update () {
	    RegisterSpawnerBtn.gameObject.SetActive(!SpawnerBehaviour.IsSpawnerStarted);
	}

    public void OnRegisterSpawnerClick()
    {
        if (!Msf.Connection.IsConnected)
        {
            Logs.Error("You must first connect to master");

            // Show a dialog box with error
            Msf.Events.Fire(Msf.EventNames.ShowDialogBox, 
                DialogBoxData.CreateError("You must first connect to master"));
            return;
        }

        // Set the default executable path
        // It's called "Default", because it's used if "-msfExe" 
        // argument doesn't override it (change it)
        SpawnerBehaviour.DefaultExePath = ExecutablePath.text;

        // Make sure that exe path is not overriden in editor
        SpawnerBehaviour.OverrideExePathInEditor = false;

        // Set default machine IP
        SpawnerBehaviour.DefaultMachineIp = MachineIp.text;

        // Start the spawner
        SpawnerBehaviour.StartSpawner();
    }
}
