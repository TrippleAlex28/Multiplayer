namespace Engine.Network.Shared.Packet;

public class Tcp_ConnectionRequestPacket : Packet
{
    public override PacketType Type => PacketType.Tcp_ConnectionRequest;

    public string ClientName { get; private set; } = string.Empty;
    public int LocalUdpPort { get; private set; }

    public Tcp_ConnectionRequestPacket()
    {
        
    }
    
    public Tcp_ConnectionRequestPacket(string clientName, int localUdpPort)
    {
        ClientName = clientName;
        LocalUdpPort = localUdpPort;
    }

    public override void SerializePayload(BinaryWriter w)
    {
        w.Write(ClientName);
        w.Write(LocalUdpPort);
    }

    public override void DeserializePayload(BinaryReader r)
    {
        ClientName = r.ReadString();
        LocalUdpPort = r.ReadInt32();
    }
}
