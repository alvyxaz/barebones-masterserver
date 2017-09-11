using System.Collections;
using Barebones.MasterServer;
using UnityEngine;
using System.Collections.Generic;

public class ProfilesTestScript : MonoBehaviour
{

    private string _username;
    private ObservableDictStringInt _serverInventory;

	// Use this for initialization
	void Start ()
	{
	    StartCoroutine(WaitForMasterToStart());
	}

    private IEnumerator WaitForMasterToStart()
    {
        var master = FindObjectOfType<MasterServerBehaviour>();

        do yield return null;
        while (!master.IsRunning);

        // Get the profiles module
        var profilesModule = FindObjectOfType<ProfilesModule>();

        var initialInventory = new Dictionary<string, int>();
        initialInventory["Wood"] = 10;
        // Set the profile factory
        profilesModule.ProfileFactory = (username, peer) => new ObservableServerProfile(username)
        {
            // Define all of the properties with default values
            new ObservableInt(MyProfileKeys.Coins, 10),
            new ObservableInt(MyProfileKeys.Score, 0),
            new ObservableString(MyProfileKeys.Title, "DefaultTitle"),
            new ObservableDictStringInt(MyProfileKeys.Inventory, initialInventory)
            // And so on, and on...
        };

        // Imitate the client
        StartCoroutine(ImitateClient());
    }

    private IEnumerator ImitateClient()
    {
        var connection = Msf.Connection;

        connection.Connect("127.0.0.1" ,5000);

        // Wait until connected to master
        while (!connection.IsConnected)
            yield return null;

        //Msf.Client.Auth.LogInAsGuest((accountInfo, error) =>
        Msf.Client.Auth.LogIn("aaaa", "aaaa", (accountInfo, error) =>
        {
            if (accountInfo == null)
            {
                Logs.Error(error);
                return;
            }

            _username = accountInfo.Username;

            // Create a profile (we're intentionally not constructing all properties)
            var profile = new ObservableProfile()
            {
                new ObservableInt(MyProfileKeys.Coins, 5),
                new ObservableString(MyProfileKeys.Title, "DefaultTitle"),
                new ObservableDictStringInt(MyProfileKeys.Inventory),
            };

            // Send a request to master server, to fill profile values
            Msf.Client.Profiles.GetProfileValues(profile, (isSuccessful, profileError) =>
            {
                if (!isSuccessful)
                {
                    Logs.Error(profileError);
                    return;
                }

                // Listen to property updates
                profile.PropertyUpdated += (code, property) =>
                {
                    // Log a message, when property changes
                    Logs.Info("Property changed:" + code + " - " + property.SerializeToString());
                };

                // Imitate game server
                StartCoroutine(ImitateGameServer());
            }, connection);

            // Listen directly to changes in coins property
            var coinsProp = profile.GetProperty<ObservableInt>(MyProfileKeys.Coins);
            coinsProp.OnDirty += property =>
            {
                Logs.Info("Coins changed to: " + coinsProp.Value);

                // OR
                // Logs.Info("Coins changed to: " + (property as ObservableInt).Value);
            };

        }, connection);
    }

    private IEnumerator ImitateGameServer()
    {
        var connection = Msf.Advanced.ClientSocketFactory();

        connection.Connect("127.0.0.1", 5000);

        // Wait until connected to master
        while (!connection.IsConnected)
            yield return null;

        // Construct the profile
        var profile = new ObservableServerProfile(_username)
        {
            new ObservableInt(MyProfileKeys.Coins, 5),
            new ObservableString(MyProfileKeys.Title, "DefaultTitle"),
            new ObservableDictStringInt(MyProfileKeys.Inventory),
        };

        // Fill profile values
        Msf.Server.Profiles.FillProfileValues(profile, (successful, error) =>
        {
            if (!successful)
            {
                Logs.Error(error);
                return;
            }

            // Modify the profile (changes will automatically be sent to the master server)
            profile.GetProperty<ObservableInt>(MyProfileKeys.Coins).Add(4);
            profile.GetProperty<ObservableString>(MyProfileKeys.Title).Set("DifferentTitle");
            _serverInventory = profile.GetProperty<ObservableDictStringInt>(MyProfileKeys.Inventory);
            _serverInventory.SetValue("Wood", _serverInventory.GetValue("Wood") + 10);

        }, connection);

    }

    // Update is called once per frame
    void Update () {

    }
}
