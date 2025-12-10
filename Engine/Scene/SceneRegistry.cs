namespace Engine.Scene;

public static class SceneRegistry
{
    private static readonly Dictionary<string, Func<SceneBase>> _constructors = new();

    public static void Register(string key, Func<SceneBase> constructor)
    {
        _constructors[key] = constructor;
    }

    public static SceneBase Create(string key)
    {
        if (!_constructors.TryGetValue(key, out var constuctor))
            throw new InvalidOperationException($"No scene registered for key: \'{key}\'");
        
        return constuctor();
    }
}