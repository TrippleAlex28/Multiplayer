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

        switch (ClientManager.Instance.NetRole)
        {
            case NetRole.Client:
                MultiplayerClientSession clientSession = (MultiplayerClientSession)Multiplayer.CurrentSession;
                spriteBatch.DrawString(
                    Multiplayer.Arial,
                    "CLIENT",
                    new Vector2(20, 20),
                    Color.Black
                );
                spriteBatch.DrawString(
                    Multiplayer.Arial,
                    $"Connected: {clientSession.Connected}",
                    new Vector2(20, 20),
                    Color.Black
                );
                break;
            case NetRole.Host:
                MultiplayerHostSession hostSession = (MultiplayerHostSession)Multiplayer.CurrentSession;
                spriteBatch.DrawString(
                    Multiplayer.Arial,
                    "HOST",
                    new Vector2(20, 20),
                    Color.Black
                );
                
                spriteBatch.DrawString(
                    Multiplayer.Arial,
                    $"Local TCP IP (LAN): {hostSession.BindAddress}:{hostSession.TcpPort}",
                    new Vector2(20, 50),
                    Color.Black
                );
                spriteBatch.DrawString(
                    Multiplayer.Arial,
                    $"Local UDP IP (LAN): {hostSession.BindAddress}:{hostSession.UdpPort}",
                    new Vector2(20, 75),
                    Color.Black
                );
                spriteBatch.DrawString(
                    Multiplayer.Arial,
                    $"PUBLIC IP: {_publicIP}",
                    new Vector2(20, 100),
                    Color.Black
                );
                break;
            default:
                break;
        }
    }
}