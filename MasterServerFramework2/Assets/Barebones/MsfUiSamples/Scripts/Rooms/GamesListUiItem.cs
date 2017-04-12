using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    /// <summary>
    ///     Represents a single row in the games list
    /// </summary>
    public class GamesListUiItem : MonoBehaviour
    {
        public GameInfoPacket RawData { get; protected set; }
        public Image BgImage;
        public Color DefaultBgColor;
        public GamesListUi ListView;
        public GameObject LockImage;
        public Text MapName;
        public Text Name;
        public Text Online;

        public Color SelectedBgColor;

        public string UnknownMapName = "Unknown";

        public int GameId { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsLobby { get; private set; }

        public bool IsPasswordProtected
        {
            get { return RawData.IsPasswordProtected; }
        }

        // Use this for initialization
        private void Awake()
        {
            BgImage = GetComponent<Image>();
            DefaultBgColor = BgImage.color;

            SetIsSelected(false);
        }

        public void SetIsSelected(bool isSelected)
        {
            IsSelected = isSelected;
            BgImage.color = isSelected ? SelectedBgColor : DefaultBgColor;
        }

        public void Setup(GameInfoPacket data)
        {
            RawData = data;
            IsLobby = data.Type == GameInfoType.Lobby;
            SetIsSelected(false);
            Name.text = data.Name;
            GameId = data.Id;
            LockImage.SetActive(data.IsPasswordProtected);

            if (data.MaxPlayers > 0)
            {
                Online.text = string.Format("{0}/{1}", data.OnlinePlayers, data.MaxPlayers);
            }
            else
            {
                Online.text = data.OnlinePlayers.ToString();
            }

            MapName.text = data.Properties.ContainsKey(MsfDictKeys.MapName) 
                ? data.Properties[MsfDictKeys.MapName] : UnknownMapName;
        }

        public void OnClick()
        {
            ListView.Select(this);
        }
    }
}