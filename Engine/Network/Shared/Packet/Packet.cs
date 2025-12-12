namespace Engine.Network.Shared.Packet;

public enum PacketType : byte
{
    Tcp_ConnectionRequest = 0,  // Client -> Server: Request connection and send necessary setup data
    Tcp_ConnectionAccept = 1,   // Server -> Client: Accept client and send necessary setup data
    Tcp_Disconnect = 2,         // Server -> Client: Refuse connection request
                                // Client -> Server: This client has disconnected from the session
                                // Server -> Client: Client has been kicked by the server
    Tcp_Chat = 3,               // Both ways: contains a message and the sender
    Tcp_SceneChange = 4,        // Server -> Client: 

    Udp_Action = 50,            // Client -> Server: contains a list of actions the client performed
    Udp_Snapshot = 51,          // Server -> Client: contains a list of updated replicated objects
}

public abstract class Packet
{
    public abstract PacketType Type { get; }

    public void Serialize(BinaryWriter w)
    {
        w.Write((byte)Type);

        SerializePayload(w);
    }
    
    public byte[] CreatePayload()
    {
        using MemoryStream ms = new();
        using BinaryWriter w = new(ms);

        Serialize(w);

        return ms.ToArray();
    }
    
    public static Packet Deserialize(BinaryReader r)
    {
        PacketType type = (PacketType)r.ReadByte();

        Packet packet = type switch
        {
            PacketType.Tcp_ConnectionRequest => new Tcp_ConnectionRequestPacket(),
            PacketType.Tcp_ConnectionAccept => new Tcp_ConnectionAcceptPacket(),
            PacketType.Tcp_Disconnect => new Tcp_DisconnectPacket(),
            PacketType.Tcp_Chat => new Tcp_ChatPacket(),
            PacketType.Tcp_SceneChange => new Tcp_SceneChangePacket(),

            PacketType.Udp_Action => new Udp_ActionPacket(),
            PacketType.Udp_Snapshot => new Udp_SnapshotPacket(),

            _ => throw new InvalidDataException($"Unknown PacketType: {type}"),
        };

        packet.DeserializePayload(r);

        return packet;
    }

    public abstract void SerializePayload(BinaryWriter w);
    public abstract void DeserializePayload(BinaryReader r);
}