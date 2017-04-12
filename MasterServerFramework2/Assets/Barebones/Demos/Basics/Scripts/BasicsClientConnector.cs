using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;
using UnityEngine.UI;

public class BasicsClientConnector : MonoBehaviour
{
    public Button ConnectBtn;
    public InputField Ip;
    public InputField Port;

    public Text StatusText;

	// Use this for initialization
	void Start () {
		ConnectBtn.onClick.AddListener(OnConnectClick);

	    Msf.Connection.StatusChanged += OnConnectionStatusChanged;

        // Call to set the initial state
	    OnConnectionStatusChanged(Msf.Connection.Status);
	}

    // Update is called once per frame
	void Update ()
	{

	    var showButton = Msf.Connection.Status == ConnectionStatus.Disconnected ||
                         Msf.Connection.Status == ConnectionStatus.None;

        ConnectBtn.gameObject.SetActive(showButton);
	}

    public void OnConnectClick()
    {
        Msf.Connection.Connect(Ip.text, int.Parse(Port.text));
    }

    private void OnConnectionStatusChanged(ConnectionStatus status)
    {
        StatusText.text = status.ToString();
    }
}
