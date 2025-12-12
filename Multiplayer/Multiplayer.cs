using Engine;
using Engine.Network;
using Engine.Network.Shared;
using Engine.Network.Shared.Action;
using Engine.Network.Shared.Object;
using Engine.Network.Shared.Session;
using Engine.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Multiplayer;

public class Multiplayer : Game
{    
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public static InputSnapshot InputSnapshot { get; set; }

    public static Texture2D BlankTexture;
    public static SpriteFont Arial;

    public Multiplayer()
    {
        #region Keybinds
        #endregion
        
        #region NetActions
        NetAction.RegisterAction(
            (byte)NetActionType.Move,
            () => new MoveAction()
        );
        InputToActionFactory.Register(
            (InputSnapshot input) => new MoveAction(input)
        );
        #endregion
        
        #region Objects
        NetObjectFactory.Register<GameObject>(NetObjectTypeIds.GameObject);
        NetObjectFactory.Register<SceneRoot>(NetObjectTypeIds.SceneRoot);
        NetObjectFactory.Register<Player>(NetObjectTypeIds.Player);
        #endregion
        
        #region Scenes
        SceneRegistry.Register("MainMenuScene", () => new MainMenuScene());
        SceneRegistry.Register("TestScene", () => new TestScene());
        SceneRegistry.Register("TestScene2", () => new TestScene2());
        #endregion
        
        SessionManager.Instance.PlayerConstructor = () => new Player();

        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override async void Initialize()
    {
        base.Initialize();

        await SessionManager.Instance.SwitchToSingleplayerAsync("MainMenuScene");
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        BlankTexture = new Texture2D(GraphicsDevice, 1, 1);
        BlankTexture.SetData([Color.White]);

        Arial = Content.Load<SpriteFont>("Fonts/Arial");
    }

    #region Tick
    KeyboardState currKb;
    KeyboardState prevKb;
    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        prevKb = currKb;
        currKb = Keyboard.GetState();
        
        InputSnapshot = new();
        if (currKb.IsKeyDown(Keys.A))
            InputSnapshot.DesiredMovementDirection.X -= 1;
        if (currKb.IsKeyDown(Keys.D))
            InputSnapshot.DesiredMovementDirection.X += 1;
        if (currKb.IsKeyDown(Keys.W))
            InputSnapshot.DesiredMovementDirection.Y -= 1;
        if (currKb.IsKeyDown(Keys.S))
            InputSnapshot.DesiredMovementDirection.Y += 1;

        SessionManager.Instance.HandleInput(InputToActionFactory.Create(InputSnapshot));
        SessionManager.Instance.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        SessionManager.Instance.DrawWorld(_spriteBatch);
        _spriteBatch.End();

        _spriteBatch.Begin();
        SessionManager.Instance.DrawUI(_spriteBatch);
        _spriteBatch.End();
    }
    #endregion

    #region MG Events
    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        SessionManager.Instance.ShutdownCurrentSession();

        // Extra safety: the server should automatically remove UPnP mappings when it stops
        // UpnpHelper.TryRemoveAllGameMappings();

        base.OnExiting(sender, args);
    }
    #endregion
}
