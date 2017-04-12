using System.Collections;
using System.Collections.Generic;
using Barebones.Networking;
using UnityEngine;

namespace Barebones.MasterServer
{
    /// <summary>
    /// This list contains a number of lobby factory methods,
    /// used for demonstration purposes
    /// </summary>
    public class DemoLobbyFactories
    {
        public static string DefaultName = "Untitled Lobby";

        /// <summary>
        /// Creates a game lobby for 1 vs 1 game
        /// </summary>
        /// <param name="module"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static ILobby OneVsOne(LobbiesModule module, Dictionary<string, string> properties, IPeer creator)
        {
            // Create the teams
            var teamA = new LobbyTeam("Team Blue")
            {
                MaxPlayers = 1,
                MinPlayers = 1
            };
            var teamB = new LobbyTeam("Team Red")
            {
                MaxPlayers = 1,
                MinPlayers = 1
            };

            // Set their colors
            teamA.SetProperty("color", "0000FF");
            teamB.SetProperty("color", "FF0000");

            var config = new LobbyConfig();

            // Create the lobby
            var lobby = new BaseLobby(module.GenerateLobbyId(),
                new[] { teamA, teamB }, module, config)
            {
                Name = ExtractLobbyName(properties)
            };

            // Override properties with what user provided
            lobby.SetLobbyProperties(properties);

            // Add control for the game speed
            lobby.AddControl(new LobbyPropertyData()
            {
                Label = "Game Speed",
                Options = new List<string>() { "1x", "2x", "3x" },
                PropertyKey = "speed"
            }, "2x"); // Default option

            // Add control to enable/disable gravity
            lobby.AddControl(new LobbyPropertyData()
            {
                Label = "Gravity",
                Options = new List<string>() { "On", "Off" },
                PropertyKey = "gravity",
            });

            return lobby;
        }

        /// <summary>
        /// Creates a lobby for a deathmatch game with 10 players
        /// </summary>
        /// <param name="module"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static ILobby Deathmatch(LobbiesModule module, Dictionary<string, string> properties, IPeer creator)
        {
            // Create the teams
            var team = new LobbyTeam("")
            {
                MaxPlayers = 10,
                MinPlayers = 1
            };

            var config = new LobbyConfig();

            // Create the lobby
            var lobby = new BaseLobby(module.GenerateLobbyId(),
                new[] { team }, module, config)
            {
                Name = ExtractLobbyName(properties)
            };

            // Override properties with what user provided
            lobby.SetLobbyProperties(properties);

            // Add control for the game speed
            lobby.AddControl(new LobbyPropertyData()
            {
                Label = "Game Speed",
                Options = new List<string>() { "1x", "2x", "3x" },
                PropertyKey = "speed"
            }, "2x"); // Default option

            return lobby;
        }

        /// <summary>
        /// Creates a game for two vs two vs four. This example shows
        /// how you can setup different size teams, and add them different constraints.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static ILobby TwoVsTwoVsFour(LobbiesModule module, Dictionary<string, string> properties, IPeer creator)
        {
            // Create the teams
            var teamA = new LobbyTeam("Team Blue")
            {
                MaxPlayers = 2,
                MinPlayers = 1
            };
            var teamB = new LobbyTeam("Team Red")
            {
                MaxPlayers = 2,
                MinPlayers = 1
            };

            var teamC = new LobbyTeam("N00bs")
            {
                MaxPlayers = 4,
                MinPlayers = 0
            };

            // Set their colors
            teamA.SetProperty("color", "0000FF");
            teamB.SetProperty("color", "FF0000");
            teamC.SetProperty("color", "00FF00");

            var config = new LobbyConfig();

            // Create the lobby
            var lobby = new BaseLobby(module.GenerateLobbyId(),
                new[] { teamA, teamB, teamC }, module, config)
            {
                Name = ExtractLobbyName(properties)
            };

            // Override properties with what user provided
            lobby.SetLobbyProperties(properties);

            // Add control for the game speed
            lobby.AddControl(new LobbyPropertyData()
            {
                Label = "Game Speed",
                Options = new List<string>() { "1x", "2x", "3x" },
                PropertyKey = "speed"
            }, "2x"); // Default option

            // Add control to enable/disable gravity
            lobby.AddControl(new LobbyPropertyData()
            {
                Label = "Gravity",
                Options = new List<string>() { "On", "Off" },
                PropertyKey = "gravity",
            });

            return lobby;
        }

        /// <summary>
        /// Creates a 3 vs 3 lobby, instead of the regular <see cref="GameLobby"/>,
        /// it uses the <see cref="GameLobbyAuto"/>, which demonstrates how you 
        /// can extend game lobby functionality
        /// </summary>
        /// <param name="module"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static ILobby ThreeVsThreeQueue(LobbiesModule module, Dictionary<string, string> properties, IPeer creator)
        {
            // Create the teams
            var teamA = new LobbyTeam("Team Blue")
            {
                MaxPlayers = 3,
                MinPlayers = 1
            };
            var teamB = new LobbyTeam("Team Red")
            {
                MaxPlayers = 3,
                MinPlayers = 1
            };

            // Set their colors
            teamA.SetProperty("color", "0000FF");
            teamB.SetProperty("color", "FF0000");

            var config = new LobbyConfig()
            {
                EnableReadySystem = false,
                EnableManualStart = false
            };

            // Create the lobby
            var lobby = new BaseLobbyAuto(module.GenerateLobbyId(),
                new[] { teamA, teamB }, module, config)
            {
                Name = ExtractLobbyName(properties)
            };

            // Override properties with what user provided
            lobby.SetLobbyProperties(properties);

            // Add control for the game speed
            lobby.AddControl(new LobbyPropertyData()
            {
                Label = "Game Speed",
                Options = new List<string>() { "1x", "2x", "3x" },
                PropertyKey = "speed"
            }, "2x"); // Default option

            // Add control to enable/disable gravity
            lobby.AddControl(new LobbyPropertyData()
            {
                Label = "Gravity",
                Options = new List<string>() { "On", "Off" },
                PropertyKey = "gravity",
            });

            lobby.StartAutomation();

            return lobby;
        }

        public static string ExtractLobbyName(Dictionary<string, string> properties)
        {
            return properties.ContainsKey(MsfDictKeys.LobbyName) ? properties[MsfDictKeys.LobbyName] : DefaultName;
        }
    }
}