using System.Collections.Generic;
using System.ComponentModel;
using Barebones.Networking;
using Barebones.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    /// <summary>
    ///     Represents a list of game servers
    /// </summary>
    public class GamesListUi : MonoBehaviour
    {
        private GenericUIList<GameInfoPacket> _items;
        public GameObject CreateRoomWindow;

        public Button GameJoinButton;
        public GamesListUiItem ItemPrefab;
        public LayoutGroup LayoutGroup;

        protected IClientSocket Connection = Msf.Connection;

        // Use this for initialization
        protected virtual void Awake()
        {
            _items = new GenericUIList<GameInfoPacket>(ItemPrefab.gameObject, LayoutGroup);

            Connection.Connected += OnConnectedToMaster;
        }

        protected virtual void HandleRoomsShowEvent(object arg1, object arg2)
        {
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            if (Connection.IsConnected)
                RequestRooms();
        }

        protected void OnConnectedToMaster()
        {
            // Get rooms, if at the time of connecting the lobby is visible
            if (gameObject.activeSelf)
                RequestRooms();
        }

        public void Setup(IEnumerable<GameInfoPacket> data)
        {
            _items.Generate<GamesListUiItem>(data, (packet, item) => { item.Setup(packet); });
            UpdateGameJoinButton();
        }

        private void UpdateGameJoinButton()
        {
            GameJoinButton.interactable = GetSelectedItem() != null;
        }

        public GamesListUiItem GetSelectedItem()
        {
            return _items.FindObject<GamesListUiItem>(item => item.IsSelected);
        }

        public void Select(GamesListUiItem gamesListItem)
        {
            _items.Iterate<GamesListUiItem>(item => { item.SetIsSelected(!item.IsSelected && (gamesListItem == item)); });
            UpdateGameJoinButton();
        }

        public void OnRefreshClick()
        {
            RequestRooms();
        }

        public void OnJoinGameClick()
        {
            var selected = GetSelectedItem();

            if (selected == null)
                return;

            if (selected.IsLobby)
            {
                OnJoinLobbyClick(selected.RawData);
                return;
            }

            if (selected.IsPasswordProtected)
            {
                // If room is password protected
                var dialogData = DialogBoxData
                    .CreateTextInput("Room is password protected. Please enter the password to proceed", password =>
                    {
                        Msf.Client.Rooms.GetAccess(selected.GameId, OnPassReceived, password);
                    });

                if (!Msf.Events.Fire(Msf.EventNames.ShowDialogBox, dialogData))
                {
                    Logs.Error("Tried to show an input to enter room password, " +
                               "but nothing handled the event: " + Msf.EventNames.ShowDialogBox);   
                }

                return;
            }

            // Room does not require a password
            Msf.Client.Rooms.GetAccess(selected.GameId, OnPassReceived);
        }

        protected virtual void OnJoinLobbyClick(GameInfoPacket packet)
        {
            var loadingPromise = Msf.Events.FireWithPromise(Msf.EventNames.ShowLoading, "Joining lobby");

            Msf.Client.Lobbies.JoinLobby(packet.Id, (lobby, error) =>
            {
                loadingPromise.Finish();

                if (lobby == null)
                {
                    Msf.Events.Fire(Msf.EventNames.ShowDialogBox, DialogBoxData.CreateError(error));
                    return;
                }

                // Hide this window
                gameObject.SetActive(false);

                var lobbyUi = FindObjectOfType<LobbyUi>();

                if (lobbyUi == null && MsfUi.Instance != null)
                {
                    // Try to get it through MsfUi
                    lobbyUi = MsfUi.Instance.LobbyUi;
                }

                if (lobbyUi == null)
                {
                    Logs.Error("Couldn't find appropriate UI element to display lobby data in the scene. " +
                               "Override OnJoinLobbyClick method, if you want to handle this differently");
                    return;
                }

                lobbyUi.gameObject.SetActive(true);
                lobby.SetListener(lobbyUi);
            });
        }

        protected virtual void OnPassReceived(RoomAccessPacket packet, string errorMessage)
        {
            if (packet == null)
            {
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox, DialogBoxData.CreateError(errorMessage));
                Logs.Error(errorMessage);
                return;
            }
            
            // Hope something handles the event
        }

        protected virtual void RequestRooms()
        {
            if (!Connection.IsConnected)
            {
                Logs.Error("Tried to request rooms, but no connection was set");
                return;
            }

            var loadingPromise = Msf.Events.FireWithPromise(Msf.EventNames.ShowLoading, "Retrieving Rooms list...");

            Msf.Client.Matchmaker.FindGames(games =>
            {
                loadingPromise.Finish();

                Setup(games);
            });
        }

        public void OnCreateGameClick()
        {
            if (CreateRoomWindow == null)
            {
                Logs.Error("You need to set a CreateRoomWindow");
                return;
            }
            CreateRoomWindow.gameObject.SetActive(true);
        }

        public void OnCloseClick()
        {
            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            Msf.Connection.Connected -= OnConnectedToMaster;
        }
    }
}