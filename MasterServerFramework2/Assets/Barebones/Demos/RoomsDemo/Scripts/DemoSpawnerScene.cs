using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.UI;

public class DemoSpawnerScene : MonoBehaviour
{
    public Button StartSpawnerButton;

    public SpawnerBehaviour Spawner;

    void Awake()
    {
        Spawner = Spawner ?? FindObjectOfType<SpawnerBehaviour>();
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var isSpawnerStarted = Spawner.IsSpawnerStarted;
        var isConnectedToMaster = Msf.Server.Spawners.Connection.IsConnected;
        var isButtonVisible = !isSpawnerStarted && isConnectedToMaster;

        StartSpawnerButton.gameObject.SetActive(isButtonVisible);
    }

    public void OnStartSpawnerClick()
    {
        Spawner.StartSpawner();
    }
}
