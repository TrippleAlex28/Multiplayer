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

public class TestScene : SceneBase
{
    private readonly string _publicIP;

    private string _typeString = "No Type";
    
    private NonReplicatedTestObject dummy;
    
    public TestScene() : base("TestScene")
    {
        _publicIP = NetworkUtils.GetPublicIPAsync().GetAwaiter().GetResult();

        dummy = new();
        this.WorldRoot.AddChild(dummy);
    }

    public override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.D2))
        {
            SessionManager.Instance.CurrentSession?.SwitchScene("TestScene2");
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
            @"TestScene
            [type]
            [localIp]
            [publicIp]"
            .Replace("[type]", _typeString)
            .Replace("[localIp]", SessionManager.Instance.CurrentType == SessionType.MultiplayerHost ? ((MultiplayerHostSession)SessionManager.Instance.CurrentSession).BindAddress.ToString() : string.Empty)
            .Replace("[publicIp]", SessionManager.Instance.CurrentType == SessionType.MultiplayerHost ? _publicIP : string.Empty),
            new Vector2(20, 20),
            Color.Black
        );
    }
}