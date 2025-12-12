using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Engine.Network.Shared;
using Engine.Network.Shared.Packet;
using Open.Nat;

namespace Engine.Network.Server;

public sealed class ClientConnection
{
    public int ClientId;
    public string ClientName;

    public FramedTcp Framed;
    public bool Connected => Framed.Connected;

    public IPEndPoint UdpEndPoint;
}

public sealed class NetServer : IDisposable
{
    public bool Running { get; private set; } = false;
    
    private readonly int _maxPlayers;

    private readonly int _tcpPort;
    public int TcpPort => _tcpPort;
    private TcpListener _tcpListener;

    private readonly int _udpPort;
    public int UdpPort => _udpPort;
    private UdpClient _udp;

    private readonly bool _useUpnp;
    private Mapping? _upnpTcpMapping;
    private Mapping? _upnpUdpMapping;

    public IPAddress BindAddress { get; private set; }

    private readonly Dictionary<int, ClientConnection> _clients = new();
    private int _nextClientId = 1;
    public IEnumerable<KeyValuePair<int, ClientConnection>> Clients => _clients;

    private CancellationTokenSource? _cts;
    
    #region Events
    public event Action<ClientConnection>? ClientConnected;
    public event Action<ClientConnection>? ClientDisconnected;
    public event Action <Udp_ActionPacket>? ActionPacketReceived;
    #endregion
    
    public NetServer(int maxPlayers = 2, int tcpPort = 7777, int udpPort = 0, bool bindToAllInterfaces = true, bool useUpnp = true)
    {
        _maxPlayers = maxPlayers;
        _tcpPort = tcpPort;
        _useUpnp = useUpnp;

        if (bindToAllInterfaces)
        {
            BindAddress = NetworkUtils.GetPrefferedOutboundIPv4() ?? IPAddress.Loopback;
            _tcpListener = new(IPAddress.Any, _tcpPort);
        }
        else
        {
            BindAddress = NetworkUtils.GetServerBindAddress();
            _tcpListener = new TcpListener(BindAddress, _tcpPort);
        }

        IPAddress udpBindAddress = bindToAllInterfaces ? IPAddress.Any : BindAddress;
        _udp = new(new IPEndPoint(udpBindAddress, udpPort));
        _udpPort = ((IPEndPoint)_udp.Client.LocalEndPoint!).Port;

        Console.WriteLine($"TCP: {BindAddress}:{TcpPort}");
        Console.WriteLine($"UDP: {BindAddress}:{UdpPort}");
        _ = LogPublicIPAsync();
    }

    private async Task LogPublicIPAsync()
    {
        
    }
    
    #region Connection
    public void Start()
    {
        _tcpListener.Start();

        _cts = new();

        _ = AcceptLoopAsync(_cts.Token);
        _ = UdpReceiveLoopAsync(_cts.Token);

        if (_useUpnp)
        {
            // Fire & Forget
            _ = Task.Run(async () =>
            {
                _upnpTcpMapping = await UpnpHelper.TryForwardTcpAsync(_tcpPort, "TCP", _cts.Token);
                _upnpUdpMapping = await UpnpHelper.TryForwardUdpAsync(_udpPort, "UDP", _cts.Token);

                if (_upnpTcpMapping == null || _upnpUdpMapping == null)
                {
                    Console.WriteLine("Auto Port-Forwarding Failed, Please Port Forward Manually");
                }
            });
        }

        Running = true;
    }

    public void Stop()
    {
        if (!Running)
            return;

        try
        {
            BroadcastTcp(new Tcp_DisconnectPacket("Server shutting down").CreatePayload());
        }
        catch {}
        
        Running = false;
        
        _cts?.Cancel();
        
        _tcpListener.Stop();
        _udp.Close();

        foreach (ClientConnection connection in _clients.Values)
        {
            DisconnectClient(connection);
        }
        _clients.Clear();

        if (_useUpnp)
        {
            // Fire & Forget / Best effort cleanup
            _ = Task.Run(async () =>
            {
                await UpnpHelper.TryRemoveAsync(_upnpTcpMapping);
                await UpnpHelper.TryRemoveAsync(_upnpUdpMapping);
            });
        }
    }
    #endregion
    
    #region Actions
    public async Task KickClient(int clientId)
    {
        if (!Running)
            return;

        if (_clients.TryGetValue(clientId, out var connection))
        {
            await connection.Framed.SendAsync(new Tcp_DisconnectPacket("You have been kicked from the server").CreatePayload());
            DisconnectClient(connection);
        }

    }
    #endregion
    
    #region Loops
    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync(ct);
                
                _ = TcpReceiveLoopAsync(tcpClient, ct);
            }
        }
        catch {}
    }

    private async Task TcpReceiveLoopAsync(TcpClient tcpClient, CancellationToken ct)
    {
        FramedTcp framed = new(tcpClient);
        
        // Check if data was sent
        byte[]? data = await framed.ReceiveAsync(ct);
        if (data == null)
            return;

        // Check if the right packet was sent
        Tcp_ConnectionRequestPacket crPacket;
        using (MemoryStream ms = new(data))
        using (BinaryReader r = new(ms))
        {
            Packet p = Packet.Deserialize(r);
            if (p.Type != PacketType.Tcp_ConnectionRequest)
            {
                framed.Dispose();
                return;
            }

            crPacket = (Tcp_ConnectionRequestPacket)p;
        }
        
        // Max players check
        if (_clients.Count >= _maxPlayers)
        {
            using (MemoryStream ms = new())
            using (BinaryWriter w = new(ms))
            {
                await framed.SendAsync(new Tcp_DisconnectPacket("Server Full").CreatePayload());
            }
            return;
        }

        // Add client to the list 
        int clientId = _nextClientId++;
        IPAddress remoteIp = ((IPEndPoint)tcpClient.Client.RemoteEndPoint!).Address;
        IPEndPoint udpEndPoint = new(remoteIp, crPacket.LocalUdpPort);
        ClientConnection connection = new()
        {
            ClientId = clientId,
            ClientName = crPacket.ClientName,

            Framed = framed,
            UdpEndPoint = udpEndPoint,
        };
        _clients.Add(clientId, connection);
        ClientConnected?.Invoke(connection);

        // Send accept packet
        await framed.SendAsync(new Tcp_ConnectionAcceptPacket(clientId, ClientManager.Instance.Name, _udpPort).CreatePayload());

        // Listen for furthur TCP packets from this client
        try
        {
            while (!ct.IsCancellationRequested && framed.Connected)
            {
                byte[]? receivedData;
                
                try
                {
                    receivedData = await framed.ReceiveAsync(ct);
                }
                catch (IOException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                
                if (receivedData == null)
                    break;

                HandleTcpPacket(connection, receivedData);
            }
        }
        finally
        {
            // Client connection lost
            DisconnectClient(connection);
        }
    }
    
    private async Task UdpReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                UdpReceiveResult result = await _udp.ReceiveAsync(ct);

                // get connection data
                ClientConnection? connection = _clients.Values.Where((c) => c.UdpEndPoint.Equals(result.RemoteEndPoint)).FirstOrDefault();
                if (connection == null)
                    continue;

                byte[] data = result.Buffer;

                HandleUdpPacket(connection, data);
            }
        }
        catch {}
    }
    #endregion
    
    #region Handlers
    private void HandleTcpPacket(ClientConnection connection, byte[] data)
    {
        Packet packet;
        using (MemoryStream ms = new(data))
        using (BinaryReader r = new(ms))
        {
            packet = Packet.Deserialize(r);
        }  

        switch (packet.Type)
        {
            case PacketType.Tcp_Disconnect:
                HandleDisconnectPacket((Tcp_DisconnectPacket)packet, connection);
                break;  
            case PacketType.Tcp_Chat:
                HandleChatPacket((Tcp_ChatPacket)packet);
                break;
            default:
                break;
        }

    }

    private void HandleUdpPacket(ClientConnection connection, byte[] data)
    {
        Packet packet;
        using (MemoryStream ms = new(data))
        using (BinaryReader r = new(ms))
        {
            packet = Packet.Deserialize(r);
        } 

        switch (packet.Type)
        {
            case PacketType.Udp_Action:
                HandleActionPacket((Udp_ActionPacket)packet);
                break;
            default: 
                break;
        }
    }
    #endregion

    #region Packets
    private void HandleChatPacket(Tcp_ChatPacket packet)
    {
        BroadcastTcp(packet.CreatePayload());
    }

    private void HandleDisconnectPacket(Tcp_DisconnectPacket packet, ClientConnection connection)
    {
        DisconnectClient(connection);
    }
    
    private void HandleActionPacket(Udp_ActionPacket packet)
    {
        ActionPacketReceived?.Invoke(packet);
    }
    #endregion
    
    #region Broadcast
    public void BroadcastTcp(byte[] payload, int? exceptClientId = null)
    {
        foreach (var kvp in _clients)
        {
            if (exceptClientId.HasValue && kvp.Key == exceptClientId.Value)
                continue;

            try
            {
                if (kvp.Value.Framed.Connected)
                {
                    _ = kvp.Value.Framed.SendAsync(payload);
                }
            }
            catch (IOException)
            {
                DisconnectClient(kvp.Value);
            }
            catch (ObjectDisposedException)
            {
                DisconnectClient(kvp.Value);
            }
        }
    }

    public void BroadcastUdp(byte[] payload, int? exceptClientId = null)
    {
        foreach (var kvp in _clients)
        {
            if (exceptClientId.HasValue && kvp.Key == exceptClientId.Value)
                continue;

            _udp.Send(payload, payload.Length, kvp.Value.UdpEndPoint);
        }
    }
    #endregion 
    
    #region Helpers
    private void DisconnectClient(ClientConnection connection)
    {
        if (connection == null || !_clients.ContainsKey(connection.ClientId))
            return;

        ClientDisconnected?.Invoke(connection);
        
        RemoveClient(connection);
    }

    private void RemoveClient(ClientConnection connection)
    {
        if (connection == null || !_clients.ContainsKey(connection.ClientId))
            return;

        try { connection.Framed.Dispose(); } catch {}

        _clients.Remove(connection.ClientId);
    }
    #endregion
    
    public void Dispose()
    {
        
    }
}