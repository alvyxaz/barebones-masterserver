using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Barebones.Networking;
using UnityEngine;

namespace Barebones.MasterServer
{
    public class BaseLobbyAuto : BaseLobby
    {
        public float WaitSecondsAfterMinPlayersReached = 10;
        public float WaitSecondsAfterFullTeams = 5;

        public BaseLobbyAuto(int lobbyId, IEnumerable<LobbyTeam> teams, LobbiesModule module, LobbyConfig config) : base(lobbyId, teams, module, config)
        {
            config.EnableManualStart = true;
            config.PlayAgainEnabled = false;
            config.EnableGameMasters = false;
        }

        public void StartAutomation()
        {
            BTimer.Instance.StartCoroutine(StartTimer());
        }

        protected IEnumerator StartTimer()
        {
            float timeToWait = WaitSecondsAfterMinPlayersReached;

            var initialState = State;

            while (State == LobbyState.Preparations || State == initialState)
            {
                yield return new WaitForSeconds(1f);

                if (IsDestroyed)
                    break;

                // Check if enough players in the room
                if (MinPlayers > Members.Count)
                {
                    timeToWait = WaitSecondsAfterMinPlayersReached;
                    StatusText = "Waiting for players: " + (MinPlayers - Members.Count) + " more";
                    continue;
                }

                // Check if there are teams that don't
                // meet the minimal requirement
                var lackingTeam = Teams.Values.FirstOrDefault(t => t.MinPlayers > t.PlayerCount);

                if (lackingTeam != null)
                {
                    timeToWait = WaitSecondsAfterMinPlayersReached;
                    StatusText = string.Format("Not enough players in team '{0}'", lackingTeam.Name);
                    continue;
                }

                // Reduce the time to wait by one second
                timeToWait -= 1;

                // Check if teams are full
                if (Teams.Values.All(t => t.MaxPlayers == t.PlayerCount))
                {
                    // Change the timer only if it's lower than current timer
                    timeToWait = timeToWait > WaitSecondsAfterFullTeams
                        ? timeToWait : WaitSecondsAfterFullTeams;
                }

                StatusText = "Starting game in " + timeToWait;

                if (timeToWait <= 0)
                {
                    StartGame();
                    break;
                }
            }
        }
    }
}