
using Barebones.MasterServer;
using UnityEngine;

public class ChatApiExample : MonoBehaviour
{
    void Awake()
    {
        // Subscribe to events
        Msf.Client.Chat.MessageReceived += OnMessageReceived;
        Msf.Client.Chat.UserJoinedChannel += OnUserJoinedChannel;
        Msf.Client.Chat.UserLeftChannel += OnUserLeftChannel;
    }

    private void OnUserLeftChannel(string channel, string user)
    {
        Debug.Log(string.Format("[{0}] User '{1}' has left", channel, user));
    }

    private void OnUserJoinedChannel(string channel, string user)
    {
        Debug.Log(string.Format("[{0}] User '{1}' has joined", channel, user));
    }

    private void OnMessageReceived(ChatMessagePacket message)
    {
        switch (message.Type)
        {
            case ChatMessagePacket.PrivateMessage:
                // Received a private message
                Debug.Log(string.Format("From [{0}]: {1}",
                    message.Sender, // Channel name
                    message.Message));
                break;

            case ChatMessagePacket.ChannelMessage:
                // Received a channel message
                Debug.Log(string.Format("[{0}] [{1}]: {2}", 
                    message.Receiver, // Channel name
                    message.Sender,
                    message.Message));
                break;
        }

    }

    void OnDestroy()
    {
        // Unsubscribe from events
        Msf.Client.Chat.MessageReceived -= OnMessageReceived;
        Msf.Client.Chat.UserJoinedChannel -= OnUserJoinedChannel;
        Msf.Client.Chat.UserLeftChannel -= OnUserLeftChannel;
    }
}
