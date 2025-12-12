namespace Engine.Network.Shared.Packet;

public class Tcp_ConnectionAcceptPacket : Packet
{
    public override PacketType Type => PacketType.Tcp_ConnectionAccept;

    public int ClientId { get; private set; }
    public string HostName { get; private set; } = string.Empty;
    public int HostUdpPort { get; private set; }

    public string CurrentSceneKey { get; private set; }
    public int CurrentSceneEpoch { get; private set; }

    public Tcp_ConnectionAcceptPacket()
    {
        
    }

    public Tcp_ConnectionAcceptPacket(int clientId, string hostName, int hostUdpPort, string currentSceneKey, int currentSceneEpoch)
    {
        ClientId = clientId;
        HostName = hostName;
        HostUdpPort = hostUdpPort;

        CurrentSceneKey = currentSceneKey;
        CurrentSceneEpoch = currentSceneEpoch;
    }

    public override void SerializePayload(BinaryWriter w)
    {
        w.Write(ClientId);
        w.Write(HostName);
        w.Write(HostUdpPort);

        w.Write(CurrentSceneKey);
        w.Write(CurrentSceneEpoch);
    }

    public override void DeserializePayload(BinaryReader r)
    {
        ClientId = r.ReadInt32();
        HostName = r.ReadString();
        HostUdpPort = r.ReadInt32();

        CurrentSceneKey = r.ReadString();
        CurrentSceneEpoch = r.ReadInt32();
    }
}
