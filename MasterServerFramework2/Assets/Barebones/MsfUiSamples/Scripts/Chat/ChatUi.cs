using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine.UI;

/// <summary>
/// This is a relatively simple and hack'ish chat UI, which
/// uses Chat Module API
/// </summary>
public class ChatUi : MonoBehaviour
{
    public event Action<bool> OnInputFocusChange;

    [Header("Settings")]
    public bool FocusOnEnterClick = true;

    // Max messages in the window
    public int MaxMessages = 20;

    /// <summary>
    /// List of channels, that will be joined automatically once logged in
    /// </summary>
    public string[] AutoJoinChannels = new[] {"Global"};

    public string ChannelJoinedMessage = "You have joined a channel '{0}'";

    public string VisibilityPrefKey = "bm.chat.isVisible";

    /// <summary>
    /// List of available chat window sizes
    /// </summary>
    public int[] AvailableSizes = new[] { 150, 200, 250, 300 };

    /// <summary>
    /// If client receives a message from channel which contains at least one word
    /// from this list, channel name will be hidden
    /// </summary>
    public string[] ChannelMasks = new[] { "Game-" };

    [Header("Colors")]
    public Color NormalColor = Color.white;
    public Color InfoColor = Color.gray;
    public Color ErrorColor = Color.red;
    public Color ChannelColor = Color.green;
    public Color LocalColor = Color.white;
    public Color PrivateColor = Color.magenta;

    [Header("Components")]
    public Image MessagesBg;
    public Text MessagePrefab;

    public GameObject Messages;
    public LayoutGroup MessagesList;
    public GameObject InputRow;
    public GameObject ChatControls;
    public InputField InputField;

    private RectTransform _chatRect;
    private Color _bgColor;
    private Queue<Text> _currentMessages;
    protected string LastWhisperFrom = "";

    private bool _allowFocusOnEnter = true;
    private bool _wasFocused = false;

    protected virtual void Awake()
    {
        _currentMessages = new Queue<Text>();
        _chatRect = GetComponent<RectTransform>();
        _bgColor = MessagesBg.color;

        // Turn channel masks to lowercase, so we don't need to do it more than once
        ChannelMasks = ChannelMasks.Select(c => c.ToLower()).ToArray();

        // Listen to "submit on enter" events
        InputField.onEndEdit.AddListener(val =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnSendClick();
            }
        });

        SetChatVisibility(PlayerPrefs.GetInt(VisibilityPrefKey, 1) > 0);
        
    }

    protected virtual void Start()
    {
        // Subscribe to events
        Msf.Client.Auth.LoggedIn += OnLoggedIn;
        Msf.Client.Chat.MessageReceived += OnMessageReceived;
        Msf.Client.Chat.UserJoinedChannel += OnUserJoinedChannel;
        Msf.Client.Chat.UserLeftChannel += OnUserLeftChannel;

        // Invoke manually, in case we're already logged in
        if (Msf.Client.Auth.IsLoggedIn)
            OnLoggedIn();
    }

    protected virtual void OnUserLeftChannel(string channel, string user)
    {
        PushInfoMessage(string.Format("Player '{0}' left '{1}'", user, channel));
    }

    protected virtual void OnUserJoinedChannel(string channel, string user)
    {
        PushInfoMessage(string.Format("Player '{0}' joined '{1}'", user, channel));
    }

    protected virtual void OnMessageReceived(ChatMessagePacket packet)
    {
        switch (packet.Type)
        {
            case ChatMessagePacket.ChannelMessage:
                PushChannelMessage(packet);
                break;
            case ChatMessagePacket.PrivateMessage:
                LastWhisperFrom = packet.Sender;
                PushPrivateMessage(packet.Sender, packet.Message);
                break;
        }
    }

    /// <summary>
    /// Invoked, when user logs in
    /// </summary>
    protected virtual void OnLoggedIn()
    {
        // Try to join the default channels
        foreach (var channel in AutoJoinChannels)
        {
            var channelName = channel;
            Msf.Client.Chat.JoinChannel(channel, (successful, error) =>
            {
                if (successful)
                {
                    PushInfoMessage(string.Format(ChannelJoinedMessage, channelName));
                }
            });
        }
    }

    /// <summary>
    /// Pushes an error message into chat window
    /// </summary>
    /// <param name="errorMessage"></param>
    protected virtual void PushErrorMessage(string errorMessage)
    {
        var text = GetTextObject();
        text.color = ErrorColor;
        text.text = string.Format("[Error]: {0}", errorMessage);
    }

    
    /// <summary>
    /// Pushes a confirmation that private message was successfully sent
    /// </summary>
    /// <param name="packet"></param>
    protected virtual void PushPrivateConfirmation(ChatMessagePacket packet)
    {
        var text = GetTextObject();
        text.color = PrivateColor;
        text.text = string.Format("To [{0}]: {1}", packet.Receiver, packet.Message);
    }

    /// <summary>
    /// Pushes a private message, which someone else sent, to chat window
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    protected virtual void PushPrivateMessage(string sender, string message)
    {
        var text = GetTextObject();
        text.color = PrivateColor;
        text.text = string.Format("[{0}] whispers: {1}", sender, message);
    }

    /// <summary>
    /// Pushes information message to the chat window
    /// </summary>
    /// <param name="errorMessage"></param>
    protected virtual void PushInfoMessage(string errorMessage)
    {
        var text = GetTextObject();
        text.text = errorMessage;

        text.color = InfoColor;
    }

    /// <summary>
    /// Pushes a normal message into chat window
    /// </summary>
    /// <param name="message"></param>
    protected virtual void PushNormalMessage(string message)
    {
        var text = GetTextObject();
        text.text = message;
    }

    /// <summary>
    /// Pushes a message, which was written in one of the joined channels,
    /// into the chat window
    /// </summary>
    /// <param name="packet"></param>
    protected virtual void PushChannelMessage(ChatMessagePacket packet)
    {
        var text = GetTextObject();

        // Mask channel name, if in the masked list
        var receiver = packet.Receiver.ToLower();
        if (ChannelMasks.Any(c => receiver.Contains(c)))
        {
            packet.Receiver = "";
        }

        if (string.IsNullOrEmpty(packet.Receiver))
        {
            // This is a local message
            text.color = LocalColor;
            text.text = string.Format("[{0}]: {1}", packet.Sender, packet.Message);
            return;
        }

        text.text = string.Format("[{0}] [{1}]: {2}", 
            ToColoredText(packet.Receiver, ChannelColor), packet.Sender, packet.Message);
    }

    /// <summary>
    /// Returns a text object, which should be used
    /// to construct a new message
    /// </summary>
    /// <returns></returns>
    protected Text GetTextObject()
    {
        var text = _currentMessages.Count >= MaxMessages ? 
            _currentMessages.Dequeue() : Instantiate(MessagePrefab);

        text.color = NormalColor;
        text.transform.SetParent(MessagesList.transform, false);
        text.transform.SetAsLastSibling();

        return text;
    }

    /// <summary>
    /// Increases chat window size
    /// </summary>
    public virtual void OnSizeIncreaseClick()
    {
        var newSize = AvailableSizes.OrderBy(i => i)
            .FirstOrDefault(i => i > _chatRect.rect.height);

        if (newSize > 0)
            _chatRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize);
    }

    /// <summary>
    /// Decreases chat window size
    /// </summary>
    public virtual void OnSizeDecreaseClick()
    {
        var newSize = AvailableSizes.OrderByDescending(i => i)
            .FirstOrDefault(i => i < _chatRect.rect.height);

        if (newSize > 0)
            _chatRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize);
    }

    /// <summary>
    /// Toggles chat window background's alpha color / visibility
    /// </summary>
    public virtual void OnToggleBgClick()
    {
        MessagesBg.color = MessagesBg.color.a < 0.1f ? 
            _bgColor : new Color(0, 0, 0, 0.01f);
    }

    protected virtual void Update()
    {
        // Ignore, if field's not wisible
        if (!InputField.gameObject.activeSelf)
            return;

        // Focus Change event handling
        if (InputField.isFocused != _wasFocused)
        {
            _wasFocused = InputField.isFocused;
            if (OnInputFocusChange != null)
                OnInputFocusChange.Invoke(InputField.isFocused);
        }

        // Focus, if return key was clicked
        if (FocusOnEnterClick && Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // On enter click
            if (_allowFocusOnEnter)
            {
                InputField.ActivateInputField();
            }
        }
    }

    /// <summary>
    /// Toggles chat visibility
    /// </summary>
    public virtual void OnToggleChat()
    {
        if (Messages.gameObject.activeSelf)
        {
            SetChatVisibility(false);
            PlayerPrefs.SetInt(VisibilityPrefKey, 0);
        }
        else
        {
            SetChatVisibility(true);
            PlayerPrefs.SetInt(VisibilityPrefKey, 1);
        }
    }

    public void SetChatVisibility(bool isVisible)
    {
        Messages.gameObject.SetActive(isVisible);
        InputRow.gameObject.SetActive(isVisible);
        ChatControls.gameObject.SetActive(isVisible);
    }

    /// <summary>
    /// Handles a message without commands
    /// Sometimes, you might want to do something special with it, but default implementation
    /// sends that message to server, which checks if user has a "local" channel, and sends
    /// a message to it
    /// </summary>
    /// <param name="message"></param>
    protected virtual void HandleLocalMessage(string message)
    {
        Msf.Client.Chat.SendToDefaultChannel(message, (successful, error) =>
        {
            if (!successful) PushErrorMessage(error);
        });
    }

    /// <summary>
    /// Handles chat commands
    /// </summary>
    /// <param name="message"></param>
    protected virtual void HandleCommand(string message)
    {
        var parts = message.Split(' ');
        var command = parts[0];

        if (command == "/w")
        {
            // Private message
            if (parts.Length < 3)
            {
                PushErrorMessage("Invalid whisper command. Example: /w Username message");
                return;
            }

            var privateMsg = new ChatMessagePacket()
            {
                Type = ChatMessagePacket.PrivateMessage,
                Receiver = parts[1],
                Message = string.Join(" ", parts.Skip(2).ToArray()),
            };

            // Send the message
            Msf.Client.Chat.SendMessage(privateMsg, (successful, error) =>
            {
                if (!successful)
                    PushErrorMessage(error);
                else
                    PushPrivateConfirmation(privateMsg);
            });

        } else if (command == "/c" || command == "/ch" || command == "/csay")
        {
            // Channel message
            if (parts.Length == 1)
            {
                Msf.Client.Chat.GetMyChannels(HandleReceivedChannelsList);
                return;
            }

            if (parts.Length < 3)
            {
                PushErrorMessage("Invalid channel message command. Example: /ch ChannelName message");
                return;
            }

            var channelMsg = new ChatMessagePacket()
            {
                Type = ChatMessagePacket.ChannelMessage,
                Receiver = parts[1],
                Message = string.Join(" ", parts.Skip(2).ToArray()),
            };

            // Send the message
            Msf.Client.Chat.SendMessage(channelMsg, (successful, error) =>
            {
                if (!successful) PushErrorMessage(error);
            });
        }
        else if (command == "/r")
        {
            if (string.IsNullOrEmpty(LastWhisperFrom))
            {
                PushErrorMessage("There's no one to reply to");
                return;
            }

            if (parts.Length < 2)
            {
                PushErrorMessage("Can't send an empty message");
                return;
            }

            var privateMsg = new ChatMessagePacket()
            {
                Type = ChatMessagePacket.PrivateMessage,
                Receiver = LastWhisperFrom,
                Message = string.Join(" ", parts.Skip(1).ToArray()),
            };

            // Send the message
            Msf.Client.Chat.SendMessage(privateMsg, (successful, error) =>
            {
                if (!successful)
                    PushErrorMessage(error);
                else
                    PushPrivateConfirmation(privateMsg);
            });
        } else if (command == "/join")
        {
            if (parts.Length < 2)
            {
                PushErrorMessage("To join a channel, you need to provide a name: /join ChannelName");
                return;
            }

            Msf.Client.Chat.JoinChannel(parts[1], (successful, error) =>
            {
                if (successful)
                {
                    PushInfoMessage(string.Format(ChannelJoinedMessage, parts[1]));
                }
                else
                {
                    PushErrorMessage(error);
                }
            });
        } else if (command == "/leave")
        {
            if (parts.Length < 2)
            {
                PushErrorMessage("To leave a channel, you need to provide a name: /leave ChannelName");
                return;
            }

            Msf.Client.Chat.LeaveChannel(parts[1], (successful, error) =>
            {
                if (successful)
                {
                    PushInfoMessage("Channel '"+ parts[1]+ "'left");
                }
                else
                {
                    PushErrorMessage(error);
                }
            });
        } else if (command == "/setLocal")
        {
            if (parts.Length < 2)
            {
                PushErrorMessage("To set a local channel, you need to provide a channel name: /setLocal ChannelName");
                return;
            }

            Msf.Client.Chat.SetDefaultChannel(parts[1], (successful, error) =>
            {
                if (successful)
                {
                    PushInfoMessage("Local channel set to '" + parts[1] + "'");
                }
                else
                {
                    PushErrorMessage("Failed to set a local channel: " + error);
                }
            });
        }
    }

    /// <summary>
    /// Handles a list of current channels, received after 
    /// a request
    /// </summary>
    protected virtual void HandleReceivedChannelsList(List<string> channels, string error)
    {
        if (channels == null)
        {
            PushErrorMessage("Channels list request failed");
            return;
        }

        if (channels.Count < 1)
        {
            PushInfoMessage("You didn't join any channels");
            return;
        }

        var stringList = string.Join(", ", channels.Select(c => "'" + c + "'").ToArray());
        PushInfoMessage("You're in these channels: " + stringList);
    }

    /// <summary>
    /// Invoked, when user submits a message
    /// </summary>
    public virtual void OnSendClick()
    {
        var text = InputField.text;
        if (string.IsNullOrEmpty(text))
            return;

        if (text[0] == '/')
        {
            HandleCommand(text);
        }
        else
        {
            HandleLocalMessage(text);
        }

        InputField.text = "";
        InputField.DeactivateInputField();

        // Workaround for not restoring focus instantly after sending a message with
        // "Return" key
        if (_allowFocusOnEnter)
            StartCoroutine(DontAllowFocusOnEnter());
    }

    void OnDestroy()
    {
        Msf.Client.Auth.LoggedIn -= OnLoggedIn;
        Msf.Client.Chat.MessageReceived -= OnMessageReceived;
        Msf.Client.Chat.UserJoinedChannel -= OnUserJoinedChannel;
        Msf.Client.Chat.UserLeftChannel -= OnUserLeftChannel;
    }

    /// <summary>
    /// Normally, after sending a message with "Return" key, focus is automatically
    /// returned back to the chat. This is a fix for the issue.
    /// </summary>
    /// <returns></returns>
    protected IEnumerator DontAllowFocusOnEnter()
    {
        _allowFocusOnEnter = false;
        yield return new WaitForSeconds(0.2f);
        _allowFocusOnEnter = true;
    }

    protected string ToColoredText(string message, Color color)
    {
        return string.Format("<color=#{0}>{1}</color>", ColorToHex(color), message);
    }

    protected string ColorToHex(Color32 color)
    {
        string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + color.a.ToString("X2");
        return hex;
    }

}
