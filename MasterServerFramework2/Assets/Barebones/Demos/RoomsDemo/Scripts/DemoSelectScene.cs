using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoSelectScene : MonoBehaviour
{
    public SceneField ClientScene;
    public SceneField GameScene;
    public SceneField MasterAndSpawnerScene;
    public SceneField SpawnerScene;

    // Use this for initialization
    void Start () {

        // Automatically select appropriate scenes

        if (Msf.Args.IsProvided(Msf.Args.Names.LoadScene))
        {
            // If an argument was provided to load a specific scene
            SceneManager.LoadScene(Msf.Args.LoadScene);
            return;
        }

        if (Msf.Args.IsProvided(Msf.Args.Names.StartMaster))
        {
            // If arguments are provided to start both master and spawner servers
            SelectMasterSpawnerScene();
            return;
        }

        if (Msf.Args.IsProvided(Msf.Args.Names.StartSpawner))
        {
            SelectSpawnerScene();
            return;
        }

        if (Msf.Args.IsProvided(Msf.Args.Names.SpawnCode))
        {
            // If spawn code is provided, let's consider that we want a game server to be spawned
            SelectGameScene();
            return;
        }
    }

    // Update is called once per frame
    void Update () {
		
	}

    public void SelectClientScene()
    {
        SceneManager.LoadScene(ClientScene.SceneName);
    }

    public void SelectGameScene()
    {
        SceneManager.LoadScene(GameScene.SceneName);
    }

    public void SelectMasterSpawnerScene()
    {
        SceneManager.LoadScene(MasterAndSpawnerScene.SceneName);
    }

    public void SelectSpawnerScene()
    {
        SceneManager.LoadScene(SpawnerScene.SceneName);
    }
}
