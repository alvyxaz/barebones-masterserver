using Barebones.MasterServer;
using UnityEngine;

namespace Barebones.MasterServer
{
    public class QuickAuthUi : MonoBehaviour
    {
        public GameObject LoginWindow;
        public GameObject RegisterWindow;

        public bool DeactivateOnLogIn = true;

        void Awake()
        {
            LoginWindow = LoginWindow ?? FindObjectOfType<LoginUi>().gameObject;
            RegisterWindow = RegisterWindow ?? FindObjectOfType<RegisterUi>().gameObject;
            Msf.Client.Auth.LoggedIn += OnLoggedIn;

            // In case we're already logged in 
            if (Msf.Client.Auth.IsLoggedIn)
                OnLoggedIn();
        }

        private void OnLoggedIn()
        {
            if (DeactivateOnLogIn)
                gameObject.SetActive(false);
        }

        public void OnLoginClick()
        {
            if (!Msf.Client.Auth.IsLoggedIn)
                LoginWindow.gameObject.SetActive(true);
        }

        public void OnGuestAccessClick()
        {
            var promise = Msf.Events.FireWithPromise(Msf.EventNames.ShowLoading, "Logging in");
            Msf.Client.Auth.LogInAsGuest((accInfo, error) =>
            {
                promise.Finish();

                if (accInfo == null)
                {
                    Msf.Events.Fire(Msf.EventNames.ShowDialogBox, DialogBoxData.CreateError(error));
                    Logs.Error(error);
                }
            });
        }

        public void OnRegisterClick()
        {
            if (!Msf.Client.Auth.IsLoggedIn)
                RegisterWindow.SetActive(true);
        }
    }
}
