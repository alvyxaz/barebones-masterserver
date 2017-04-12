using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Containts references to auth ui components, and methods to display them.
    /// </summary>
    public class AuthUiController : MonoBehaviour
    {
        public QuickAuthUi QuickAuthUi;
        public LoginUi LoginWindow;
        public RegisterUi RegisterWindow;
        public PasswordResetUi PasswordResetWindow;
        public EmailConfirmUi EmailConfirmationWindow;

        public List<GameObject> EnableObjectsOnLogIn;
        public List<GameObject> DisableObjectsOnLogout;

        public static AuthUiController Instance;

        protected virtual void Awake()
        {
            Instance = this;
            QuickAuthUi = QuickAuthUi ?? FindObjectOfType<QuickAuthUi>();
            LoginWindow = LoginWindow ?? FindObjectOfType<LoginUi>();
            RegisterWindow = RegisterWindow ?? FindObjectOfType<RegisterUi>();
            PasswordResetWindow = PasswordResetWindow ?? FindObjectOfType<PasswordResetUi>();
            EmailConfirmationWindow = EmailConfirmationWindow ?? FindObjectOfType<EmailConfirmUi>();

            Msf.Client.Auth.LoggedIn += OnLoggedIn;
            Msf.Client.Auth.LoggedOut += OnLoggedOut;

            if (Msf.Client.Auth.IsLoggedIn)
            {
                OnLoggedIn();
            }
        }

        private void OnLoggedIn()
        {
            foreach (var obj in EnableObjectsOnLogIn)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        protected virtual void OnLoggedOut()
        {
            if (QuickAuthUi != null) 
                QuickAuthUi.gameObject.SetActive(true);

            foreach (var obj in DisableObjectsOnLogout)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            Msf.Client.Auth.LoggedOut -= OnLoggedOut;
            Msf.Client.Auth.LoggedIn -= OnLoggedIn;
        }

        /// <summary>
        /// Displays login window
        /// </summary>
        public virtual void ShowLoginWindow()
        {
            LoginWindow.gameObject.SetActive(true);
        }

        /// <summary>
        /// Displays registration window
        /// </summary>
        public virtual void ShowRegisterWindow()
        {
            RegisterWindow.gameObject.SetActive(true);
        }

        /// <summary>
        /// Displays password reset window
        /// </summary>
        public virtual void ShowPasswordResetWindow()
        {
            PasswordResetWindow.gameObject.SetActive(true);
        }

        /// <summary>
        /// Displays e-mail confirmation window
        /// </summary>
        public virtual void ShowEmailConfirmationWindow()
        {
            EmailConfirmationWindow.gameObject.SetActive(true);
        }

        public void LogOut()
        {
            Msf.Client.Auth.LogOut();
        }
    }
}