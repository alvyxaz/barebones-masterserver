using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEngine;

public class ChatTest : MonoBehaviour {

	// Use this for initialization
	void Awake () {
        Msf.Connection.AddConnectionListener(OnConnected, true);
    }

    private void OnConnected()
    {
        Msf.Client.Auth.LogInAsGuest(((successful, error) =>
        {
            Msf.Client.Chat.JoinChannel("Miau", (ss, ee) =>
            {
                Logs.Error(ee);
            });
        }));

        Msf.Client.Chat.MessageReceived += message =>
        {
            Logs.Debug("Chat msg: " + message.Sender + "|" + message.Message);

        };

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Msf.Connection.SendMessage(777, "NewName-" + Random.Range(0, 1000));
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Msf.Client.Chat.SendChannelMessage("Miau", "Works", (successful, error) =>
            {
                Logs.Error(error);
            });
        }
    }

}
