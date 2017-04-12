namespace Barebones.MasterServer
{
    public enum MsfOpCodes : short
    {
        // Standard error code
        Error = -1,

        MsfStart = 32000,

        // Security
        AesKeyRequest,
        RequestPermissionLevel,
        PeerGuidRequest,

        // Rooms
        RegisterRoom,
        DestroyRoom,
        SaveRoomOptions,
        GetRoomAccess,
        ProvideRoomAccessCheck,
        ValidateRoomAccess,
        PlayerLeftRoom,

        // Spawner
        RegisterSpawner,
        SpawnRequest,
        ClientsSpawnRequest,
        SpawnRequestStatusChange,
        RegisterSpawnedProcess,
        CompleteSpawnProcess,
        KillSpawnedProcess,
        ProcessStarted,
        ProcessKilled,
        AbortSpawnRequest,
        GetSpawnFinalizationData,
        UpdateSpawnerProcessesCount,

        // Matchmaker
        FindGames,

        // Auth
        LogIn,
        RegisterAccount,
        PasswordResetCodeRequest,
        RequestEmailConfirmCode,
        ConfirmEmail,
        GetLoggedInCount,
        PasswordChange,
        GetPeerAccountInfo,

        // Chat
        PickUsername,
        JoinChannel,
        LeaveChannel,
        GetCurrentChannels,
        ChatMessage,
        GetUsersInChannel,
        UserJoinedChannel,
        UserLeftChannel,
        SetDefaultChannel,

        // TODO cleanup
        // Lobbies
        JoinLobby,
        LeaveLobby,
        CreateLobby,
        LobbyInfo,
        SetLobbyProperties,
        SetMyLobbyProperties,
        LobbySetReady,
        LobbyStartGame,
        LobbyChatMessage,
        LobbySendChatMessage,
        JoinLobbyTeam,
        LobbyGameAccessRequest,
        LobbyIsInLobby,
        LobbyMasterChange,
        LobbyStateChange,
        LobbyStatusTextChange,
        LobbyMemberPropertySet,
        LeftLobby,
        LobbyPropertyChanged,
        LobbyMemberJoined,
        LobbyMemberLeft,
        LobbyMemberChangedTeam,
        LobbyMemberReadyStatusChange,
        LobbyMemberPropertyChanged,
        GetLobbyRoomAccess,
        GetLobbyMemberData,
        GetLobbyInfo,

        // Profiles
        ClientProfileRequest,
        ServerProfileRequest,
        UpdateServerProfile,
        UpdateClientProfile,
    }
}