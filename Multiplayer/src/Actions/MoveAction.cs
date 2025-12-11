using System.IO;
using Engine;
using Engine.Network.Shared;
using Engine.Network.Shared.Action;
using Engine.Network.Shared.State;
using Microsoft.Xna.Framework;

namespace Multiplayer;

public class MoveAction : NetAction
{
    public override byte Type => (byte)NetActionType.Move;
    
    public Vector2 DesiredDirection { get; private set; }
    
    public MoveAction()
    {
        
    }
    
    public MoveAction(InputSnapshot inputSnapshot)
    {
        DesiredDirection = inputSnapshot.DesiredMovementDirection;
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