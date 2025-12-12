using System.Runtime.CompilerServices;
using Engine;
using Engine.Network;
using Engine.Network.Shared;
using Engine.Network.Shared.Session;
using Engine.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Multiplayer;

public class LoadingScene : SceneBase
{
    public LoadingScene() : base("LoadingScene")
    {
    }

    private KeyboardState prevKb;
    private KeyboardState currKb;
    public override void Update(GameTime gameTime)
    {
        prevKb = currKb;
        currKb = Keyboard.GetState();
        
        if (currKb.IsKeyDown(Keys.Enter) && prevKb.IsKeyUp(Keys.Enter))
        {
            SessionManager.Instance.CurrentSession?.SwitchScene("TestScene2");
        }
        
        base.Update(gameTime);
    }

    public override void DrawUI(SpriteBatch spriteBatch)
    {
        base.DrawUI(spriteBatch);

        spriteBatch.DrawString(
            Multiplayer.Arial,
            "LOADING...",
            new Vector2(100, 100),
            Color.Black
        );
    }
}