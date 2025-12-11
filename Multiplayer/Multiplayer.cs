using Engine;
using Engine.Network;
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

    public static IGameSession? CurrentSession { get; private set; }
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
        SceneRegistry.Register("TestScene", () => new TestScene());
        SceneRegistry.Register("TestScene2", () => new TestScene2());
        #endregion
        
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        BlankTexture = new Texture2D(GraphicsDevice, 1, 1);
        BlankTexture.SetData([Color.White]);

        Arial = Content.Load<SpriteFont>("Fonts/Arial");
        
        // CurrentSession = new SingleplayerSession(() => new Player());
        CurrentSession = new MultiplayerHostSession(() => new Player());
        ClientManager.Instance.NetRole = NetRole.Host;
        CurrentSession.Initialize();
    }

    KeyboardState currKb;
    KeyboardState prevKb;
    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        prevKb = currKb;
        currKb = Keyboard.GetState();

        if (currKb.IsKeyDown(Keys.Enter) && prevKb.IsKeyUp(Keys.Enter))
        {
            CurrentSession.SwitchScene("TestScene2");
        }
        
        InputSnapshot = new();
        if (currKb.IsKeyDown(Keys.A))
            InputSnapshot.DesiredMovementDirection.X -= 1;
        if (currKb.IsKeyDown(Keys.D))
            InputSnapshot.DesiredMovementDirection.X += 1;
        if (currKb.IsKeyDown(Keys.W))
            InputSnapshot.DesiredMovementDirection.Y -= 1;
        if (currKb.IsKeyDown(Keys.S))
            InputSnapshot.DesiredMovementDirection.Y += 1;

        CurrentSession.HandleInput(InputToActionFactory.Create(InputSnapshot));
        CurrentSession.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        CurrentSession.DrawWorld(_spriteBatch);
        _spriteBatch.End();

        _spriteBatch.Begin();
        CurrentSession.DrawUI(_spriteBatch);
        _spriteBatch.End();
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        CurrentSession.Stop();

        // Extra safety: the server should automatically remove UPnP mappings when it stops
        UpnpHelper.TryRemoveAllGameMappings();

        base.OnExiting(sender, args);
    }

}
