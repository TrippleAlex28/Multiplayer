using Engine.Network.Shared.State;
using Microsoft.Xna.Framework;

namespace Engine.Network.Shared.Action;

public abstract class NetAction
{
    public abstract byte Type { get; }
    
    public uint SequenceNumber { get; set; }
    public uint Tick { get; protected set; }

    // Registry filled with constructors
    private static readonly Dictionary<byte, Func<NetAction>> _constructors = new();
    public static void RegisterAction(byte type, Func<NetAction> constructor)
    {
        _constructors[type] = constructor;
    }
    
    public abstract void Apply(GameState gs, int clientId);

    public void Serialize(BinaryWriter w)
    {
        w.Write(Type);

        w.Write(SequenceNumber);
        w.Write(Tick);

        SerializePayload(w);
    }

    public static NetAction Deserialize(BinaryReader r)
    {
        byte type = r.ReadByte();

        if (!_constructors.TryGetValue(type, out var constructor))
        {
            throw new InvalidDataException($"Unknown NetActionType: {type}");
        }

        NetAction action = constructor();

        action.SequenceNumber = r.ReadUInt32();
        action.Tick = r.ReadUInt32();

        action.DeserializePayload(r);
        
        return action;
    }

    public abstract void SerializePayload(BinaryWriter w);
    public abstract void DeserializePayload(BinaryReader r);
}

public sealed class InputSnapshot
{
    public Vector2 DesiredMovementDirection = Vector2.Zero;
}
