namespace Engine.Network.Shared.Object;

public abstract class NetObject
{
    public abstract NetObjectTypeIds TypeId { get; }
    
    public bool Replicate { get; protected set; } = false;
    public int NetworkId { get; set; } = -1;

    /// <summary>
    /// NetObjects can only be manipulated by their owning client, server owned = -1, singleplayer = 0
    /// </summary>
    public int OwningClientId { get; set; } = -1;

    private List<INetProperty> _properties = new();
    public IEnumerable<INetProperty> Properties => _properties;

    #region Properties
    protected NetProperty<T> RegisterProperty<T>(string name, Func<T> getter, Action<T> setter)
    {
        NetProperty<T> p = new(name, getter, setter);
        _properties.Add(p);
        return p;
    }
    #endregion

    #region Dirty
    public void UpdateDirtyFlags()
    {
        foreach (INetProperty p in _properties)
        {
            if (p is NetProperty<object> _) // unnecessary if I'm not retarded, but honestly I'm not so sure rn
            {
                p.UpdateDirtyFlag();
            }
        }
    }

    public void ClearDirtyFlags()
    {
        foreach (INetProperty p in _properties)
            p.ClearDirtyFlag();
    }
    #endregion

    #region Serialization
    public void SerializeProperties(BinaryWriter w, bool onlyDirty)
    {
        List<INetProperty> list = onlyDirty ? _properties.Where(p => p.IsDirty).ToList() : _properties;

        w.Write(list.Count);

        foreach (INetProperty p in list)
        {
            w.Write(p.Name);
            p.Serialize(w);
        }

    }

    public void DeserializeProperties(BinaryReader r)
    {
        int count = r.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            string name = r.ReadString();
            INetProperty? p = _properties.FirstOrDefault(p => p.Name == name);
            p?.Deserialize(r);
        }
    }
    #endregion
}

public enum NetObjectTypeIds : byte
{
    GameObject = 0,
    SceneRoot = 1,
    Player = 2,
}

/// <summary>
/// Registry so the client can spawn actors from a type ID
/// </summary>
public static class NetObjectFactory
{
    private static readonly Dictionary<NetObjectTypeIds, Func<NetObject>> _constructors = new();

    public static void Register<T>(NetObjectTypeIds typeId) where T : NetObject, new()
    {
        _constructors[typeId] = () => new T();
    }

    public static NetObject Create(NetObjectTypeIds typeId)
    {
        return _constructors[typeId]();
    }
}
