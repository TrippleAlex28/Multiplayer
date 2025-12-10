using Engine.Network.Shared.Object;
using Engine.Network.Shared.State;
using Microsoft.Xna.Framework;

namespace Engine.Network.Shared.Action;

public class MoveAction : NetAction
{
    public override NetActionType Type => NetActionType.Move;
    
    public Vector2 DesiredDirection { get; private set; }
    
    public MoveAction()
    {
        
    }
    
    public MoveAction(Vector2 desiredDirection)
    {
        DesiredDirection = desiredDirection;
    }

    public override void Apply(GameState gs, int clientId)
    {
        GameObject? obj = gs.GetPawn(clientId);
        if (obj == null)
            return;

        obj.Direction = DesiredDirection;
    }

    public override void SerializePayload(BinaryWriter w)
    {
        w.Write(DesiredDirection);
    }

    public override void DeserializePayload(BinaryReader r)
    {
        DesiredDirection = r.ReadVector2();
    }
}