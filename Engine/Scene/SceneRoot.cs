using Engine.Network.Shared.Object;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Scene;

public class SceneRoot : GameObject
{
    public override NetObjectTypeIds TypeId => NetObjectTypeIds.SceneRoot;
    
    static SceneRoot()
    {
        NetObjectFactory.Register<SceneRoot>(NetObjectTypeIds.SceneRoot);
    }
    
    public SceneRoot()
    {
        this.Replicate = true;
    }
    
    public void UpdateScene(GameTime gameTime)
    {
        Update(gameTime);
    }

    public void DrawScene(SpriteBatch spriteBatch)
    {
        Draw(spriteBatch);
    }

    // Prevent misuse: a SceneRoot should never be rmeoved from a parent, because it shouldn't ever have a parent
    public new void RemoveFromParent()
    {
        
    }

    // Basically same as above
    public new void RemoveSelf()
    {
        
    }
}