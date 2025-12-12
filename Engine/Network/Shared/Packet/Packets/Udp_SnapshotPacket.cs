using Engine.Network.Shared.Object;
using Engine.Scene;

namespace Engine.Network.Shared.Packet;

public class Udp_SnapshotPacket : Packet
{
    public override PacketType Type => PacketType.Udp_Snapshot;

    public int SceneEpoch { get; private set; }
    public uint Tick { get; private set; }

    // for server-side reconciliation
    public Dictionary<int, uint> LastProcessedSequencePerClient { get; } = new();

    // Representation of gamestate
    public SceneRoot WorldRoot { get; private set; }

    public Udp_SnapshotPacket()
    {
        
    }

    public Udp_SnapshotPacket(int sceneEpoch, uint tick, Dictionary<int, uint> lastProcessedSeqPerClient, SceneRoot sceneRoot)
    {
        SceneEpoch = sceneEpoch;
        Tick = tick;
        LastProcessedSequencePerClient = lastProcessedSeqPerClient;
        WorldRoot = sceneRoot;
    }
    
    public override void SerializePayload(BinaryWriter w)
    {
        w.Write(SceneEpoch);
        w.Write(Tick);

        w.Write(LastProcessedSequencePerClient.Count);
        foreach (KeyValuePair<int, uint> kvp in LastProcessedSequencePerClient)
        {
            w.Write(kvp.Key);
            w.Write(kvp.Value);
        }

        SerializeTree(WorldRoot, w);
    }

    private void SerializeTree(GameObject root, BinaryWriter w)
    {
        w.Write((byte)root.TypeId);

        w.Write(root.NetworkId);
        w.Write(root.OwningClientId);

        root.SerializeProperties(w, false);

        List<GameObject> replicatedChildren = root.Children
            .Where(c => c.Replicate)
            .ToList();

        w.Write((uint)replicatedChildren.Count);


        foreach (GameObject child in replicatedChildren)
        {
            SerializeTree(child, w);
        }
    }
    
    public override void DeserializePayload(BinaryReader r)
    {
        this.SceneEpoch = r.ReadInt32();
        this.Tick = r.ReadUInt32();

        int ackCount = r.ReadInt32();
        for (int i = 0; i < ackCount; i++)
        {
            int clientId = r.ReadInt32();
            uint seq = r.ReadUInt32();
            this.LastProcessedSequencePerClient[clientId] = seq;
        }

        WorldRoot = (SceneRoot)DeserializeTree(r);
    }

    private GameObject DeserializeTree(BinaryReader r)
    {
        NetObjectTypeIds typeId = (NetObjectTypeIds)r.ReadByte();
        NetObject netObj = NetObjectFactory.Create(typeId);

        if (netObj is not GameObject obj)
            throw new InvalidDataException($"TypeId {typeId} is not a GameObject");

        obj.NetworkId = r.ReadInt32();
        obj.OwningClientId = r.ReadInt32();
        obj.DeserializeProperties(r);

        uint childCount = r.ReadUInt32();
        for (uint i = 0; i < childCount; i++)
        {
            GameObject child = DeserializeTree(r);
            obj.AddChild(child);
        }

        return obj;
    }
}
