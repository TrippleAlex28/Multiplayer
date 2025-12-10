using Engine.Network.Shared.State;
using Microsoft.Xna.Framework;

namespace Engine.Network.Shared.Action;

public enum NetActionType
{
    Move
}

public abstract class NetAction
{
    public abstract NetActionType Type { get; }
    
    public uint SequenceNumber { get; set; }
    public uint Tick { get; protected set; }

    public abstract void Apply(GameState gs, int clientId);

    public void Serialize(BinaryWriter w)
    {
        w.Write((byte)Type);

        w.Write(SequenceNumber);
        w.Write(Tick);

        SerializePayload(w);
    }

    public static NetAction Deserialize(BinaryReader r)
    {
        NetActionType type = (NetActionType)r.ReadByte();

        NetAction action = type switch
        {
            NetActionType.Move => new MoveAction(),

            _ => throw new InvalidDataException($"Unknown NetActionType: {type}")
        };

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

public static class NetActionFactory
{
    private readonly static Dictionary<NetActionType, Func<InputSnapshot, NetAction>> _constructors = new()
    {
        { NetActionType.Move, (InputSnapshot inputSnapshot) => new MoveAction(inputSnapshot.DesiredMovementDirection) }
    };

    public static void Register(NetActionType type, Func<InputSnapshot, NetAction> constructor)
    {
        _constructors[type] = constructor;
    }

    public static List<NetAction> Create(InputSnapshot inputSnapshot)
    {
        List<NetAction> actions = new();

        foreach (var kvp in _constructors)
        {
            NetAction action = kvp.Value(inputSnapshot);
            actions.Add(action);    
        }
        
        return actions;
    }
}