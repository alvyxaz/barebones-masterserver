using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    public class PasswordResetUi : MonoBehaviour
    {
        public InputField Email;
        public InputField ResetCode;
        public InputField Password;
        public InputField PasswordRepeat;

        public Button SendCodeButton;
        public Button ResetButton;

        // Use this for initialization
        void Start()
        {
            if (SendCodeButton != null)
                SendCodeButton.onClick.AddListener(OnSendCodeClick);

            if (ResetButton != null)
                ResetButton.onClick.AddListener(OnResetClick);
        }

        private void OnEnable()
        {
            gameObject.transform.localPosition = Vector3.zero;
        }

        public void OnSendCodeClick()
        {
            var email = Email.text.ToLower().Trim();

            if (email.Length < 3 || !email.Contains("@"))
            {
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateError("Invalid e-mail address provided"));
                return;
            }

            var promise = Msf.Events.FireWithPromise(Msf.EventNames.ShowLoading, 
                "Requesting reset code");

            Msf.Client.Auth.RequestPasswordReset(email, (successful, error) =>
            {
                promise.Finish();

                if (!successful)
                {
                    Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                        DialogBoxData.CreateError(error));
                    return;
                }

                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateInfo(
                    "Reset code has been sent to the provided e-mail address."));
            });
        }

        public void OnResetClick()
        {
            var email = Email.text.Trim().ToLower();
            var code = ResetCode.text;
            var newPassword = Password.text;

            if (Password.text != PasswordRepeat.text)
            {
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateError("Passwords do not match"));
                return;
            }

            if (newPassword.Length < 3)
            {
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateError("Password is too short"));
                return;
            }

            if (string.IsNullOrEmpty(code))
            {
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateError("Invalid code"));
                return;
            }

            if (email.Length < 3 || !email.Contains("@"))
            {
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateError("Invalid e-mail address provided"));
                return;
            }

            var data = new PasswordChangeData()
            {
                Email = email,
                Code = code,
                NewPassword = newPassword
            };

            var promise = Msf.Events.FireWithPromise(Msf.EventNames.ShowLoading, 
                "Changing a password");

            Msf.Client.Auth.ChangePassword(data, (successful, error) =>
            {
                promise.Finish();

                if (!successful)
                {
                    Msf.Events.Fire(Msf.EventNames.ShowDialogBox, 
                        DialogBoxData.CreateError(error));
                    return;
                }

                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateInfo(
                    "Password changed successfully"));

                Msf.Events.Fire(Msf.EventNames.RestoreLoginForm, new LoginFormData
                {
                    Username = null,
                    Password = ""
                });

                gameObject.SetActive(false);
            });
        }
    }
}
