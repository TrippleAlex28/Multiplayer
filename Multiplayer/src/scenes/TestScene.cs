using Engine;
using Engine.Network;
using Engine.Network.Shared.Session;
using Engine.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Multiplayer;

public class TestScene : SceneBase
{
    private readonly string _publicIP;
    
    public TestScene() : base("TestScene")
    {
        _publicIP = NetworkUtils.GetPublicIPAsync().GetAwaiter().GetResult();
    }

    public override void DrawUI(SpriteBatch spriteBatch)
    {
        base.DrawUI(spriteBatch);

        if (!(ClientManager.Instance.NetRole == Engine.Network.NetRole.Host))
            return;
        
        MultiplayerHostSession session = (MultiplayerHostSession)Multiplayer.CurrentSession;

        spriteBatch.DrawString(
            Multiplayer.Arial,
            "HOST",
            new Vector2(20, 20),
            Color.Black
        );
        
        spriteBatch.DrawString(
            Multiplayer.Arial,
            $"Local TCP IP (LAN): {session.BindAddress}:{session.TcpPort}",
            new Vector2(20, 50),
            Color.Black
        );
        spriteBatch.DrawString(
            Multiplayer.Arial,
            $"Local UDP IP (LAN): {session.BindAddress}:{session.UdpPort}",
            new Vector2(20, 75),
            Color.Black
        );
        spriteBatch.DrawString(
            Multiplayer.Arial,
            $"PUBLIC IP: {_publicIP}",
            new Vector2(20, 100),
            Color.Black
        );
    }
}