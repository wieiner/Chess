using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ChessApp;

internal sealed class ChessNetworkEndpoint : IDisposable
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private TcpListener? _listener;
    private TcpClient? _client;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cts;

    public event Action<ChessNetworkMessage>? MessageReceived;
    public event Action<string>? StatusChanged;
    public event Action? PeerConnected;

    public async Task StartHostAsync(int port)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        StatusChanged?.Invoke($"Endpoint: host 0.0.0.0:{port}, waiting");
        _ = AcceptLoopAsync(_cts.Token);
        await Task.CompletedTask;
    }

    public async Task ConnectAsync(string host, int port)
    {
        Stop();
        _cts = new CancellationTokenSource();
        var client = new TcpClient();
        await client.ConnectAsync(host, port, _cts.Token);
        AttachClient(client, $"Endpoint: connected to {host}:{port}");
    }

    public async Task SendAsync(ChessNetworkMessage message)
    {
        var writer = _writer;
        if (writer == null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(message);
        await _sendLock.WaitAsync();
        try
        {
            await writer.WriteLineAsync(json);
            await writer.FlushAsync();
        }
        catch (IOException ex)
        {
            StatusChanged?.Invoke($"Endpoint: send failed, {ex.Message}");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _writer?.Dispose();
        _writer = null;
        _client?.Dispose();
        _client = null;
        _listener?.Stop();
        _listener = null;
        StatusChanged?.Invoke("Endpoint: off");
    }

    public void Dispose()
    {
        Stop();
        _sendLock.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                AttachClient(client, $"Endpoint: peer {client.Client.RemoteEndPoint}");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Endpoint: accept failed, {ex.Message}");
            }
        }
    }

    private void AttachClient(TcpClient client, string status)
    {
        _client?.Dispose();
        _client = client;
        var stream = client.GetStream();
        _writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        StatusChanged?.Invoke(status);
        PeerConnected?.Invoke();
        _ = ReadLoopAsync(client, stream, _cts?.Token ?? CancellationToken.None);
    }

    private async Task ReadLoopAsync(TcpClient client, NetworkStream stream, CancellationToken token)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            while (!token.IsCancellationRequested && client.Connected)
            {
                var line = await reader.ReadLineAsync(token);
                if (line == null)
                {
                    break;
                }

                var message = JsonSerializer.Deserialize<ChessNetworkMessage>(line);
                if (message != null)
                {
                    MessageReceived?.Invoke(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException ex)
        {
            StatusChanged?.Invoke($"Endpoint: disconnected, {ex.Message}");
        }
        catch (JsonException ex)
        {
            StatusChanged?.Invoke($"Endpoint: bad message, {ex.Message}");
        }
    }
}

internal sealed class ChessNetworkMessage
{
    public string Type { get; set; } = "";
    public string Fen { get; set; } = "";
    public int FromFile { get; set; }
    public int FromRank { get; set; }
    public int ToFile { get; set; }
    public int ToRank { get; set; }
    public int Promotion { get; set; }
}
