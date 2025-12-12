using System;
using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Multiplayer;

public class NonReplicatedTestObject : GameObject
{
    public NonReplicatedTestObject()
    {
        Random rand = new();
        this.GlobalPosition = new(rand.Next(0, 250));
    }
    
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(
            Multiplayer.BlankTexture,
            new Rectangle(
                this.GlobalPosition.ToPoint(),
                new Point(50, 50)
            ),
            Color.Red
        );
    }
}