
using System;
using System.Collections.Generic;
using Barebones.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    public class LobbyUi : MonoBehaviour, ILobbyListener
    {

        #region Unity inspector

        public LobbyUserUi UserPrefab;
        public Text LobbyName;
        public LobbyTeamUi TeamPrefab;
        public LayoutGroup TeamsLayoutGroup;
        public LobbyChatUi Chat;
        public GameObject LoadingScreen;
        public Text GameStatus;
        public Text PlayerCount;
        public Text LobbyType;

        public LobbyPropControllersUi PropControllers;

        public Button PlayButton;
        public Button ReadyButton;
        public Image ReadyButtonTick;

        public Button StartButton;
        
        /// <summary>
        /// Text, appended to the user who is game master
        /// </summary>
        public string MasterText = "<color=orange> (Master)</color>";

        /// <summary>
        /// Text, appended to user who is the "current user"
        /// </summary>
        public string CurrentPlayerText = "<color=green> (You)</color>";

        /// <summary>
        /// If true, when the game becomes ready, user joins automatically
        /// </summary>
        public bool AutoJoinGameWhenReady = true;

        #endregion

        protected Dictionary<string, LobbyTeamUi> Teams;
        protected GenericPool<LobbyTeamUi> TeamsPool;

        protected Dictionary<string, LobbyUserUi> Users;
        protected GenericPool<LobbyUserUi> UsersPool;

        protected bool IsReady;

        private bool _wasGameRunningWhenOpened;

        public JoinedLobby JoinedLobby { get; protected set; }

        public string CurrentUser { get; protected set; }

        protected virtual void Awake()
        {
            InitializeCollectionsIfNecessary();
        }

        protected virtual void InitializeCollectionsIfNecessary()
        {
            Teams = Teams ?? new Dictionary<string, LobbyTeamUi>();
            TeamsPool = TeamsPool ?? new GenericPool<LobbyTeamUi>(TeamPrefab);

            Users = Users ?? new Dictionary<string, LobbyUserUi>();
            UsersPool = UsersPool ?? new GenericPool<LobbyUserUi>(UserPrefab);
        }

        public virtual void Setup(JoinedLobby lobby)
        {
            Reset();

            _wasGameRunningWhenOpened = lobby.State == LobbyState.GameInProgress;

            CurrentUser = lobby.Data.CurrentUserUsername;

            LobbyName.text = lobby.LobbyName;
            LobbyType.text = lobby.Data.LobbyType;

            Teams.Clear();
            Users.Clear();

            // Setup teams
            foreach (var team in lobby.Teams)
            { 
                Teams.Add(team.Key, CreateTeamView(team.Key, team.Value));
            }

            // Setup users
            foreach (var player in lobby.Members)
                Users.Add(player.Key, CreateMemberView(player.Value));

            // Setup controls
            if (PropControllers != null)
                PropControllers.Setup(JoinedLobby.Data.Controls);

            // Current player's ready state
            if (lobby.Members.ContainsKey(CurrentUser))
                IsReady = lobby.Members[CurrentUser].IsReady;

            OnLobbyStateChange(lobby.State);
            OnMasterChanged(lobby.Data.GameMaster);

            UpdateTeamJoinButtons();
            UpdateReadyButton();
            UpdateStartGameButton();
            UpdatePlayerCount();
        }

        public void Reset()
        {
            // It's possible that this is called before awake
            InitializeCollectionsIfNecessary();

            PlayButton.gameObject.SetActive(false);
            LobbyName.text = "";

            // Cleanup teams
            foreach (var team in Teams)
            {
                TeamsPool.Store(team.Value);
                team.Value.Reset();
            }
            Teams.Clear();

            // Cleanup users
            foreach (var user in Users)
            {
                UsersPool.Store(user.Value);
                user.Value.Reset();
            }
            Users.Clear();

            // Clear the chat 
            if (Chat != null)
            {
                Chat.Clear();
            }
        }

        /// <summary>
        /// Creates the team view from the data given
        /// </summary>
        /// <param name="teamName"></param>
        /// <param name="teamProperties"></param>
        /// <returns></returns>
        protected virtual LobbyTeamUi CreateTeamView(string teamName,
            LobbyTeamData data)
        {
            var teamView = TeamsPool.GetResource();
            teamView.Setup(teamName, data);
            teamView.gameObject.SetActive(true);
            teamView.transform.SetParent(TeamsLayoutGroup.transform, false);
            teamView.transform.SetAsLastSibling();

            return teamView;
        }

        /// <summary>
        /// Creates user view from the date given
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual LobbyUserUi CreateMemberView(LobbyMemberData data)
        {
            // Get team
            var team = Teams[data.Team];

            // Get user view
            var user = UsersPool.GetResource();
            user.Reset();
            user.gameObject.SetActive(true);
            user.Setup(data);
            user.IsCurrentPlayer = data.Username == CurrentUser;

            // Add user to team
            user.transform.SetParent(team.UsersLayoutGroup.transform, false);
            user.transform.SetAsLastSibling();

            // Set ready status from data
            if (data.Username == CurrentUser)
                IsReady = data.IsReady;

            user.SetReadyStatusVisibility(AreUserReadyStatesVisible());

            // Generate username text
            user.Username.text = GenerateUsernameText(user);

            return user;
        }

        /// <summary>
        /// Returns true if user ready states should be visible
        /// </summary>
        /// <returns></returns>
        protected bool AreUserReadyStatesVisible()
        {
            return JoinedLobby.State == LobbyState.Preparations && JoinedLobby.Data.EnableReadySystem;
        }

        /// <summary>
        /// Generates text for the username. Also, appends
        /// "master" and "current player" tags
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        protected virtual string GenerateUsernameText(LobbyUserUi view)
        {
            var username = view.RawData.Username;

            if (view.IsCurrentPlayer)
            {
                username += CurrentPlayerText;
            }

            if (view.IsMaster)
            {
                username += MasterText;
            }

            return username;
        }

        protected virtual void OnDestroy()
        {
            if (JoinedLobby != null && JoinedLobby.Listener.Equals(this))
            {
                JoinedLobby.SetListener(null);
            }
        }

        #region User input

        /// <summary>
        /// Invoked, when client clicks "Play" button
        /// </summary>
        public virtual void OnPlayClick()
        {
            JoinedLobby.GetLobbyRoomAccess((access, error) =>
            {
                if (access == null)
                {
                    Logs.Error("Failed: " + error);
                    return;
                }

                OnRoomAccessReceived(access);
            });
        }

        public virtual void OnRoomAccessReceived(RoomAccessPacket access)
        {



            // Connect via room connector
            if (RoomConnector.Instance != null)
                RoomConnector.Connect(access);
        }

        public virtual void OnStartGameClick()
        {
            JoinedLobby.StartGame((successful, error) =>
            {
                if (!successful)
                {
                    Logs.Error(error);
                }
            });
        }

        public virtual void OnReadyClick()
        {
            JoinedLobby.SetReadyStatus(!IsReady);
        }

        public virtual void OnLeaveClick()
        {
            JoinedLobby.Leave();
        }

        #endregion

        #region Update methods

        /// <summary>
        /// Enables / disables "join team" buttons, according to the state of the lobby
        /// </summary>
        protected virtual void UpdateTeamJoinButtons()
        {
            var currentPlayer = JoinedLobby.Data.Players[CurrentUser];

            foreach (var team in Teams)
            {
                // Disable join team button if team swtiching is not allowed
                team.Value.JoinButton.gameObject.SetActive(JoinedLobby.Data.EnableTeamSwitching);

                // Disable join team button if we're already in this team
                if (team.Key == currentPlayer.Team)
                {
                    team.Value.JoinButton.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Enables / disables ready button, according to the
        /// state of the lobby
        /// </summary>
        protected virtual void UpdateReadyButton()
        {
            var user = Users[CurrentUser];

            // Hide / show ready button if the ready system is enabled
            ReadyButton.gameObject.SetActive(JoinedLobby.Data.EnableReadySystem 
                && JoinedLobby.State == LobbyState.Preparations);

            ReadyButtonTick.gameObject.SetActive(user.IsReady);
        }


        /// <summary>
        /// Enables / disables "start game" button, according to the
        /// state of the lobby
        /// </summary>
        protected virtual void UpdateStartGameButton()
        {
            var isCurrentPlayerMaster = Users[CurrentUser].IsMaster;
            var isManualStart = JoinedLobby.Data.EnableManualStart;

            var canGameBeStarted = JoinedLobby.State == LobbyState.Preparations;

            // Show / hide the button
            StartButton.gameObject.SetActive(isCurrentPlayerMaster && isManualStart && canGameBeStarted);
        }


        /// <summary>
        /// Enables / disables "play" button, according to the
        /// state of the lobby
        /// </summary>
        protected virtual void UpdatePlayButton()
        {
            PlayButton.gameObject.SetActive(JoinedLobby.State == LobbyState.GameInProgress);
        }


        /// <summary>
        /// Enables / disables lobby property controls, 
        /// according to the state of the lobby
        /// </summary>
        protected virtual void UpdateControls()
        {
            var controlsView = GetComponentInChildren<LobbyPropControllersUi>();

            if (controlsView != null)
            {
                controlsView.SetAllowEditing(CurrentUser == JoinedLobby.Data.GameMaster 
                    && JoinedLobby.State == LobbyState.Preparations);
            }
        }

        /// <summary>
        /// Updates the player count text
        /// </summary>
        protected virtual void UpdatePlayerCount()
        {
            PlayerCount.text = string.Format("{0} Players ({1} Max)", Users.Count, JoinedLobby.Data.MaxPlayers);
        }

        /// <summary>
        /// Updates player readyness states
        /// </summary>
        protected virtual void UpdatePlayerStates()
        {
            var showReadyState = AreUserReadyStatesVisible();

            foreach (var userView in Users.Values)
            {
                userView.SetReadyStatusVisibility(showReadyState);
            }
        }

        #endregion


        #region ILobbyListener

        public virtual void Initialize(JoinedLobby lobby)
        {
            JoinedLobby = lobby;

            gameObject.SetActive(true);

            Setup(lobby);
        }

        public virtual void OnMemberPropertyChanged(LobbyMemberData member, string property, string value)
        {
        }

        public virtual void OnMemberJoined(LobbyMemberData member)
        {
            Users.Add(member.Username, CreateMemberView(member));

            UpdateStartGameButton();
            UpdatePlayerCount();
        }

        public virtual void OnMemberLeft(LobbyMemberData member)
        {
            LobbyUserUi user;
            Users.TryGetValue(member.Username, out user);

            if (user == null)
                return;

            Users.Remove(member.Username);

            UsersPool.Store(user);

            UpdateStartGameButton();
            UpdatePlayerCount();
        }

        public virtual void OnLobbyLeft()
        {
            gameObject.SetActive(false);
        }

        public virtual void OnChatMessageReceived(LobbyChatPacket packet)
        {
            Chat.OnMessageReceived(packet);
        }

        public virtual void OnLobbyPropertyChanged(string property, string value)
        {
            PropControllers.OnPropertyChange(property, value);
        }

        public virtual void OnMasterChanged(string masterUsername)
        {
            foreach (var user in Users.Values)
            {
                user.SetReadyStatusVisibility(JoinedLobby.Data.EnableReadySystem);
                user.IsMaster = user.RawData.Username == masterUsername;

                user.Username.text = GenerateUsernameText(user);
            }

            UpdateStartGameButton();
            UpdateControls();
        }

        public virtual void OnMemberReadyStatusChanged(LobbyMemberData member, bool isReady)
        {
            LobbyUserUi user;
            Users.TryGetValue(member.Username, out user);

            if (user == null)
                return;

            user.SetReady(isReady);

            if (member.Username == CurrentUser)
            {
                IsReady = isReady;
                UpdateReadyButton();
            }

            UpdateStartGameButton();
        }

        public virtual void OnMemberTeamChanged(LobbyMemberData member, LobbyTeamData team)
        {
            LobbyUserUi user;
            Users.TryGetValue(member.Username, out user);

            if (user == null)
                return;

            // Player changed teams
            var newTeam = Teams[team.Name];
            user.transform.SetParent(newTeam.UsersLayoutGroup.transform, false);

            if (member.Username == CurrentUser)
                UpdateReadyButton();

            UpdateTeamJoinButtons();
            UpdateStartGameButton();
        }

        public virtual void OnLobbyStatusTextChanged(string statusText)
        {
            GameStatus.text = statusText;
        }

        public virtual void OnLobbyStateChange(LobbyState state)
        {
            UpdateStartGameButton();
            UpdateReadyButton();
            UpdatePlayButton();
            UpdateControls();
            UpdatePlayerStates();

            // Emulate clicking a play button
            if (state == LobbyState.GameInProgress
                && AutoJoinGameWhenReady
                && !_wasGameRunningWhenOpened)
            {
                OnPlayClick();
            }
        }
        #endregion
    }
}
