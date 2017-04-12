using System.Collections;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class UnetMsfPlayer
{
    public NetworkConnection Connection { get; private set; }
    public PeerAccountInfoPacket AccountInfo { get; set; }

    public UnetMsfPlayer(NetworkConnection connection, PeerAccountInfoPacket accountInfo)
    {
        Connection = connection;
        AccountInfo = accountInfo;
    }

    public string Username { get { return AccountInfo.Username; } }
    public int PeerId { get { return AccountInfo.PeerId; } }

}
