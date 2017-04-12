using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.UI;

public class DemoMasterScene : MonoBehaviour
{
    public Button StartMasterBtn;

    public MasterServerBehaviour MasterServer;

    void Awake()
    {
        MasterServer = MasterServer ?? FindObjectOfType<MasterServerBehaviour>();
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

	    if (MasterServer.IsRunning == StartMasterBtn.gameObject.activeSelf)
	    {
            StartMasterBtn.gameObject.SetActive(!MasterServer.IsRunning);
        }
    }

    public void OnStartMasterClick()
    {
        MasterServer.StartServer(MasterServer.Port);
    }
}
