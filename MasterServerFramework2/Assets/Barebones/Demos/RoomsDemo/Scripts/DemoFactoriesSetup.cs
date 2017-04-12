using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;

public class DemoFactoriesSetup : MonoBehaviour
{
    public HelpBox Header = new HelpBox()
    {
        Text = "This script adds demo lobby factories to the module " +
               "(1vs1, Deathmatch, 2vs2vs4 and 3vs3)"
    };

    // Use this for initialization
    void Start()
    {
        // Add demo lobby factories

        var module = FindObjectOfType<LobbiesModule>();

        module.AddFactory(new LobbyFactoryAnonymous("1 vs 1",
            module, DemoLobbyFactories.OneVsOne));

        module.AddFactory(new LobbyFactoryAnonymous("Deathmatch",
            module, DemoLobbyFactories.Deathmatch));

        module.AddFactory(new LobbyFactoryAnonymous("2 vs 2 vs 4",
            module, DemoLobbyFactories.TwoVsTwoVsFour));

        module.AddFactory(new LobbyFactoryAnonymous("3 vs 3 auto",
            module, DemoLobbyFactories.ThreeVsThreeQueue));
    }
}
