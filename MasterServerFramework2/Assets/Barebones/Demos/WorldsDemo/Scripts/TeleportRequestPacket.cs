using Barebones.Networking;
using UnityEngine;

public class TeleportRequestPacket : SerializablePacket
{
    public string Username;
    public string ZoneName;
    public string Position;

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(Username);
        writer.Write(ZoneName);
        writer.Write(Position);
    }

    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        Username = reader.ReadString();
        ZoneName = reader.ReadString();
        Position = reader.ReadString();
    }
}
