using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Represents a lobby chat window
    /// 
    /// </summary>
    public class LobbyChatUi : MonoBehaviour
    {
        [Header("Settings")]
        public bool FocusOnEnterClick = true;

        // Max messages in the window
        public int MaxMessages = 20;

        public Color NormalColor = Color.white;
        public Color ErrorColor = Color.red;

        private Queue<Text> _currentMessages;

        private bool _allowFocusOnEnter = true;
        private bool _wasFocused = false;

        [Header("Components")]
        public Text MessagePrefab;
        public LayoutGroup MessagesList;
        public InputField InputField;

        protected LobbyUi Lobby;

        void Awake()
        {
            Lobby = Lobby ?? GetComponentInParent<LobbyUi>();

            _currentMessages = new Queue<Text>();

            // Listen to "submit on enter" events
            InputField.onEndEdit.AddListener(val =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    OnSendClick();
                }
            });
        }

        protected virtual void Update()
        {
            // Ignore, if field's not wisible
            if (!InputField.gameObject.activeSelf)
                return;

            // Focus Change event handling
            if (InputField.isFocused != _wasFocused)
                _wasFocused = InputField.isFocused;

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
        /// Invoked, when user submits a message
        /// </summary>
        public virtual void OnSendClick()
        {
            var text = InputField.text;
            if (string.IsNullOrEmpty(text))
                return;

            // Send chat message
            Lobby.JoinedLobby.SendChatMessage(InputField.text);

            // Refresh the view
            InputField.text = "";
            InputField.DeactivateInputField();

            // Workaround for not restoring focus instantly after sending a message with
            // "Return" key
            if (_allowFocusOnEnter)
                StartCoroutine(DontAllowFocusOnEnter());
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

        /// <summary>
        /// Returns a text object, which should be used
        /// to construct a new message
        /// </summary>
        /// <returns></returns>
        protected Text GetTextObject()
        {
            Text text;

            if (_currentMessages.Count >= MaxMessages)
            {
                text = _currentMessages.Dequeue();
            }
            else
            {
                text = Instantiate(MessagePrefab);
                _currentMessages.Enqueue(text);
            }

            text.gameObject.SetActive(true);
            text.color = NormalColor;
            text.transform.SetParent(MessagesList.transform, false);
            text.transform.SetAsLastSibling();
            _currentMessages.Enqueue(text);

            return text;
        }

        public virtual void OnMessageReceived(LobbyChatPacket msg)
        {
            var text = GetTextObject();
            text.text = GenerateMessageText(msg);
        }

        public virtual string GenerateMessageText(LobbyChatPacket packet)
        {
            if (packet.IsError)
            {
                return string.Format(ToColoredText("[{0}]: {1}", ErrorColor), packet.Sender, packet.Message);
            }
            else
            {
                return string.Format("[{0}]: {1}", packet.Sender, packet.Message);
            }
        }

        public virtual void WriteError(string error)
        {
            var text = GetTextObject();
            text.text = ToColoredText("[Error] " + error, Color.red);
        }

        public void Clear()
        {
            if (_currentMessages != null)
            {
                foreach (var msg in _currentMessages)
                {
                    msg.gameObject.SetActive(false);
                }
            }
        }
    }
}