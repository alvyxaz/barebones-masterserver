using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    /// <summary>
    ///     Game creation window
    /// </summary>
    public class CreateGameUi : MonoBehaviour
    {
        public Dropdown Map;

        public Image MapImage;

        public List<MapSelection> Maps;
        public int MaxNameLength = 14;
        public Dropdown MaxPlayers;

        public int MaxPlayersLowerLimit = 2;
        public int MaxPlayersUpperLimit = 10;

        public int MinNameLength = 3;

        public CreateGameProgressUi ProgressUi;
        public InputField RoomName;

        public bool RequireAuthentication = true;

        public bool SetAsLastSiblingOnEnable = true;

        protected virtual void Awake()
        {
            ProgressUi = ProgressUi ?? FindObjectOfType<CreateGameProgressUi>();
            Map.ClearOptions();
            Map.AddOptions(Maps.Select(m => new Dropdown.OptionData(m.Name)).ToList());

            OnMapChange();
        }

        public void OnEnable()
        {
            if (SetAsLastSiblingOnEnable)
                transform.SetAsLastSibling();
        }

        public void OnCreateClick()
        {
            if (ProgressUi == null)
            {
                Logs.Error("You need to set a ProgressUi");
                return;
            }

            if (RequireAuthentication && !Msf.Client.Auth.IsLoggedIn)
            {
                ShowError("You must be logged in to create a room");
                return;
            }

            var name = RoomName.text.Trim();

            if (string.IsNullOrEmpty(name) || (name.Length < MinNameLength) || (name.Length > MaxNameLength))
            {
                ShowError(string.Format("Invalid length of game name, shoul be between {0} and {1}", MinNameLength,
                    MaxNameLength));
                return;
            }

            var maxPlayers = 0;
            int.TryParse(MaxPlayers.captionText.text, out maxPlayers);

            if ((maxPlayers < MaxPlayersLowerLimit) || (maxPlayers > MaxPlayersUpperLimit))
            {
                ShowError(string.Format("Invalid number of max players. Value should be between {0} and {1}",
                    MaxPlayersLowerLimit, MaxPlayersUpperLimit));
                return;
            }

            var settings = new Dictionary<string, string>
            {
                {MsfDictKeys.MaxPlayers, maxPlayers.ToString()},
                {MsfDictKeys.RoomName, name},
                {MsfDictKeys.MapName, GetSelectedMap().Name},
                {MsfDictKeys.SceneName, GetSelectedMap().Scene.SceneName}
            };

            Msf.Client.Spawners.RequestSpawn(settings, "", (requestController, errorMsg) =>
            {
                if (requestController == null)
                {
                    ProgressUi.gameObject.SetActive(false);
                    Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                        DialogBoxData.CreateError("Failed to create a game: " + errorMsg));

                    Logs.Error("Failed to create a game: " + errorMsg);
                }

                ProgressUi.Display(requestController);
            });
        }

        public MapSelection GetSelectedMap()
        {
            var text = Map.captionText.text;
            return Maps.FirstOrDefault(m => m.Name == text);
        }

        public void OnMapChange()
        {
            var selected = GetSelectedMap();

            if (selected == null)
            {
                Logs.Error("Invalid map selection");
                return;
            }

            MapImage.sprite = selected.Sprite;
        }

        private void ShowError(string error)
        {
            Msf.Events.Fire(Msf.EventNames.ShowDialogBox, DialogBoxData.CreateError(error));
        }

        [Serializable]
        public class MapSelection
        {
            public string Name;
            public SceneField Scene;
            public Sprite Sprite;
        }
    }
}