using System.Collections.Generic;
using Barebones.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Represents a list of game lobby property controllers
    /// </summary>
    public class LobbyPropControllersUi : MonoBehaviour
    {
        // If true, editig values should be allowed
        protected bool AllowEditing { get; set; }

        public LobbyPropController PropPrefab;
        public LayoutGroup PropertiesGroup;

        /// <summary>
        /// A list of properties
        /// </summary>
        protected GenericUIList<LobbyPropertyData> Properties;

        public LobbyUi Lobby;

        protected virtual void Awake()
        {
            Properties = new GenericUIList<LobbyPropertyData>(PropPrefab.gameObject, PropertiesGroup);
            Lobby = Lobby ?? GetComponentInParent<LobbyUi>();
        }

        /// <summary>
        /// Uses the data to generate property controllers
        /// </summary>
        /// <param name="propData"></param>
        public void Setup(List<LobbyPropertyData> propData)
        {
            Properties.Generate(propData, (data, o) =>
            {
                var view = o.GetComponent<LobbyPropController>();
                view.Setup(data);
                view.AllowEditing(AllowEditing);

                view.UpdateValue(Lobby.JoinedLobby.Data.LobbyProperties[data.PropertyKey]);
            });
        }

        /// <summary>
        /// Invoked, when one of the properties changes in the server.
        /// (this is usually called on a client)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public virtual void OnPropertyChange(string key, string value)
        {
            var propView = Properties.FindObject<LobbyPropController>(view => view.RawData.PropertyKey == key);

            if (propView == null)
                return;

            propView.UpdateValue(value);
        }

        /// <summary>
        /// Enables / disables editing properties
        /// </summary>
        /// <param name="allowEditing"></param>
        public void SetAllowEditing(bool allowEditing)
        {
            AllowEditing = allowEditing;
            OnAllowEditingChange(allowEditing);
        }

        /// <summary>
        /// Invoked, when values editing is enabled / disabled
        /// </summary>
        /// <param name="allowEditing"></param>
        protected virtual void OnAllowEditingChange(bool allowEditing)
        {
            Properties.Iterate<LobbyPropController>(c => c.AllowEditing(allowEditing));
        }
    }
}