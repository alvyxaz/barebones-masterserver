using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    /// <summary>
    /// RegistrationPacket, containing data about which player changed which property
    /// </summary>
    public class LobbyMemberPropChangePacket : SerializablePacket
    {
        public int LobbyId;
        public string Username;
        public string Property;
        public string Value;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(LobbyId);
            writer.Write(Username);
            writer.Write(Property);
            writer.Write(Value);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            LobbyId = reader.ReadInt32();
            Username = reader.ReadString();
            Property = reader.ReadString();
            Value = reader.ReadString();
        }
    }
}