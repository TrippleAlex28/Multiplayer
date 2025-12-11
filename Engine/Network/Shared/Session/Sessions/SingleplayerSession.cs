using Engine.Network.Shared.Action;
using Engine.Network.Shared.State;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Network.Shared.Session;

public class SingleplayerSession : IGameSession
{
    public GameState gs { get; } = new("TestScene");

    /// <summary>
    /// Actions performed this frame, queued to be applied
    /// </summary>
    private List<NetAction> _frameActions = new();

    private bool _initialized = false;

    /// ----- SPAWNING CLASSES -----
    private Func<GameObject> _playerConstructor;

    public SingleplayerSession(Func<GameObject> playerConstructor)
    {
        _playerConstructor = playerConstructor;
    }

    public async Task Initialize()
    {
        gs.AddWorldObject(
            _playerConstructor(),
            owningClientId: 0
        );
        
        _initialized = true;
    }
    
    #region Tick
    public void HandleInput(List<NetAction> actions)
    {
        if (!_initialized) return;
        
        _frameActions = actions;
    }

    public void Update(GameTime gameTime)
    {
        if (!_initialized) return;

        foreach (NetAction action in _frameActions)
            action.Apply(gs, 0);

        gs.Update(gameTime);
    }
    
    public void DrawWorld(SpriteBatch spriteBatch)
    {
        if (!_initialized) return;
        
        gs.DrawWorld(spriteBatch);
    }

    public void DrawUI(SpriteBatch spriteBatch)
    {
        if (!_initialized) return;
        
        gs.DrawUI(spriteBatch);
    }
    #endregion

    #region Actions
    public void Stop()
    {
        
    }
    
    public void SwitchScene(string sceneKey)
    {
        if (!_initialized) return;
        
        gs.SwitchScene(sceneKey);

        gs.AddWorldObject(
            _playerConstructor(),
            owningClientId: 0
        );
    }
    #endregion
    
    public void Dispose()
    {
        
    }
}