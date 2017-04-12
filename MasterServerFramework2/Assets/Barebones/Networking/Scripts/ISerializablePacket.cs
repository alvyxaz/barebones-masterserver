
namespace Barebones.Networking
{
    public interface ISerializablePacket
    {
        void ToBinaryWriter(EndianBinaryWriter writer);
        void FromBinaryReader(EndianBinaryReader reader);
        byte[] ToBytes();
    }
}