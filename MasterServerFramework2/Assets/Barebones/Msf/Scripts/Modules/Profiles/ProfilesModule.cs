using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Barebones.Logging;
using Barebones.Networking;

using UnityEngine;

namespace Barebones.MasterServer
{
    public delegate ObservableServerProfile ProfileFactory(string username, IPeer clientPeer);

    /// <summary>
    /// Handles player profiles within master server.
    /// Listens to changes in player profiles, and sends updates to
    /// clients of interest.
    /// Also, reads changes from game server, and applies them to players profile
    /// </summary>
    public class ProfilesModule : ServerModuleBehaviour
    {
        /// <summary>
        /// Time to pass after logging out, until profile
        /// will be removed from the lookup. Should be enough for game
        /// server to submit last changes
        /// </summary>
        public float UnloadProfileAfter = 20f;

        /// <summary>
        /// Interval, in which updated profiles will be saved to database
        /// </summary>
        public float SaveProfileInterval = 1f;

        /// <summary>
        /// Interval, in which profile updates will be sent to clients
        /// </summary>
        public float ClientUpdateInterval = 0f;

        public int EditProfilePermissionLevel = 0;

        public IProfilesDatabase Database;

        private Dictionary<string, ObservableServerProfile> _profiles;

        private AuthModule _auth;

        private HashSet<string> _debouncedSaves;
        private HashSet<string> _debouncedClientUpdates;

        protected BmLogger Logger = LogManager.GetLogger(typeof(ProfilesModule).Name);

        public bool IgnoreProfileMissmatchError = false;

        /// <summary>
        /// By default, profiles module will use this factory to create a profile for users.
        /// If you're using profiles, you will need to change this factory to construct the
        /// structure of a profile.
        /// </summary>
        public ProfileFactory ProfileFactory { get; set; }

        void Awake()
        {
            if (DestroyIfExists()) return;

            AddOptionalDependency<AuthModule>();

            _profiles = new Dictionary<string, ObservableServerProfile>();
            _debouncedSaves = new HashSet<string>();
            _debouncedClientUpdates = new HashSet<string>();
        }

        public override void Initialize(IServer server)
        {
            Database = Msf.Server.DbAccessors.GetAccessor<IProfilesDatabase>();

            if (Database == null)
            {
                Logger.Error("Profiles database implementation was not found");
            }

            server.SetHandler((short)MsfOpCodes.ClientProfileRequest, HandleClientProfileRequest);

            // Auth dependency setup
            _auth = server.GetModule<AuthModule>();
            if (_auth != null)
            {
                _auth.LoggedIn += OnLoggedIn;
            }

            // Games dependency setup
            server.SetHandler((short) MsfOpCodes.ServerProfileRequest, HandleGameServerProfileRequest);
            server.SetHandler((short) MsfOpCodes.UpdateServerProfile, HandleProfileUpdates);
        }

        /// <summary>
        /// Invoked, when user logs into the master server
        /// </summary>
        /// <param name="session"></param>
        /// <param name="accountData"></param>
        private void OnLoggedIn(IUserExtension user)
        {
            user.Peer.Disconnected += OnPeerPlayerDisconnected;

            // Create a profile
            ObservableServerProfile profile;

            if (_profiles.ContainsKey(user.Username))
            {
                // There's a profile from before, which we can use
                profile = _profiles[user.Username];
                profile.ClientPeer = user.Peer;
            }
            else
            {
                // We need to create a new one
                profile = CreateProfile(user.Username, user.Peer);
                _profiles.Add(user.Username, profile);
            }

            // Restore profile data from database (only if not a guest)
            if (!user.AccountData.IsGuest)
                Database.RestoreProfile(profile);

            // Save profile property
            user.Peer.AddExtension(new ProfileExtension(profile, user.Peer));

            // Listen to profile events
            profile.ModifiedInServer += OnProfileChanged;
        }

        /// <summary>
        /// Creates an observable profile for a client.
        /// Override this, if you want to customize the profile creation
        /// </summary>
        /// <param name="username"></param>
        /// <param name="clientPeer"></param>
        /// <returns></returns>
        protected virtual ObservableServerProfile CreateProfile(string username, IPeer clientPeer)
        {
            if (ProfileFactory != null)
            {
                var profile = ProfileFactory(username, clientPeer);
                profile.ClientPeer = clientPeer;
                return profile;
            }

            return new ObservableServerProfile(username)
            {
                ClientPeer = clientPeer
            };
        }

        /// <summary>
        /// Invoked, when profile is changed
        /// </summary>
        /// <param name="profile"></param>
        private void OnProfileChanged(ObservableServerProfile profile)
        {
            // Debouncing is used to reduce a number of updates per interval to one
            // TODO make debounce lookup more efficient than using string hashet

            if (!_debouncedSaves.Contains(profile.Username) && profile.ShouldBeSavedToDatabase)
            {
                // If profile is not already waiting to be saved
                _debouncedSaves.Add(profile.Username);
                StartCoroutine(SaveProfile(profile, SaveProfileInterval));
            }

            if (!_debouncedClientUpdates.Contains(profile.Username))
            {
                // If it's a master server
                _debouncedClientUpdates.Add(profile.Username);
                StartCoroutine(SendUpdatesToClient(profile, ClientUpdateInterval));
            }
        }

        /// <summary>
        /// Invoked, when user logs out (disconnects from master)
        /// </summary>
        /// <param name="session"></param>
        private void OnPeerPlayerDisconnected(IPeer peer)
        {
            peer.Disconnected -= OnPeerPlayerDisconnected;

            var profileExtension = peer.GetExtension<ProfileExtension>();

            if (profileExtension == null)
                return;

            // Unload profile
            StartCoroutine(UnloadProfile(profileExtension.Username, UnloadProfileAfter));
        }

        /// <summary>
        /// Saves a profile into database after delay
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator SaveProfile(ObservableServerProfile profile, float delay)
        {
            // Wait for the delay
            yield return new WaitForSecondsRealtime(delay);

            // Remove value from debounced updates
            _debouncedSaves.Remove(profile.Username);

            Database.UpdateProfile(profile);

            profile.UnsavedProperties.Clear();
        }

        /// <summary>
        /// Collets changes in the profile, and sends them to client after delay
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator SendUpdatesToClient(ObservableServerProfile profile, float delay)
        {
            // Wait for the delay
            if (delay > 0.01f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }
            else
            {
                // Wait one frame, so that we don't send multiple packets
                // in case we update multiple values
                yield return null;
            }

            // Remove value from debounced updates
            _debouncedClientUpdates.Remove(profile.Username);

            if (profile.ClientPeer == null || !profile.ClientPeer.IsConnected)
            {
                // If client is not connected, and we don't need to send him profile updates
                profile.ClearUpdates();
                yield break;
            }

            using (var ms = new MemoryStream())
            {
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
                {
                    profile.GetUpdates(writer);
                    profile.ClearUpdates();
                }

                profile.ClientPeer.SendMessage(MessageHelper.Create((short) MsfOpCodes.UpdateClientProfile, ms.ToArray()),
                    DeliveryMethod.ReliableSequenced);
            }
        }

        /// <summary>
        /// Coroutine, which unloads profile after a period of time
        /// </summary>
        /// <param name="username"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator UnloadProfile(string username, float delay)
        {
            // Wait for the delay
            yield return new WaitForSecondsRealtime(delay);

            // If user is not actually logged in, remove the profile
            if (_auth.IsUserLoggedIn(username))
                yield break;

            ObservableServerProfile profile;
            _profiles.TryGetValue(username, out profile);

            if (profile == null)
                yield break;

            // Remove profile
            _profiles.Remove(username);

            // Remove listeners
            profile.ModifiedInServer -= OnProfileChanged;
        }

        protected virtual bool HasPermissionToEditProfiles(IPeer messagePeer)
        {
            var securityExtension = messagePeer.GetExtension<PeerSecurityExtension>();

            return securityExtension != null
                   && securityExtension.PermissionLevel >= EditProfilePermissionLevel;
        }

        #region Handlers

        /// <summary>
        /// Handles a message from game server, which includes player profiles updates
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleProfileUpdates(IIncommingMessage message)
        {
            if (!HasPermissionToEditProfiles(message.Peer))
            {
                Logs.Error("Master server received an update for a profile, but peer who tried to " +
                           "update it did not have sufficient permissions");
                return;
            }

            var data = message.AsBytes();

            using (var ms = new MemoryStream(data))
            {
                using (var reader = new EndianBinaryReader(EndianBitConverter.Big, ms))
                {
                    // Read profiles count
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; i++)
                    {
                        // Read username
                        var username = reader.ReadString();

                        // Read updates length
                        var updatesLength = reader.ReadInt32();

                        // Read updates
                        var updates = reader.ReadBytes(updatesLength);

                        try
                        {
                            ObservableServerProfile profile;
                            _profiles.TryGetValue(username, out profile);

                            if (profile != null)
                            {
                                profile.ApplyUpdates(updates);
                            }
                        }
                        catch (Exception e)
                        {
                            Logs.Error("Error while trying to handle profile updates from master server");
                            Logs.Error(e);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles a request from client to get profile
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleClientProfileRequest(IIncommingMessage message)
        {
            var clientPropCount = message.AsInt();

            var profileExt = message.Peer.GetExtension<ProfileExtension>();
            if (profileExt == null)
            {
                message.Respond("Profile not found", ResponseStatus.Failed);
                return;
            }

            profileExt.Profile.ClientPeer = message.Peer;

            if (!IgnoreProfileMissmatchError && clientPropCount != profileExt.Profile.PropertyCount)
            {
                Logger.Error(string.Format("Client requested a profile with {0} properties, but server " +
                                           "constructed a profile with {1}. Make sure that you've changed the " +
                                           "profile factory on the ProfilesModule", clientPropCount, profileExt.Profile.PropertyCount));
            }

            message.Respond(profileExt.Profile.ToBytes(), ResponseStatus.Success);
        }

        /// <summary>
        /// Handles a request from game server to get a profile
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleGameServerProfileRequest(IIncommingMessage message)
        {
            if (!HasPermissionToEditProfiles(message.Peer))
            {
                message.Respond("Invalid permission level", ResponseStatus.Unauthorized);
                return;
            }

            var username = message.AsString();

            ObservableServerProfile profile;
            _profiles.TryGetValue(username, out profile);

            if (profile == null)
            {
                message.Respond(ResponseStatus.Failed);
                return;
            }

            message.Respond(profile.ToBytes(), ResponseStatus.Success);
        }

        

        #endregion
    }
}