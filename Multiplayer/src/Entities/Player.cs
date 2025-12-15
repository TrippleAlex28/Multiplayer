using Engine;
using Engine.Network.Shared.Object;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Multiplayer;

public class Player : GameObject
{
    private static Color[] playerCols = [Color.White, Color.Black, Color.Red, Color.Blue, Color.Green];
    
    public override NetObjectTypeIds TypeId => NetObjectTypeIds.Player;

    public Player()
    {
        this.Replicate = true;

        this.Velocity = 100;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(
            Multiplayer.BlankTexture,
            new Rectangle(
                this.GlobalPosition.ToPoint(),
                new Point(40, 80)
            ),
            playerCols[this.OwningClientId]
        );
    }   
}