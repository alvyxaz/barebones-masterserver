namespace Barebones.MasterServer
{
    public interface ILobbyListener
    {
        /// <summary>
        /// Called, when listener is added to joined lobby
        /// </summary>
        /// <param name="lobby"></param>
        void Initialize(JoinedLobby lobby);

        /// <summary>
        /// Called, when one of the lobby members property changes
        /// </summary>
        /// <param name="member"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        void OnMemberPropertyChanged(LobbyMemberData member, string property, string value); //

        /// <summary>
        /// Called, when a new member joins the lobby
        /// </summary>
        /// <param name="member"></param>
        void OnMemberJoined(LobbyMemberData member);

        /// <summary>
        /// Called, when one of the members leaves a lobby
        /// </summary>
        /// <param name="member"></param>
        void OnMemberLeft(LobbyMemberData member);

        /// <summary>
        /// Called, when "you" leave a lobby
        /// </summary>
        void OnLobbyLeft();

        /// <summary>
        /// Called, when chat message is received
        /// </summary>
        /// <param name="packet"></param>
        void OnChatMessageReceived(LobbyChatPacket packet);

        /// <summary>
        /// Called, when one of the lobby properties changes
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        void OnLobbyPropertyChanged(string property, string value);

        /// <summary>
        /// Called, when game master of the lobby changes
        /// </summary>
        /// <param name="masterUsername"></param>
        void OnMasterChanged(string masterUsername);

        /// <summary>
        /// Called, when on the member's ready status changes
        /// </summary>
        /// <param name="member"></param>
        /// <param name="isReady"></param>
        void OnMemberReadyStatusChanged(LobbyMemberData member, bool isReady);

        /// <summary>
        /// Called, when one of the members changes a team
        /// </summary>
        /// <param name="member"></param>
        /// <param name="team"></param>
        void OnMemberTeamChanged(LobbyMemberData member, LobbyTeamData team);

        /// <summary>
        /// Called, when lobby status text changes
        /// </summary>
        /// <param name="statusText"></param>
        void OnLobbyStatusTextChanged(string statusText);

        /// <summary>
        /// Called, when lobby state changes
        /// </summary>
        /// <param name="state"></param>
        void OnLobbyStateChange(LobbyState state);
    }
}