using System.Net.Sockets;

namespace Engine.Network.Shared;

public class FramedTcp : IDisposable
{
    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;
    private readonly byte[] _lenBuffer = new byte[4];

    public FramedTcp(TcpClient tcp)
    {
        _tcp = tcp;
        _stream = tcp.GetStream();
    }

    public bool Connected => _tcp.Connected;

    public async Task SendAsync(byte[] payload)
    {
        if (!Connected)
            return;

        int length = payload.Length;
        byte[] buffer = new byte[4 + length];
        BitConverter.GetBytes(length).CopyTo(buffer, 0);
        payload.CopyTo(buffer, 4);

        await _stream.WriteAsync(buffer, 0, buffer.Length);
    }

    public async Task<byte[]?> ReceiveAsync(CancellationToken ct)
    {
        if (!await ReadExactAsync(_lenBuffer, 4, ct))
            return null;

        int length = BitConverter.ToInt32(_lenBuffer, 0);
        if (length <= 0 || length > 1024 * 1024) // 1MB safety
            return null;

        byte[] data = new byte[length];
        if (!await ReadExactAsync(data, length, ct))
            return null;
        return data;
    }

    private async Task<bool> ReadExactAsync(byte[] buffer, int count, CancellationToken ct)
    {
        int offset = 0;
        while (offset < count)
        {
            int read;
            try
            {
                read = await _stream.ReadAsync(buffer, offset, count - offset, ct);
            }
            catch
            {
                return false;
            }

            if (read <= 0) 
                return false;
                
            offset += read;
        }

        return true;
    }

    public void Dispose()
    {
        _stream.Dispose();
        _tcp.Close();
    }
}
