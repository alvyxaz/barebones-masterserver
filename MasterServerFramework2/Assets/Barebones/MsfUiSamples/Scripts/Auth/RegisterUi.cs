using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    /// <summary>
    ///     Represents a basic view of registration form
    /// </summary>
    public class RegisterUi : MonoBehaviour
    {
        public InputField Email;
        public Text ErrorText;
        public Text GuestNotice;
        public InputField Password;

        public Button RegisterButton;
        public InputField RepeatPassword;

        public InputField Username;

        protected void OnAwake()
        {
            Msf.Client.Auth.LoggedIn += OnLoggedIn;

            Email = Email ?? transform.FindChild("Email").GetComponent<InputField>();
            ErrorText = ErrorText ?? transform.FindChild("Error").GetComponent<Text>();
            GuestNotice = GuestNotice ?? transform.FindChild("GuestNotice").GetComponent<Text>();
            RegisterButton = RegisterButton ?? transform.FindChild("Button").GetComponent<Button>();
            Password = Password ?? transform.FindChild("Password").GetComponent<InputField>();
            RepeatPassword = RepeatPassword ?? transform.FindChild("RepeatPassword").GetComponent<InputField>();
            Username = Username ?? transform.FindChild("Username").GetComponent<InputField>();

            ErrorText.gameObject.SetActive(false);
        }

        // TODO make more checks
        public bool ValidateInput()
        {
            var error = "";

            if (Username.text.Length <= 3)
                error += "Username too short\n";

            if (Password.text.Length <= 3)
                error += "Password is too short\n";

            if (!Password.text.Equals(RepeatPassword.text))
                error += "Passwords don't match\n";

            if (Email.text.Length <= 3)
                error += "Email too short\n";

            if (error.Length > 0)
            {
                error = error.Remove(error.Length - 1);
                ShowError(error);
                return false;
            }

            return true;
        }

        private void UpdateGuestNotice()
        {
            var auth = Msf.Client.Auth;
            if (GuestNotice != null && auth != null && auth.AccountInfo != null)
            {
                GuestNotice.gameObject.SetActive(auth.IsLoggedIn && auth.AccountInfo.IsGuest);
            }
        }

        private void OnEnable()
        {
            gameObject.transform.localPosition = Vector3.zero;
            UpdateGuestNotice();
        }

        protected void OnLoggedIn()
        {
            UpdateGuestNotice();
            gameObject.SetActive(false);
        }

        private void ShowError(string message)
        {
            ErrorText.gameObject.SetActive(true);
            ErrorText.text = message;
        }

        public void OnRegisterClick()
        {
            // Disable error
            ErrorText.gameObject.SetActive(false);

            // Ignore if didn't pass validation
            if (!ValidateInput())
                return;

            var data = new Dictionary<string, string>
            {
                {"username", Username.text},
                {"password", Password.text},
                {"email", Email.text}
            };

            var promise = Msf.Events.FireWithPromise(Msf.EventNames.ShowLoading);

            Msf.Client.Auth.Register(data, (successful, message) =>
            {
                promise.Finish();

                if (!successful && (message != null))
                {
                    ShowError(message);
                    return;
                }

                OnSuccess();
            });
        }

        protected void OnSuccess()
        {
            // Hide registration form
            gameObject.SetActive(false);

            // Show the dialog box
            Msf.Events.Fire(Msf.EventNames.ShowDialogBox,  
                DialogBoxData.CreateInfo("Registration successful!"));

            // Show the login forum
            Msf.Events.Fire(Msf.EventNames.RestoreLoginForm, new LoginFormData
            {
                Username = Username.text,
                Password = ""
            });

            if ((Msf.Client.Auth.AccountInfo != null) && Msf.Client.Auth.AccountInfo.IsGuest)
                Msf.Client.Auth.LogOut();
        }

        void OnDestroy()
        {
            Msf.Client.Auth.LoggedIn -= OnLoggedIn;
        }
    }
}