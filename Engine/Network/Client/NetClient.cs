using System.Net;
using System.Net.Sockets;
using Engine.Network.Shared;
using Engine.Network.Shared.Packet;

namespace Engine.Network.Client;

public sealed class NetClient : IDisposable
{
    public bool Connected { get; private set; } = false;
    public int ClientId { get; private set; }
    public string? HostName { get; private set; }
    
    private UdpClient? _udp;
    private IPEndPoint? _serverUdpEndPoint;

    private TcpClient? _tcp;
    private FramedTcp? _framed;
    private CancellationTokenSource? _cts;

    #region Events
    public event Action<string, string>? ChatMessageReceived;
    public event Action<Udp_SnapshotPacket>? SnapshotPacketReceived;
    public event Action<string>? Disconnected;
    #endregion
    
    public NetClient()
    {
        
    }

    #region Connection
    public async Task<bool> ConnectAsync(string host, int hostTcpPort = 7777, int localUdpPort = 0)
    {
        try
        {
            // Setup Tcp
            _tcp = new();
            await _tcp.ConnectAsync(host, hostTcpPort);
            _framed = new(_tcp);

            // Setup Udp
            _udp = new(localUdpPort);
            localUdpPort = ((IPEndPoint)_udp.Client.LocalEndPoint!).Port;

            // Send Tcp_ConnectionRequest
            Tcp_ConnectionRequestPacket crPacket = new(ClientManager.Instance.Name, localUdpPort);
            await _framed.SendAsync(crPacket.CreatePayload());

            // Read Respone for either Tcp_ConnectionAccept or TCP_Disconnect
            byte[]? data = await _framed.ReceiveAsync(CancellationToken.None);
            if (data == null)
                return false;
            
            Packet p;
            using (MemoryStream ms = new(data))
            using (BinaryReader r = new(ms))
            {
                p = Packet.Deserialize(r);
            }

            switch (p.Type)
            {
                case PacketType.Tcp_ConnectionAccept:
                    StartRunning((Tcp_ConnectionAcceptPacket)p, host);
                    return true;
                case PacketType.Tcp_Disconnect:
                    return false;
                default:
                    return false;
            }
        }
        catch
        {
            CleanupFailedConnect();
            return false;
        }
    }

    public async Task DisconnectAsync(string reason)
    {
        if (_framed == null)
            return;

        try
        {
            if (_framed.Connected)
            {
                Tcp_DisconnectPacket packet = new(reason);
                await _framed.SendAsync(packet.CreatePayload());
            }
        }
        catch {}
        finally
        {
            Disconnected?.Invoke(reason);
            StopRunning();
        }
    }
    #endregion

    #region Loops
    private async Task TcpReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _framed!.Connected)
            {
                byte[]? data = await _framed.ReceiveAsync(ct);
                if (data == null)
                    break;

                HandleTcpPacket(data);
            }
        }
        catch
        {
            await DisconnectAsync("TCP Receive network error");
        }
    }

    private async Task UdpReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                UdpReceiveResult result = await _udp!.ReceiveAsync();
                byte[] data = result.Buffer;

                HandleUdpPacket(data);
            }
        }
        catch
        {
            await DisconnectAsync("Lost UDP connection to server");
        }
    }
    #endregion

    #region Handlers
    private void HandleTcpPacket(byte[] data)
    {
        Packet p;
        using (MemoryStream ms = new(data))
        using (BinaryReader r = new(ms))
        {
            p = Packet.Deserialize(r);
        }

        switch (p.Type)
        {
            case PacketType.Tcp_Chat:
                HandleChatPacket((Tcp_ChatPacket)p);
                break;
            case PacketType.Tcp_Disconnect:
                HandleDisconnectPacket((Tcp_DisconnectPacket)p);
                break;
            default:
                break;
        }
    }

    private void HandleUdpPacket(byte[] data)
    {       
        Packet p;
        using (MemoryStream ms = new(data))
        using (BinaryReader r = new(ms))
        {
            p = Packet.Deserialize(r);
        }

        switch (p.Type)
        {
            case PacketType.Udp_Snapshot:
                HandleSnapshotPacket((Udp_SnapshotPacket)p);
                break;
            default:
                break;
        }
    }
    #endregion

    #region Packets
    public void SendActionPacket(Udp_ActionPacket packet)
    {
        if (_udp == null || _serverUdpEndPoint == null)
            return;

        byte[] payload = packet.CreatePayload();
        _udp.Send(payload, payload.Length, _serverUdpEndPoint);
    }
    
    private void HandleChatPacket(Tcp_ChatPacket packet)
    {
        ChatMessageReceived?.Invoke(packet.Sender, packet.Message);
    }

    private void HandleDisconnectPacket(Tcp_DisconnectPacket packet)
    {
        // Fire and forget
        _ = DisconnectAsync("Kicked by Server"); 
    }

    private void HandleSnapshotPacket(Udp_SnapshotPacket packet)
    {
        SnapshotPacketReceived?.Invoke(packet);
    }
    #endregion

    #region Helpers
    private void StartRunning(Tcp_ConnectionAcceptPacket packet, string host)
    {
        this.ClientId = packet.ClientId;
        this.HostName = packet.HostName;
        this._serverUdpEndPoint = new(IPAddress.Parse(host), packet.HostUdpPort);

        _cts = new();
        _ = TcpReceiveLoopAsync(_cts.Token);
        _ = UdpReceiveLoopAsync(_cts.Token);

        Connected = true;
    }

    private void StopRunning()
    {
        Connected = false;
        
        _cts?.Cancel();
        _framed?.Dispose();
        _udp?.Close();

        _framed = null;
        _tcp = null;
        _udp = null;
    }

    private void CleanupFailedConnect()
    {
        try { _framed?.Dispose(); } catch {}
        try { _udp?.Dispose(); } catch {}

        _framed =  null;
        _tcp = null;
        _udp = null;
    }
    #endregion

    public void Dispose()
    {
        _framed?.Dispose();
        _udp?.Dispose();
    }
}