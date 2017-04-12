using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barebones.MasterServer
{
    public class MsfUi : MonoBehaviour
    {
        public static MsfUi Instance;

        public HelpBox _header = new HelpBox()
        {
            Text = "It's not necessary to use this component, but it makes " +
                   "sure that 'dialog box' and 'loading' elements " +
                   "subscribe to events. Make sure it's a child of Canvas component",
            Type = HelpBoxType.Info,
            Height = 50
        };

        public DialogBoxUi DialogBox;
        public LoadingUi Loading;

        public CreateGameUi CreateGameWindow;
        public CreateGameProgressUi CreateGameProgressWindow;
        public GamesListUi GamesListWindow;
        public ClientConnectionStatusUi ConnectionStatus;

        public LobbyUi LobbyUi;
        public LobbyCreateUi LobbyCreate;

        protected virtual void Awake()
        {
            if (Msf.Args.DestroyUi)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DialogBox = DialogBox ?? FindObjectOfType<DialogBoxUi>();
            Loading = Loading ?? FindObjectOfType<LoadingUi>();

            SubscribeToEvents();
        }

        protected virtual void Start()
        {
            DisplayLobbyWindowIfInLobby();
        }

        protected virtual void DisplayLobbyWindowIfInLobby()
        {
            var lastLobby = Msf.Client.Lobbies.LastJoinedLobby;
            if (lastLobby != null && !lastLobby.HasLeft)
            {
                lastLobby.SetListener(LobbyUi);
                LobbyUi.gameObject.SetActive(true);
            }
        }

        protected virtual void SubscribeToEvents()
        {
            if (DialogBox != null)
                DialogBox.SubscribeToEvents();

            if (Loading != null)
                Loading.SubscribeToEvents();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}