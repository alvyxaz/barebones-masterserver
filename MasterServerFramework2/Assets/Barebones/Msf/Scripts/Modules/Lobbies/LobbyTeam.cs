using System.Collections.Generic;

namespace Barebones.MasterServer
{
    public class LobbyTeam
    {
        /// <summary>
        /// Members of the team
        /// </summary>
        protected Dictionary<string, LobbyMember> Members;

        /// <summary>
        /// Team properties
        /// </summary>
        protected Dictionary<string, string> Properties;

        /// <summary>
        /// Min number of players, required in this team
        /// </summary>
        public int MinPlayers { get; set; }

        /// <summary>
        /// How many players can join this team
        /// </summary>
        public int MaxPlayers { get; set; }

        public LobbyTeam(string name)
        {
            Name = name;

            MinPlayers = 1;
            MaxPlayers = 5;

            Members = new Dictionary<string, LobbyMember>();
            Properties = new Dictionary<string, string>();
        }

        public string Name { get; private set; }

        /// <summary>
        /// Returns a number of members in this team
        /// </summary>
        public int PlayerCount
        {
            get { return Members.Count; }
        }

        /// <summary>
        /// Checks if a specific member can be added to the lobby
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual bool CanAddPlayer(LobbyMember member)
        {
            return PlayerCount < MaxPlayers;
        }

        /// <summary>
        /// Adds a member to the lobby
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public bool AddMember(LobbyMember member)
        {
            if (Members.ContainsKey(member.Username))
            {
                return false;
            }

            Members.Add(member.Username, member);
            member.Team = this;

            return true;
        }

        /// <summary>
        /// Removes a member from the lobby
        /// </summary>
        /// <param name="member"></param>
        public void RemoveMember(LobbyMember member)
        {
            Members.Remove(member.Username);

            if (member.Team == this)
                member.Team = null;
        }

        /// <summary>
        /// Sets lobby property to a specified value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetProperty(string key, string value)
        {
            Properties[key] = value;
        }

        /// <summary>
        /// Returns a MUTABLE dictionary of properties
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetTeamProperties()
        {
            return Properties;
        }

        /// <summary>
        /// Generates a lobby data packet
        /// </summary>
        /// <returns></returns>
        public LobbyTeamData GenerateData()
        {
            return new LobbyTeamData()
            {
                MaxPlayers = MaxPlayers,
                MinPlayers = MinPlayers,
                Name = Name,
                Properties = Properties
            };
        }
    }
}