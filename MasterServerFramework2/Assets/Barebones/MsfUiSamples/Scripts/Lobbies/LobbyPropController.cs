using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Represents a single controller
    /// </summary>
    public class LobbyPropController : MonoBehaviour
    {
        public Text Label;

        public Dropdown Dropdown;

        /// <summary>
        /// Raw data, used to setup this view
        /// </summary>
        public LobbyPropertyData RawData;

        /// <summary>
        /// Last value, saved to check if changes were made
        /// </summary>
        protected string LastValue;

        protected LobbyUi Lobby;

        /// <summary>
        /// Uses property data to setup the controller view
        /// </summary>
        /// <param name="propertyData"></param>
        public virtual void Setup(LobbyPropertyData propertyData)
        {
            Lobby = Lobby ?? GetComponentInParent<LobbyUi>();

            RawData = propertyData;
            Label.text = propertyData.Label;

            Dropdown.ClearOptions();

            Dropdown.AddOptions(propertyData.Options);
        }

        /// <summary>
        /// Updates a value of the controller to the given one
        /// </summary>
        /// <param name="value"></param>
        public virtual void UpdateValue(string value)
        {
            var index = RawData.Options.FindIndex(o => o == value);

            LastValue = value;

            Dropdown.value = Mathf.Abs(index < 0 ? 0 : index);
            Dropdown.RefreshShownValue();
        }

        /// <summary>
        /// Enables / disables interactions with the controller
        /// </summary>
        /// <param name="isAllowed"></param>
        public virtual void AllowEditing(bool isAllowed)
        {
            Dropdown.interactable = isAllowed;
        }

        /// <summary>
        /// Restores the value which was last set
        /// </summary>
        public virtual void RestoreLastValue()
        {
            UpdateValue(LastValue);
        }

        /// <summary>
        /// Invoked, when user changes the value of this controller
        /// </summary>
        public virtual void OnValueChanged()
        {
            var currentValue = Dropdown.captionText.text;

            // Ignore if this value was change by an update, 
            // and not by the user
            if (LastValue == currentValue)
                return;

            var loadingPromise = Msf.Events.FireWithPromise(Msf.EventNames.ShowLoading, "Updating");

            Lobby.JoinedLobby.SetLobbyProperty(RawData.PropertyKey, currentValue, (successful, error) =>
            {
                loadingPromise.Finish();

                if (!successful)
                {
                    Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                        DialogBoxData.CreateInfo(error));
                    Logs.Error(error);
                    RestoreLastValue();
                }
            });
        }
    }
}