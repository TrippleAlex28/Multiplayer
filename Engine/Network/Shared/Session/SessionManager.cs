using Engine.Network.Shared.Action;
using Engine.Network.Shared.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Network.Shared;

public enum SessionType
{
    None,
    Singleplayer,
    MultiplayerClient,
    MultiplayerHost,
}

public sealed class SessionManager
{
    #region Singleton
    private static SessionManager? _instance;
    public static SessionManager Instance
    {
        get
        {
            _instance ??= new SessionManager();
            return _instance;
        }
    }
    private SessionManager()
    {
        
    }
    #endregion
    
    public IGameSession? CurrentSession { get; private set; }
    public SessionType CurrentType { get; private set; } = SessionType.None;

    /// <summary>
    /// The name of the active scene
    /// </summary>
    public string? CurrentSceneKey => CurrentSession?.gs?.CurrentScene?.Name;

    /// <summary>
    /// Factory used to spawn the local player in sessions that need it (SP and MPHost)
    /// MUST be set by the game at startup
    /// </summary>
    public Func<GameObject>? PlayerConstructor { get; set; }

    public event Action<IGameSession?, SessionType?>? SessionChanged;
    public event Action<string?> SceneChanged;

    #region Session Switching 
    public async Task SwitchToSingleplayerAsync(string? sceneKey = null)
    {
        EnsurePlayerConstructor();
        
        SingleplayerSession newSession = new(PlayerConstructor!);

        await SetSessionAsync(newSession, SessionType.Singleplayer, sceneKey);
    }

    public async Task SwitchToMultiplayerClientAsync(string host, string? sceneKey = null)
    {
        MultiplayerClientSession newSession = new();

        await SetSessionAsync(newSession, SessionType.MultiplayerClient, sceneKey);

        await newSession.ConnectAsync(host);
    }

    public async Task SwitchToMultiplayerHostAsync(string? sceneKey = null)
    {
        EnsurePlayerConstructor();
        
        MultiplayerHostSession newSession = new(PlayerConstructor!);

        await SetSessionAsync(newSession, SessionType.MultiplayerHost, sceneKey);
    }

    public void ShutdownCurrentSession()
    {
        if (CurrentSession != null)
        {
            CurrentSession.Stop();
            CurrentSession.Dispose();
        }

        CurrentSession = null;
        CurrentType = SessionType.None;
        SessionChanged?.Invoke(null, CurrentType);
    }
    #endregion

    #region Forward Game Loop
    public void HandleInput(List<NetAction> actions)
    {
        CurrentSession?.HandleInput(actions);
    }

    public void Update(GameTime gameTime)
    {
        CurrentSession?.Update(gameTime);
    }

    public void DrawWorld(SpriteBatch spriteBatch)
    {
        CurrentSession?.DrawWorld(spriteBatch);
    }

    public void DrawUI(SpriteBatch spriteBatch)
    {
        CurrentSession?.DrawUI(spriteBatch);
    }
    #endregion

    #region Helpers
    private async Task SetSessionAsync(IGameSession newSession, SessionType newType, string? requestedSceneKey)
    {
        string? targetSceneKey = requestedSceneKey;

        if (targetSceneKey == null && CurrentSession?.gs?.CurrentScene != null)
            targetSceneKey = CurrentSession.gs.CurrentScene.Name;

        // Cleanup previous session
        if (CurrentSession != null)
        {
            try
            {
                CurrentSession.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SessionManager: error stopping old session: {ex.Message}");
            }

            CurrentSession.Dispose();
        }

        await newSession.Initialize();
        
        CurrentSession = newSession;
        CurrentType = newType;
        SessionChanged?.Invoke(CurrentSession, CurrentType);

        if (targetSceneKey != null && newType != SessionType.MultiplayerClient)
        {
            CurrentSession.SwitchScene(targetSceneKey);
            SceneChanged?.Invoke(targetSceneKey);
        }
    }

    private void EnsurePlayerConstructor()
    {
        if (PlayerConstructor == null)
            throw new InvalidOperationException("SessionManager: PlayerConstructor must be set before creating a Singleplayer or MultiplayerHost session");
    }
    #endregion
}