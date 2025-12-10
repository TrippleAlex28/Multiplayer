using Engine.Scene;

namespace Multiplayer;

public class TestScene2 : SceneBase
{
    public TestScene2() : base("TestScene2")
    {
        WorldRoot.AddChild(
            new Player()
        );
    }
}