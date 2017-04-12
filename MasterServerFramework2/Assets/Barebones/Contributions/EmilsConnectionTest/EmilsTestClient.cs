/*-------------------------------------------------
 *    Big thanks to Emil Rainero for contributing this script!
 *--------------------------------------------------*/

using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;

/// <summary>
/// Big thanks to Emil Rainero for contributing this script!
/// </summary>
public class EmilsTestClient : MonoBehaviour {

    public string ServerIpAddress = "127.0.0.1";
    public int Port = 777;
    public bool UseWs = true;
    public bool AutoStartClient = true;

    private IClientSocket _client;
    private const int _messageOpCode = 0;

    void Start()
    {
        ParseCommandLineArguments();
        
        if (AutoStartClient)
        {
            StartClient();
        }
    }

    private void StartClient()
    {
        if (UseWs)
            _client = new ClientSocketWs();
        else _client = new ClientSocketUnet();

        _client.Connected += Connected;
        _client.Disconnected += Disconnected;
        _client.SetHandler(new PacketHandler(_messageOpCode, HandleMessage));

        Debug.Log("Client: Trying to connect to " + ServerIpAddress + ":" + Port);
        _client.Connect(ServerIpAddress, Port);
    }

    private void ParseCommandLineArguments()
    {
        AutoStartClient = Msf.Args.IsProvided("-startClient") ? true : AutoStartClient;
        UseWs = Msf.Args.IsProvided("-useWs") ? true : UseWs;
        UseWs = !Msf.Args.IsProvided("-useUnet") ? false : UseWs;

        if (Msf.Args.IsProvided("-port"))
        {
            Port = Msf.Args.ExtractValueInt("-port");
        }
        if (Msf.Args.IsProvided("-serverIp"))
        {
            ServerIpAddress = Msf.Args.ExtractValue("-serverIp");
        }
#if UNITY_EDITOR
        AutoStartClient = true;
#endif
    }

    private void Connected()
    {
        Debug.Log("Client: Connected");
    }

    private void Disconnected()
    {
        Debug.Log("Client: Disconnected");
    }

    private void HandleMessage(IIncommingMessage msg)
    {
        Debug.Log("Client: Got message " + msg);
    }
}
