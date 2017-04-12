using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;

public class DemoPlayerProfiles : ServerModuleBehaviour
{
    public const int CoinsKey = 777;
    public const int WeaponKey = 778;

    public HelpBox _header = new HelpBox()
    {
        Text = "This script is a custom module, which sets up profiles module " +
               "for the demo"
    };

    void Awake()
    {
        // Request for profiles module
        AddOptionalDependency<ProfilesModule>();
    }

    public override void Initialize(IServer server)
    {
        base.Initialize(server);

        var profilesModule = server.GetModule<ProfilesModule>();

        if (profilesModule == null)
            return;

        // Set the new factory
        profilesModule.ProfileFactory = CreateProfileInServer;

        Logs.Warn("Created Demo profile factory");
    }

    /// <summary>
    /// This method generates a "scheme" for profile on the server side
    /// </summary>
    /// <param name="username"></param>
    /// <param name="peer"></param>
    /// <returns></returns>
    public static ObservableServerProfile CreateProfileInServer(string username, IPeer peer)
    {
        return new ObservableServerProfile(username, peer)
        {
            // Start with 10 coins by default
            new ObservableInt(CoinsKey, 10),
            new ObservableString(WeaponKey,"Carrot")
        };
    }

    public static ObservableServerProfile CreateProfileInServer(string username)
    {
        return CreateProfileInServer(username, null);
    }


}
