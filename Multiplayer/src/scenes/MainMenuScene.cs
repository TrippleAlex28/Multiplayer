using Engine;
using Engine.Network;
using Engine.Network.Shared;
using Engine.Network.Shared.Session;
using Engine.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Multiplayer;

public class MainMenuScene : SceneBase
{
    private const string _displayText = 
    @"Main Menu
    1) Continue Singleplayer Session
    2) Create MultiplayerClient Session
    3) Create MultiplayerHost Session";
    
    private NonReplicatedTestObject dummy;
    
    public MainMenuScene() : base("MainMenuScene")
    {
        dummy = new();
        WorldRoot.AddChild(dummy);
    }

    public override async void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.D1))
        {
            SessionManager.Instance.CurrentSession?.SwitchScene("TestScene");
        }
        if (Keyboard.GetState().IsKeyDown(Keys.D2))
        {
            await SessionManager.Instance.SwitchToMultiplayerClientAsync("192.168.2.23", "TestScene");
        }
        if (Keyboard.GetState().IsKeyDown(Keys.D3))
        {
            await SessionManager.Instance.SwitchToMultiplayerHostAsync("TestScene");
        }
        
        base.Update(gameTime);
    }

    public override void DrawUI(SpriteBatch spriteBatch)
    {
        base.DrawUI(spriteBatch);

        spriteBatch.DrawString(
            Multiplayer.Arial,
            _displayText,
            new Vector2(20, 20),
            Color.Black
        );
    }
}