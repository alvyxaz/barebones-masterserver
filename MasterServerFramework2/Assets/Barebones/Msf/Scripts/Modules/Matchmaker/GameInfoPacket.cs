using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class GameInfoPacket : SerializablePacket
    {
        public string Address = "";
        public int Id;
        public GameInfoType Type;
        public string Name = "";

        public bool IsPasswordProtected;
        public int MaxPlayers;
        public int OnlinePlayers;
        public Dictionary<string, string> Properties;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Address);
            writer.Write(Id);
            writer.Write((int)Type);
            writer.Write(Name);

            writer.Write(IsPasswordProtected);
            writer.Write(MaxPlayers);
            writer.Write(OnlinePlayers);
            writer.Write(Properties);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Address = reader.ReadString();
            Id = reader.ReadInt32();
            Type = (GameInfoType) reader.ReadInt32();
            Name = reader.ReadString();

            IsPasswordProtected = reader.ReadBoolean();
            MaxPlayers = reader.ReadInt32();
            OnlinePlayers = reader.ReadInt32();
            Properties = reader.ReadDictionary();
        }

        public override string ToString()
        {
            return string.Format("[GameInfo: id: {0}, address: {1}, players: {2}/{3}, type: {4}]",
                Id, Address, OnlinePlayers, MaxPlayers, Type);
        }
    }
}