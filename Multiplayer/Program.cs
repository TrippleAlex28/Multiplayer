using System;
using Engine.Network;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Global cleanup hooks
        AppDomain.CurrentDomain.ProcessExit += (_, __) =>
        {
            UpnpHelper.TryRemoveAllGameMappings();
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            UpnpHelper.TryRemoveAllGameMappings();
        };

        using var game = new Multiplayer.Multiplayer();
            game.Run();
    }
}