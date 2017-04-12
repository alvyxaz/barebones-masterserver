using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class MsfProfilesClient : MsfBaseClient
    {
        public MsfProfilesClient(IClientSocket connection) : base(connection)
        {
        }

        /// <summary>
        /// Sends a request to server, retrieves all profile values, and applies them to a provided
        /// profile
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="callback"></param>
        public void GetProfileValues(ObservableProfile profile, SuccessCallback callback)
        {
            GetProfileValues(profile, callback, Connection);
        }

        /// <summary>
        /// Sends a request to server, retrieves all profile values, and applies them to a provided
        /// profile
        /// </summary>
        public void GetProfileValues(ObservableProfile profile, SuccessCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            connection.SendMessage((short) MsfOpCodes.ClientProfileRequest, profile.PropertyCount, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                // Use the bytes received, to replicate the profile
                profile.FromBytes(response.AsBytes());

                // Listen to profile updates, and apply them
                connection.SetHandler((short) MsfOpCodes.UpdateClientProfile, message =>
                {
                    profile.ApplyUpdates(message.AsBytes());
                });

                callback.Invoke(true, null);
            });
        }
    }
}