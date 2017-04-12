using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.UI;

public class DemoMasterSpawnerScene : MonoBehaviour
{
    public Button StartMasterBtn;
    public Button StartSpawnerBtn;

    public MasterServerBehaviour MasterServer;
    public SpawnerBehaviour Spawner;

    void Awake()
    {
        MasterServer = MasterServer ?? FindObjectOfType<MasterServerBehaviour>();
        Spawner = Spawner ?? FindObjectOfType<SpawnerBehaviour>();
    }

    // Update is called once per frame
    void Update()
    {
        var isMasterRunning = MasterServer.IsRunning;

        // Show/ hide Start Master button
        StartMasterBtn.gameObject.SetActive(!isMasterRunning);

        var isSpawnerRunning = Spawner.IsSpawnerStarted;

        StartSpawnerBtn.gameObject.SetActive(isMasterRunning && !isSpawnerRunning);
    }

    public void OnStartMasterClick()
    {
        MasterServer.StartServer(MasterServer.Port);
    }

    public void OnStartSpawnerClick()
    {
        Spawner.StartSpawner();
    }
}
