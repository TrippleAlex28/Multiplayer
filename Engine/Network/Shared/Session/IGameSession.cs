using Engine.Network.Shared.Action;
using Engine.Network.Shared.State;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Network.Shared.Session;

public interface IGameSession : IDisposable
{
    public GameState gs { get; }

    Task Initialize();
    
    void HandleInput(List<NetAction> actions);
    void Update(GameTime gameTime);

    void DrawWorld(SpriteBatch spriteBatch);
    void DrawUI(SpriteBatch spriteBatch);

    void Stop();
    void SwitchScene(string sceneKey);
}