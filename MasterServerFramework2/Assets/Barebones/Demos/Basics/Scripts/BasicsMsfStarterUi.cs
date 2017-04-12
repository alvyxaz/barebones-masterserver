using System;
using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using Microsoft.Win32;
using UnityEngine;
using UnityEngine.UI;

public class BasicsMsfStarterUi : MonoBehaviour
{
    public InputField Port;
    public Button StartMasterButton;

	// Use this for initialization
	void Start () {
        StartMasterButton.onClick.AddListener(OnStartMasterClick);

    }
	
	// Update is called once per frame
	void Update () {

        StartMasterButton.gameObject.SetActive(!MasterServerBehaviour.IsMasterRunning);

    }

    public void OnStartMasterClick()
    {
        var master = FindObjectOfType<MasterServerBehaviour>();
        
        master.StartServer(int.Parse(Port.text));
    }
}
