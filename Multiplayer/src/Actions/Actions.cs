using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Network.Shared.Action;

namespace Multiplayer;

public enum NetActionType
{
    Move
}

public sealed class InputSnapshot
{
    public Vector2 DesiredMovementDirection;
}

public static class InputToActionFactory
{
    private static readonly List<Func<InputSnapshot, NetAction>> _constructors = new();

    public static void Register(Func<InputSnapshot, NetAction> constructor)
    {
        _constructors.Add(constructor);
    }

    public static List<NetAction> Create(InputSnapshot inputSnapshot)
    {
        List<NetAction> actions = new();

        foreach (var constructor in _constructors)
        {
            NetAction action = constructor(inputSnapshot);   
            actions.Add(action);
        }

        return actions;
    }
}