using Microsoft.Xna.Framework;

namespace Engine.Network.Shared;


public static class NetSerializationExtensions
{
    public static void Write(this BinaryWriter w, Vector2 value)
    {
        w.Write(value.X);
        w.Write(value.Y);
    }

    public static Vector2 ReadVector2(this BinaryReader r)
    {
        float x = r.ReadSingle();
        float y = r.ReadSingle();
        return new Vector2(x, y);
    }
}
