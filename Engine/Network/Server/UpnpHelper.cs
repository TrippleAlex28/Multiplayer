using Open.Nat;

namespace Engine.Network.Server;

public static class UpnpHelper
{
    public const string DescriptionPrefix = "MonoGameEngineGame_";
    private const int MappingLifetimeSeconds = 7200;

    
    public static async Task<Mapping?> TryForwardTcpAsync(
        int port,
        string description,
        CancellationToken ct = default
    )
    {
        try
        {
            NatDiscoverer discoverer = new();

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            var mapping = new Mapping(Protocol.Tcp, port, port, MappingLifetimeSeconds, DescriptionPrefix + description);
            await device.CreatePortMapAsync(mapping);

            Console.WriteLine($"UPnP TCP forward created: {port}");

            return mapping;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UPnP TCP forward failed: {ex.Message}");
            return null;
        }
    }

    public static async Task<Mapping?> TryForwardUdpAsync(
        int port,
        string description,
        CancellationToken ct = default
    )
    {
        try
        {
            NatDiscoverer discoverer = new();

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            var mapping = new Mapping(Protocol.Udp, port, port, MappingLifetimeSeconds, DescriptionPrefix + description);
            await device.CreatePortMapAsync(mapping);

            Console.WriteLine($"UPnP UDP forward created: {port}");

            return mapping;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UPnP UDP forward failed: {ex.Message}");
            return null;
        }
    }

    public static async Task TryRemoveAsync(Mapping? mapping, CancellationToken ct = default)
    {
        if (mapping == null)
            return;

        try
        {
            NatDiscoverer discoverer = new();

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            
            var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            await device.DeletePortMapAsync(mapping);

            Console.WriteLine($"UPnP mapping removed: {mapping.PublicPort}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UPnP remove failed: {ex.Message}");
        }
    }

    public static async Task TryRemoveAllGameMappingsAsync(CancellationToken ct = default)
    {
        try
        {
            NatDiscoverer discoverer = new();

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            IEnumerable<Mapping> allMappings = await device.GetAllMappingsAsync();
            List<Mapping> gameMappings = allMappings
                .Where((m) => !string.IsNullOrEmpty(m.Description) && m.Description.StartsWith(DescriptionPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (Mapping m in gameMappings)
            {
                try
                {
                    await device.DeletePortMapAsync(m);
                    Console.WriteLine($"UPnP game mapping removed: {m.Protocol} {m.PublicPort}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to remove game mapping {m.PublicPort}: {ex.Message}");
                }
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UPnP cleanup failed: {ex.Message}");
        }
    }

    public static void TryRemoveAllGameMappings()
    {
        try
        {
            TryRemoveAllGameMappingsAsync().GetAwaiter().GetResult();
        }
        catch {}
    }
}