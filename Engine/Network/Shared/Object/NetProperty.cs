using Microsoft.Xna.Framework;

namespace Engine.Network.Shared.Object;

public interface INetProperty
{
    string Name { get; }

    bool IsDirty { get; }
    void UpdateDirtyFlag();
    void ClearDirtyFlag();

    public void Serialize(BinaryWriter w);
    public void Deserialize(BinaryReader r);
}

public sealed class NetProperty<T> : INetProperty
{
    public string Name { get; }

    public bool IsDirty { get; private set; }
    private T _lastValue;

    private readonly Func<T> _getter;
    private readonly Action<T> _setter;

    public NetProperty(string name, Func<T> getter, Action<T> setter)
    {
        Name = name;
        _getter = getter;
        _setter = setter;
        _lastValue = getter();
    }

    #region Dirty
    public void UpdateDirtyFlag()
    {
        T current = _getter();
        if (!EqualityComparer<T>.Default.Equals(current, _lastValue))
        {
            _lastValue = current;
            IsDirty = true;
        }
    }

    public void ClearDirtyFlag() => IsDirty = false;
    #endregion

    #region Serialization
    public void Serialize(BinaryWriter w)
    {
        object? value = _getter();

        switch(default(T))
        {
            case bool:
                w.Write((bool)value!);
                break;
            case int:
                w.Write((int)value!);
                break;
            case float:
                w.Write((float)value!);
                break;
            case string:
                w.Write((string)value!);
                break;
            case Vector2:
                w.Write((Vector2)value!);
                break;
            default:
                throw new NotSupportedException($"NetProperty<T> - T: {typeof(T)} not supported!");
        }
    }

    public void Deserialize(BinaryReader r)
    {
        T value;

        switch(default(T))
        {
            case bool:
                value = (T)(object)r.ReadBoolean();
                break;
            case int:
                value = (T)(object)r.ReadInt32();
                break;
            case float:
               value = (T)(object)r.ReadSingle();
                break;
            case string:
                value = (T)(object)r.ReadString();
                break;
            case Vector2:
                value = (T)(object)r.ReadVector2();
                break;
            default:
                throw new NotSupportedException($"NetProperty<T> - T: {typeof(T)} not supported!");
        }

        _setter(value);
        _lastValue = value;
    }
    #endregion
}