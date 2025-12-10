using Engine.Network.Shared.Action;

namespace Engine.Network.Shared.Packet;

public class Udp_ActionPacket : Packet
{
    public override PacketType Type => PacketType.Udp_Action;
    
    public int ClientId { get; private set; }
    public List<NetAction> Actions { get; private set; } = new();

    public Udp_ActionPacket()
    {
        
    }
    
    public Udp_ActionPacket(int clientId, List<NetAction> actions)
    {
        ClientId = clientId;
        Actions = actions;
    }

    public override void SerializePayload(BinaryWriter w)
    {
        w.Write(ClientId);
        
        w.Write(Actions.Count);
        foreach (NetAction a in Actions)
        {
            a.Serialize(w);
        }
    }

    public override void DeserializePayload(BinaryReader r)
    {
        Actions.Clear();

        ClientId = r.ReadInt32();
        
        int count = r.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            NetAction action = NetAction.Deserialize(r);
            Actions.Add(action);
        }
    }
}
