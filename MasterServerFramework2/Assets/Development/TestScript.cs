using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;
using UnityEngine.Networking.NetworkSystem;

public class TestScript : MonoBehaviour, ILobbyListener {

	// Use this for initialization
	void Start () {
        Msf.Connection.AddConnectionListener(OnConnected, true);
	}

    //private JoinedLobby _firstLobby;

    private void OnConnected()
    {
        Msf.Client.Auth.LogInAsGuest(((successful, error) => { }));

        // Sending a Unet messages (instances of MessageBase) to server
        Msf.Connection.SendMessage(777, new IntegerMessage(5));

        // Sending a string message to server, and expecting for response
        Msf.Connection.SendMessage(777, "My message", (status, response) =>
        {
            if (status == ResponseStatus.Success)
            {
                Logs.Error("Request was successful!");
            }
        });

        // Login, create lobby and join it
        //Msf.Client.Auth.LogInAsGuest((successful, authError) =>
        //{
        //    Msf.Client.Lobbies.CreateLobby("deathmatch", new Dictionary<string, string>(), (id, error) =>
        //    {
        //        Msf.Client.Lobbies.JoinLobby(id.Value, (lobby, joinError) =>
        //        {
        //            lobby.SetListener(this);

        //            Debug.Log("Joined another lobby: " + joinError + lobby);

        //            // Create another lobby after leaving
        //            lobby.Leave(CreateAnotherLobby);
        //        });
        //    });
        //});

        //CreateAnotherLobby();
    }

    public void CreateAnotherLobby()
    {

        Msf.Client.Lobbies.CreateAndJoin("2vs2vs4", new Dictionary<string, string>(), (lobby, error) =>
        {
            if (lobby == null)
                Logs.Error(error);

            //_firstLobby = lobby;

            var lobbyUi = FindObjectOfType<LobbyUi>();
            lobby.SetListener(lobbyUi);

            lobby.SetLobbyProperty("cookie", "tasty", (successful, setError) =>
            {
                if (!successful) Debug.Log(setError);
            });

            lobby.SendChatMessage("This is a message!");

            // Add another user to the lobby
            JoinAsOtherMember("Some FancyTesting");
        });

    }

    private void JoinAsOtherMember(string username)
    {
        var connection = Msf.Advanced.ClientSocketFactory();

        // Use another connection to imitate another user
        connection.Connected += () =>
        {
            Msf.Client.Auth.LogInAsGuest((successful, authError) =>
            {
                Msf.Client.Lobbies.JoinLobby(0, (lobby, joinError) =>
                {
                    Debug.Log("Fancy tester joined the lobby: " + joinError + lobby);

                    lobby.SetReadyStatus(true);

                    //_firstLobby.Leave();

                    //lobby.Leave();
                }, connection);
            }, connection);
        };

        connection.Connect("127.0.0.1", 5000);
    }

    // Update is called once per frame
	void Update () {
		
	}

    #region ILobbyListener

    private JoinedLobby _lobby;

    public void Initialize(JoinedLobby lobby)
    {
        _lobby = lobby;
    }

    public void OnMemberPropertyChanged(LobbyMemberData member, string property, string value)
    {
        Logs.Error("Member property changed: " + member.Properties[property]);
    }

    public void OnMemberJoined(LobbyMemberData member)
    {
        Logs.Error("Member joined: " + member.Username);
    }

    public void OnMemberLeft(LobbyMemberData member)
    {
        Logs.Error("Member left: " + member.Username);
    }

    public void OnLobbyLeft()
    {
        Logs.Error("Lobby left!");
    }

    public void OnChatMessageReceived(LobbyChatPacket packet)
    {
        Logs.Error("Chat message received: " + packet.Message + " | BY " + packet.Sender);
    }

    public void OnLobbyPropertyChanged(string property, string value)
    {
        Logs.Error("Lobby property changed: " + _lobby.Properties[property]);
    }

    public void OnMasterChanged(string masterUsername)
    {
        Logs.Error("Master changed to:" + masterUsername);
    }

    public void OnMemberReadyStatusChanged(LobbyMemberData member, bool isReady)
    {
        Logs.Error("Member is READY:" + member.Username + " - " + isReady);
    }

    public void OnMemberTeamChanged(LobbyMemberData member, LobbyTeamData team)
    {
        Logs.Error("Member switched teams:" + member.Username + " - " + team.Name);
    }

    public void OnLobbyStatusTextChanged(string statusText)
    {
        Logs.Error("Lobby status text changed: " + statusText);
    }

    public void OnLobbyStateChange(LobbyState state)
    {
        Logs.Error("Lobby state changed: " + state);
    }

    #endregion
}
