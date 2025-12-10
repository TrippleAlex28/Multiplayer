namespace Engine.Network.Shared.Packet;

public class Tcp_DisconnectPacket : Packet
{
    public override PacketType Type => PacketType.Tcp_Disconnect;

    public string Reason { get; private set; } = "No Reason";

    public Tcp_DisconnectPacket()
    {
        
    }

    public Tcp_DisconnectPacket(string reason)
    {
        Reason = reason;
    }

    public override void SerializePayload(BinaryWriter w)
    {
        w.Write(Reason);
    }

    public override void DeserializePayload(BinaryReader r)
    {
        Reason = r.ReadString();
    }
}
