using System.Collections;
using System.Collections.Generic;
using System.IO;
using Barebones.Networking;
using UnityEngine;

namespace Barebones.MasterServer
{
    public class MsfProfilesServer : MsfBaseClient
    {
        /// <summary>
        /// Time, after which game server will try sending profile 
        /// updates to master server
        /// </summary>
        public float ProfileUpdatesInterval = 0.1f;

        private Dictionary<string, ObservableServerProfile> _profiles;

        private HashSet<ObservableServerProfile> _modifiedProfiles;

        private Coroutine _sendUpdatesCoroutine;

        public MsfProfilesServer(IClientSocket connection) : base(connection)
        {
            _profiles = new Dictionary<string, ObservableServerProfile>();
            _modifiedProfiles = new HashSet<ObservableServerProfile>();
        }

        /// <summary>
        /// Sends a request to server, retrieves all profile values, and applies them to a provided
        /// profile
        /// </summary>
        public void FillProfileValues(ObservableServerProfile profile, SuccessCallback callback)
        {
            FillProfileValues(profile, callback, Connection);
        }

        /// <summary>
        /// Sends a request to server, retrieves all profile values, and applies them to a provided
        /// profile
        /// </summary>
        public void FillProfileValues(ObservableServerProfile profile, SuccessCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            connection.SendMessage((short) MsfOpCodes.ServerProfileRequest, profile.Username, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                // Use the bytes received, to replicate the profile
                profile.FromBytes(response.AsBytes());

                profile.ClearUpdates();

                _profiles[profile.Username] = profile;

                profile.ModifiedInServer += serverProfile =>
                {
                    OnProfileModified(profile, connection);
                };

                profile.Disposed += OnProfileDisposed;

                callback.Invoke(true, null);
            });
        }

        private void OnProfileModified(ObservableServerProfile profile, IClientSocket connection)
        {
            _modifiedProfiles.Add(profile);

            if (_sendUpdatesCoroutine != null)
                return;

            _sendUpdatesCoroutine = BTimer.Instance.StartCoroutine(KeepSendingUpdates(connection));
        }

        private void OnProfileDisposed(ObservableServerProfile profile)
        {
            profile.Disposed -= OnProfileDisposed;

            _profiles.Remove(profile.Username);
        }

        private IEnumerator KeepSendingUpdates(IClientSocket connection)
        {
            while (true)
            {
                yield return new WaitForSeconds(ProfileUpdatesInterval);

                if (_modifiedProfiles.Count == 0)
                    continue;

                using (var ms = new MemoryStream())
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
                {
                    // Write profiles count
                    writer.Write(_modifiedProfiles.Count);

                    foreach (var profile in _modifiedProfiles)
                    {
                        // Write username
                        writer.Write(profile.Username);

                        var updates = profile.GetUpdates();

                        // Write updates length
                        writer.Write(updates.Length);

                        // Write updates
                        writer.Write(updates);

                        profile.ClearUpdates();
                    }

                    connection.SendMessage((short) MsfOpCodes.UpdateServerProfile, ms.ToArray());
                }

                _modifiedProfiles.Clear();
            }
        }
    }
}