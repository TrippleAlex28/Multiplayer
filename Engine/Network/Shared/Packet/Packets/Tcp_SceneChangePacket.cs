namespace Engine.Network.Shared.Packet;

public class Tcp_SceneChangePacket : Packet
{
    public override PacketType Type => PacketType.Tcp_Chat;

    public int SceneEpoch { get; private set; }
    public string NewSceneKey { get; private set; } = string.Empty;

    public Tcp_SceneChangePacket()
    {
        
    }

    public Tcp_SceneChangePacket(int sceneEpoch, string newSceneKey)
    {
        SceneEpoch = sceneEpoch;
        NewSceneKey = newSceneKey;
    }

    public override void SerializePayload(BinaryWriter w)
    {
        w.Write(SceneEpoch);
        w.Write(NewSceneKey);
    }

    public override void DeserializePayload(BinaryReader r)
    {
        SceneEpoch = r.ReadInt32();
        NewSceneKey = r.ReadString();
    }
}
