using System.Net;
using System.Net.Sockets;
using System.Text;
using Engine.Network.Shared;
using Engine.Network.Shared.Packet;

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
    private readonly int _maxPlayers;

    private readonly int _tcpPort;
    private TcpListener _tcpListener;

    private readonly int _udpPort;
    private UdpClient _udp;

    private readonly Dictionary<int, ClientConnection> _clients = new();
    private int _nextClientId = 1;

    private CancellationTokenSource? _cts;
    
    #region Events
    public event Action<ClientConnection>? ClientConnected;
    public event Action<ClientConnection>? ClientDisconnected;
    public event Action <Udp_ActionPacket>? ActionPacketReceived;
    public event Action? SendSnapshot;
    #endregion
    
    public NetServer(int maxPlayers = 2, int tcpPort = 7777, int udpPort = 0)
    {
        _maxPlayers = maxPlayers;

        _tcpPort = tcpPort;
        _tcpListener = new(IPAddress.Any, _tcpPort);

        _udp = new(udpPort);
        _udpPort = ((IPEndPoint)_udp.Client.LocalEndPoint!).Port;
    }

    #region Connection
    public void Start()
    {
        _tcpListener.Start();

        _cts = new();

        _ = AcceptLoopAsync(_cts.Token);
        _ = UdpReceiveLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();

        _tcpListener.Stop();
        _udp.Close();

        foreach (ClientConnection connection in _clients.Values)
        {
            ClientDisconnected?.Invoke(connection);

            connection.Framed.Dispose();
        }
        _clients.Clear();
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
        IPAddress remoteIp = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
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
                byte[]? receivedData = await framed.ReceiveAsync(ct);
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
    private void BroadcastTcp(byte[] payload, int? exceptClientId = null)
    {
        foreach (var kvp in _clients)
        {
            if (exceptClientId.HasValue && kvp.Key == exceptClientId.Value)
                continue;

            _ = kvp.Value.Framed.SendAsync(payload);
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
        if (connection == null)
            return;

        ClientDisconnected?.Invoke(connection);
        
        RemoveClient(connection);
    }

    private void RemoveClient(ClientConnection connection)
    {
        if (connection == null)
            return;

        try { connection.Framed.Dispose(); } catch {}

        _clients.Remove(connection.ClientId);
    }
    #endregion
    
    public void Dispose()
    {
        
    }
}