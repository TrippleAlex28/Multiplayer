using Engine.Network.Shared;
using Engine.Network.Shared.Session;
using Engine.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Multiplayer;

public class TestScene2 : SceneBase
{
    private string _typeString = "No Type";
    
    private NonReplicatedTestObject dummy;
    
    public TestScene2() : base("TestScene2")
    {
        dummy = new();
        WorldRoot.AddChild(dummy);
    }

    private KeyboardState prevKb;
    private KeyboardState currKb;
    public override void Update(GameTime gameTime)
    {
        prevKb = currKb;
        currKb = Keyboard.GetState();
        
        if (currKb.IsKeyDown(Keys.Enter) && prevKb.IsKeyUp(Keys.Enter))
        {
            SessionManager.Instance.CurrentSession?.SwitchScene("TestScene");
        }
        
        base.Update(gameTime);
    }

    public override void DrawUI(SpriteBatch spriteBatch)
    {
        base.DrawUI(spriteBatch);

        switch (SessionManager.Instance.CurrentType)
        {
            case SessionType.Singleplayer:
                _typeString = "SP";
                break;
            case SessionType.MultiplayerClient:
                _typeString = "MPClient";
                break;
            case SessionType.MultiplayerHost:
                _typeString = "MPHost";
                break;
        }

        spriteBatch.DrawString(
            Multiplayer.Arial,
            @"TestScene2
            [type]"
            .Replace("[type]", _typeString),
            new Vector2(20, 20),
            Color.Black
        );
    }
}