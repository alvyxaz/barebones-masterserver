using UnityEngine;
using System.Collections;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine.UI;

/// <summary>
/// Handles inputs from the email confirmation window
/// </summary>
public class EmailConfirmUi : MonoBehaviour
{
    public Button ResendButton;
    public InputField Code;

    // Use this for initialization
    void Awake () {
	    
	}

    public void OnConfirmClick()
    {
        Msf.Client.Auth.ConfirmEmail(Code.text, (successful, error) =>
        {
            if (!successful)
            {
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateError("Confirmation failed: " + error));
                Logs.Error("Confirmation failed: " + error);
                return;
            }

            Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateInfo("Email confirmed successfully"));

            // Hide the window
            gameObject.SetActive(false);
        });
    }

    public void OnResendClick()
    {
        ResendButton.interactable = false;

        Msf.Client.Auth.RequestEmailConfirmationCode((successful, error) =>
        {
            if (!successful)
            {
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateError("Confirmation code request failed: " + error));

                Logs.Error("Confirmation code request failed: " + error);

                ResendButton.interactable = true;
                return;
            }

            Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateInfo("Confirmation code was sent to your e-mail. " +
                                             "It should arrive within few minutes"));
        });
    }
}
