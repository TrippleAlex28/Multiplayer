namespace Engine.Network.Shared.Packet;

public class Tcp_ChatPacket : Packet
{
    public override PacketType Type => PacketType.Tcp_Chat;

    public string Sender { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    public Tcp_ChatPacket()
    {
        
    }

    public Tcp_ChatPacket(string sender, string message)
    {
        Sender = sender;
        Message = message;
    }

    public override void SerializePayload(BinaryWriter w)
    {
        w.Write(Sender);
        w.Write(Message);
    }

    public override void DeserializePayload(BinaryReader r)
    {
        Sender = r.ReadString();
        Message = r.ReadString();
    }
}
