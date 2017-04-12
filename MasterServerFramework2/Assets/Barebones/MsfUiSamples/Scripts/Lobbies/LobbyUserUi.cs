using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Represents a view of a single user within the lobby
    /// </summary>
    public class LobbyUserUi : MonoBehaviour
    {
        public Text Username;
        public Image ReadyBackground;
        public Text ReadyText;

        public string ReadyNotification = "READY";
        public Color ReadyColor = new Color(89 / 255f, 159 / 255f, 41 / 255f);

        public string NotReadyNotification = "NOT READY";
        public Color NotReadyColor = new Color(0.4f, 0.4f, 0.4f);

        /// <summary>
        /// Raw data, which was received when setting up
        /// </summary>
        public LobbyMemberData RawData { get; protected set; }

        /// <summary>
        /// Sets up the view from the data given
        /// </summary>
        /// <param name="data"></param>
        public void Setup(LobbyMemberData data)
        {
            RawData = data;
            Username.text = data.Username;
            SetReady(data.IsReady);
        }

        /// <summary>
        /// True, if this is the current player
        /// </summary>
        public bool IsCurrentPlayer { get; set; }

        /// <summary>
        /// True, if this player is the master
        /// </summary>
        public bool IsMaster { get; set; }

        /// <summary>
        /// True, if user is set to "Ready"
        /// </summary>
        public bool IsReady { get; protected set; }

        /// <summary>
        /// Changes users "readyness" (only visually)
        /// </summary>
        /// <param name="isReady"></param>
        public void SetReady(bool isReady)
        {
            IsReady = isReady;
            ReadyBackground.color = isReady ? ReadyColor : NotReadyColor;
            ReadyText.text = isReady ? ReadyNotification : NotReadyNotification;
        }

        public void SetReadyStatusVisibility(bool isVisible)
        {
            if (gameObject == null)
                return;

            ReadyBackground.gameObject.SetActive(isVisible);
        }
        
        /// <summary>
        /// Resets the user view
        /// </summary>
        public virtual void Reset()
        {
            Username.text = "";
            SetReady(false);
        }

    }
}