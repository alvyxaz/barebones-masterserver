using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Barebones.Networking;
using UnityEngine;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Authentication module, which handles logging in and registration of accounts
    /// </summary>
    public class AuthModule : ServerModuleBehaviour
    {
        public delegate void AuthEventHandler(IUserExtension account);

        [Tooltip("If true, players will be able to log in as guests")]
        public bool EnableGuestLogin = true;

        [Tooltip("Guest names will start with this prefix")]
        public string GuestPrefix = "Guest-";
        protected int NextGuestId;

        [Tooltip("Minimal permission level, required to retrieve peer account information")]
        public int GetPeerDataPermissionsLevel = 0;

        /// <summary>
        /// Collection of users who are currently logged in
        /// </summary>
        public Dictionary<string, IUserExtension> LoggedInUsers;

        /// <summary>
        /// Invoked, when user logs in
        /// </summary>
        public event AuthEventHandler LoggedIn;

        /// <summary>
        /// Invoked, when user logs out
        /// </summary>
        public event AuthEventHandler LoggedOut;

        /// <summary>
        /// Invoked, when user successfully registers an account
        /// </summary>
        public event Action<IPeer, IAccountData> Registered;

        /// <summary>
        /// Invoked, when user successfully confirms his e-mail
        /// </summary>
        public event Action<IAccountData> EmailConfirmed;

        public AuthModuleConfig Config;

        [Header("Mail settings")]
        public Mailer Mailer;

        [TextArea(3, 10)]
        public string ActivationForm = "<h1>Activation</h1>" +
                                       "<p>Your email activation code is: <b>{0}</b> </p>";

        [TextArea(3, 10)]
        public string PasswordResetCode = "<h1>Password Reset Code</h1>" +
                                       "<p>Your password reset code is: <b>{0}</b> </p>";

        public override void Initialize(IServer server)
        {
            base.Initialize(server);

            Mailer = Mailer ?? FindObjectOfType<Mailer>();

            LoggedInUsers = new Dictionary<string, IUserExtension>();

            // Set handlers
            server.SetHandler((short)MsfOpCodes.LogIn, HandleLogIn);
            server.SetHandler((short)MsfOpCodes.RegisterAccount, HandleRegister);
            server.SetHandler((short)MsfOpCodes.PasswordResetCodeRequest, HandlePasswordResetRequest);
            server.SetHandler((short)MsfOpCodes.RequestEmailConfirmCode, HandleRequestEmailConfirmCode);
            server.SetHandler((short)MsfOpCodes.ConfirmEmail, HandleEmailConfirmation);
            server.SetHandler((short)MsfOpCodes.GetLoggedInCount, HandleGetLoggedInCount);
            server.SetHandler((short)MsfOpCodes.PasswordChange, HandlePasswordChange);

            server.SetHandler((short)MsfOpCodes.GetPeerAccountInfo, HandleGetPeerAccountInfo);
        }

        public string GenerateGuestUsername()
        {
            return GuestPrefix + NextGuestId++;
        }

        public virtual IUserExtension CreateUserExtension(IPeer peer)
        {
            return new UserExtension(peer);
        }

        protected void FinalizeLogin(IUserExtension extension)
        {
            extension.Peer.Disconnected += OnUserDisconnect;

            // Add to lookup of logged in users
            LoggedInUsers.Add(extension.Username.ToLower(), extension);

            // Trigger the login event
            if (LoggedIn != null)
                LoggedIn.Invoke(extension);
        }

        private void OnUserDisconnect(IPeer peer)
        {
            var extension = peer.GetExtension<IUserExtension>();

            if (extension == null)
                return;

            LoggedInUsers.Remove(extension.Username.ToLower());

            peer.Disconnected -= OnUserDisconnect;

            if (LoggedOut != null)
                LoggedOut.Invoke(extension);
        }

        public IUserExtension GetLoggedInUser(string username)
        {
            IUserExtension extension;
            LoggedInUsers.TryGetValue(username.ToLower(), out extension);
            return extension;
        }

        protected virtual bool IsUsernameValid(string username)
        {
            return !string.IsNullOrEmpty(username) && // If username is empty
                   username == username.Replace(" ", ""); // If username contains spaces
        }

        protected virtual bool ValidateEmail(string email)
        {
            return !string.IsNullOrEmpty(email)
                   && email.Contains("@")
                   && email.Contains(".");
        }

        public bool HasGetPeerInfoPermissions(IPeer peer)
        {
            var extension = peer.GetExtension<PeerSecurityExtension>();
            return extension.PermissionLevel >= GetPeerDataPermissionsLevel;
        }

        public bool IsUserLoggedIn(string username)
        {
            return LoggedInUsers.ContainsKey(username);
        }

        #region Message Handlers

        /// <summary>
        /// Handles client's request to change password
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandlePasswordChange(IIncommingMessage message)
        {
            var data = new Dictionary<string, string>().FromBytes(message.AsBytes());

            if (!data.ContainsKey("code") || !data.ContainsKey("password") || !data.ContainsKey("email"))
            {
                message.Respond("Invalid request", ResponseStatus.Unauthorized);
                return;
            }

            var db = Msf.Server.DbAccessors.GetAccessor<IAuthDatabase>();

            var resetData = db.GetPasswordResetData(data["email"]);

            if (resetData == null || resetData.Code == null || resetData.Code != data["code"])
            {
                message.Respond("Invalid code provided", ResponseStatus.Unauthorized);
                return;
            }

            var account = db.GetAccountByEmail(data["email"]);

            // Delete (overwrite) code used
            db.SavePasswordResetCode(account, null);

            account.Password = Msf.Security.CreateHash(data["password"]);
            db.UpdateAccount(account);

            message.Respond(ResponseStatus.Success);
        }

        /// <summary>
        /// Handles a request to retrieve a number of logged in users
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleGetLoggedInCount(IIncommingMessage message)
        {
            message.Respond(LoggedInUsers.Count, ResponseStatus.Success);
        }

        /// <summary>
        /// Handles e-mail confirmation request
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleEmailConfirmation(IIncommingMessage message)
        {
            var code = message.AsString();

            var extension = message.Peer.GetExtension<IUserExtension>();

            if (extension == null || extension.AccountData == null)
            {
                message.Respond("Invalid session", ResponseStatus.Unauthorized);
                return;
            }

            if (extension.AccountData.IsGuest)
            {
                message.Respond("Guests cannot confirm e-mails", ResponseStatus.Unauthorized);
                return;
            }

            if (extension.AccountData.IsEmailConfirmed)
            {
                // We still need to respond with "success" in case
                // response is handled somehow on the client
                message.Respond("Your email is already confirmed",
                    ResponseStatus.Success);
                return;
            }

            var db = Msf.Server.DbAccessors.GetAccessor<IAuthDatabase>();

            var requiredCode = db.GetEmailConfirmationCode(extension.AccountData.Email);

            if (requiredCode != code)
            {
                message.Respond("Invalid activation code", ResponseStatus.Error);
                return;
            }

            // Confirm e-mail
            extension.AccountData.IsEmailConfirmed = true;

            // Update account
            db.UpdateAccount(extension.AccountData);

            // Respond with success
            message.Respond(ResponseStatus.Success);

            // Invoke the event
            if (EmailConfirmed != null)
                EmailConfirmed.Invoke(extension.AccountData);
        }

        /// <summary>
        /// Handles password reset request
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandlePasswordResetRequest(IIncommingMessage message)
        {
            var email = message.AsString();
            var db = Msf.Server.DbAccessors.GetAccessor<IAuthDatabase>();

            var account = db.GetAccountByEmail(email);

            if (account == null)
            {
                message.Respond("No such e-mail in the system", ResponseStatus.Unauthorized);
                return;
            }

            var code = Msf.Helper.CreateRandomString(4);

            db.SavePasswordResetCode(account, code);

            if (!Mailer.SendMail(account.Email, "Password Reset Code", string.Format(PasswordResetCode, code)))
            {
                message.Respond("Couldn't send an activation code to your e-mail");
                return;
            }

            message.Respond(ResponseStatus.Success);
        }

        protected virtual void HandleRequestEmailConfirmCode(IIncommingMessage message)
        {
            var extension = message.Peer.GetExtension<IUserExtension>();

            if (extension == null || extension.AccountData == null)
            {
                message.Respond("Invalid session", ResponseStatus.Unauthorized);
                return;
            }

            if (extension.AccountData.IsGuest)
            {
                message.Respond("Guests cannot confirm e-mails", ResponseStatus.Unauthorized);
                return;
            }

            var code = Msf.Helper.CreateRandomString(6);

            var db = Msf.Server.DbAccessors.GetAccessor<IAuthDatabase>();

            // Save the new code
            Debug.LogError("SHOULD BE HERE");
            db.SaveEmailConfirmationCode(extension.AccountData.Email, code);

            if (!Mailer.SendMail(extension.AccountData.Email, "E-mail confirmation", string.Format(ActivationForm, code)))
            {
                message.Respond("Couldn't send a confirmation code to your e-mail. Please contact support");
                return;
            }

            // Respond with success
            message.Respond(ResponseStatus.Success);
        }

        /// <summary>
        /// Handles account registration request
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleRegister(IIncommingMessage message)
        {
            var encryptedData = message.AsBytes();

            var securityExt = message.Peer.GetExtension<PeerSecurityExtension>();
            var aesKey = securityExt.AesKey;

            if (aesKey == null)
            {
                // There's no aesKey that client and master agreed upon
                message.Respond("Insecure request".ToBytes(), ResponseStatus.Unauthorized);
                return;
            }

            var decrypted = Msf.Security.DecryptAES(encryptedData, aesKey);
            var data = new Dictionary<string, string>().FromBytes(decrypted);

            if (!data.ContainsKey("username") || !data.ContainsKey("password") || !data.ContainsKey("email"))
            {
                message.Respond("Invalid registration request".ToBytes(), ResponseStatus.Error);
                return;
            }

            var username = data["username"];
            var password = data["password"];
            var email = data["email"].ToLower();

            var usernameLower = username.ToLower();

            var extension = message.Peer.GetExtension<IUserExtension>();

            if (extension != null && !extension.AccountData.IsGuest)
            {
                // Fail, if user is already logged in, and not with a guest account
                message.Respond("Invalid registration request".ToBytes(), ResponseStatus.Error);
                return;
            }

            if (!IsUsernameValid(usernameLower))
            {
                message.Respond("Invalid Username".ToBytes(), ResponseStatus.Error);
                return;
            }

            if (Config.ForbiddenUsernames.Contains(usernameLower))
            {
                // Check if uses forbidden username
                message.Respond("Forbidden word used in username".ToBytes(), ResponseStatus.Error);
                return;
            }

            if (Config.ForbiddenWordsInUsernames.FirstOrDefault(usernameLower.Contains) != null)
            {
                // Check if there's a forbidden word in username
                message.Respond("Forbidden word used in username".ToBytes(), ResponseStatus.Error);
                return;
            }

            if ((username.Length < Config.UsernameMinChars) ||
                (username.Length > Config.UsernameMaxChars))
            {
                // Check if username length is good
                message.Respond("Invalid usernanme length".ToBytes(), ResponseStatus.Error);

                return;
            }

            if (!ValidateEmail(email))
            {
                // Check if email is valid
                message.Respond("Invalid Email".ToBytes(), ResponseStatus.Error);
                return;
            }

            var db = Msf.Server.DbAccessors.GetAccessor<IAuthDatabase>();

            var account = db.CreateAccountObject();

            account.Username = username;
            account.Email = email;
            account.Password = Msf.Security.CreateHash(password);

            try
            {
                db.InsertNewAccount(account);

                if (Registered != null)
                    Registered.Invoke(message.Peer, account);

                message.Respond(ResponseStatus.Success);
            }
            catch (Exception e)
            {
                Logs.Error(e);
                message.Respond("Username or E-mail is already registered".ToBytes(), ResponseStatus.Error);
            }
        }

        /// <summary>
        /// Handles a request to retrieve account information
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleGetPeerAccountInfo(IIncommingMessage message)
        {
            if (!HasGetPeerInfoPermissions(message.Peer))
            {
                message.Respond("Unauthorized", ResponseStatus.Unauthorized);
                return;
            }

            var peerId = message.AsInt();

            var peer = Server.GetPeer(peerId);

            if (peer == null)
            {
                message.Respond("Peer with a given ID is not in the game", ResponseStatus.Error);
                return;
            }

            var account = peer.GetExtension<IUserExtension>();

            if (account == null)
            {
                message.Respond("Peer has not been authenticated", ResponseStatus.Failed);
                return;
            }

            var data = account.AccountData;

            var packet = new PeerAccountInfoPacket()
            {
                PeerId = peerId,
                Properties = data.Properties,
                Username = account.Username
            };

            message.Respond(packet, ResponseStatus.Success);
        }

        /// <summary>
        /// Handles a request to log in
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleLogIn(IIncommingMessage message)
        {
            if (message.Peer.HasExtension<IUserExtension>())
            {
                message.Respond("Already logged in", ResponseStatus.Unauthorized);
                return;
            }

            var encryptedData = message.AsBytes();
            var securityExt = message.Peer.GetExtension<PeerSecurityExtension>();
            var aesKey = securityExt.AesKey;

            if (aesKey == null)
            {
                // There's no aesKey that client and master agreed upon
                message.Respond("Insecure request".ToBytes(), ResponseStatus.Unauthorized);
                return;
            }

            var decrypted = Msf.Security.DecryptAES(encryptedData, aesKey);
            var data = new Dictionary<string, string>().FromBytes(decrypted);

            var db = Msf.Server.DbAccessors.GetAccessor<IAuthDatabase>();

            IAccountData accountData = null;

            // ---------------------------------------------
            // Guest Authentication
            if (data.ContainsKey("guest") && EnableGuestLogin)
            {
                var guestUsername = GenerateGuestUsername();
                accountData = db.CreateAccountObject();

                accountData.Username = guestUsername;
                accountData.IsGuest = true;
                accountData.IsAdmin = false;
            }

            // ----------------------------------------------
            // Token Authentication
            if (data.ContainsKey("token") && accountData == null)
            {
                accountData = db.GetAccountByToken(data["token"]);
                if (accountData == null)
                {
                    message.Respond("Invalid Credentials".ToBytes(), ResponseStatus.Unauthorized);
                    return;
                }

                var otherSession = GetLoggedInUser(accountData.Username);
                if (otherSession != null)
                {
                    otherSession.Peer.Disconnect("Other user logged in");
                    message.Respond("This account is already logged in".ToBytes(),
                        ResponseStatus.Unauthorized);
                    return;
                }
            }

            // ----------------------------------------------
            // Username / Password authentication

            if (data.ContainsKey("username") && data.ContainsKey("password") && accountData == null)
            {
                var username = data["username"];
                var password = data["password"];

                accountData = db.GetAccount(username);

                if (accountData == null)
                {
                    // Couldn't find an account with this name
                    message.Respond("Invalid Credentials".ToBytes(), ResponseStatus.Unauthorized);
                    return;
                }

                if (!Msf.Security.ValidatePassword(password, accountData.Password))
                {
                    // Password is not correct
                    message.Respond("Invalid Credentials".ToBytes(), ResponseStatus.Unauthorized);
                    return;
                }
            }

            if (accountData == null)
            {
                message.Respond("Invalid request", ResponseStatus.Unauthorized);
                return;
            }

            // Setup auth extension
            var extension = message.Peer.AddExtension(CreateUserExtension(message.Peer));
            extension.Load(accountData);
            var infoPacket = extension.CreateInfoPacket();

            // Finalize login
            FinalizeLogin(extension);

            message.Respond(infoPacket.ToBytes(), ResponseStatus.Success);
            return;
        }

        #endregion

        [Serializable]
        public class AuthModuleConfig
        {
            public List<string> ForbiddenUsernames = new List<string>();
            public List<string> ForbiddenWordsInUsernames = new List<string>();
            public int UsernameMaxChars = 12;
            public int UsernameMinChars = 3;
        }
    }
}