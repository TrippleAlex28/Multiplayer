using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Scene;

public class SceneBase
{
    public string Name { get; }

    public SceneRoot WorldRoot { get; }
    public SceneRoot UIRoot { get; }
    
    public SceneBase(string name)
    {
        Name = name;

        WorldRoot = new();
        UIRoot = new();
    }

    public virtual void Open()
    {
        
    }

    public virtual void Close()
    {
        
    }
    
    public virtual void Update(GameTime gameTime)
    {
        WorldRoot.UpdateScene(gameTime);
        UIRoot.UpdateScene(gameTime);
    }

    public virtual void DrawWorld(SpriteBatch spriteBatch)
    {
        WorldRoot.DrawScene(spriteBatch);
    }

    public virtual void DrawUI(SpriteBatch spriteBatch)
    {
        UIRoot.DrawScene(spriteBatch);
    }
}