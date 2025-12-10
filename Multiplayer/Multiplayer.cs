using Engine.Network.Shared.Action;
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

    public Multiplayer()
    {
        #region Scenes
        SceneRegistry.Register("TestScene", () => new TestScene());
        #endregion

        #region Objects

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

        // CurrentSession = new SingleplayerSession(() => new Player());
        CurrentSession = new MultiplayerHostSession(() => new Player());
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

        InputSnapshot = new();
        if (currKb.IsKeyDown(Keys.A))
            InputSnapshot.DesiredMovementDirection.X -= 1;
        if (currKb.IsKeyDown(Keys.D))
            InputSnapshot.DesiredMovementDirection.X += 1;
        if (currKb.IsKeyDown(Keys.W))
            InputSnapshot.DesiredMovementDirection.Y -= 1;
        if (currKb.IsKeyDown(Keys.S))
            InputSnapshot.DesiredMovementDirection.Y += 1;

        CurrentSession.HandleInput(InputSnapshot);
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
}
