using Fleck;

namespace Fasst_ssh.Services;

/// <summary>
/// 本地 WebSocket 桥：把 WebView 里的 xterm.js 与 SSH ShellStream 双向连接。
/// WebView 页面通过 ws://127.0.0.1:{Port} 接入。
/// 协议约定：
///   JS → C#：裸字符串 = 终端输入；"\u0000resize:cols:rows" = 调整窗口大小。
///   C# → JS：裸字符串 = 终端输出（含 ANSI 转义序列，由 xterm.js 渲染）。
/// </summary>
public sealed class TerminalBridge : IDisposable
{
    public const int Port = 22886;

    private static readonly string ResizePrefix = "\u0000resize:";

    private WebSocketServer? _server;
    private readonly List<IWebSocketConnection> _clients = new();
    private readonly object _lock = new();

    /// <summary>WebSocket 客户端连接数变化时通知 UI。</summary>
    public event Action? ClientsChanged;

    public bool IsRunning => _server != null;

    public int ClientCount
    {
        get { lock (_lock) return _clients.Count; }
    }

    /// <summary>启动本地 WebSocket 服务器（绑定 127.0.0.1，仅本机可访问）。</summary>
    public bool Start()
    {
        try
        {
            _server = new WebSocketServer($"ws://127.0.0.1:{Port}");
            _server.RestartAfterListenError = true;
            _server.Start(socket =>
            {
                socket.OnOpen = () => AddClient(socket);
                socket.OnClose = () => RemoveClient(socket);
                socket.OnError = _ => RemoveClient(socket);
                socket.OnMessage = HandleMessage;
            });
            return true;
        }
        catch
        {
            try { _server?.Dispose(); } catch { }
            _server = null;
            return false;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            try { _server?.Dispose(); } catch { }
            _server = null;
            _clients.Clear();
        }
    }

    /// <summary>把终端输出推送给所有已连接的 WebView。</summary>
    public void Broadcast(string text)
    {
        IWebSocketConnection[] snapshot;
        lock (_lock) snapshot = _clients.ToArray();
        foreach (var c in snapshot)
        {
            try { c.Send(text); }
            catch { RemoveClient(c); }
        }
    }

    private void AddClient(IWebSocketConnection socket)
    {
        lock (_lock) _clients.Add(socket);
        ClientsChanged?.Invoke();
    }

    private void RemoveClient(IWebSocketConnection socket)
    {
        lock (_lock) _clients.Remove(socket);
        ClientsChanged?.Invoke();
    }

    private void HandleMessage(string message)
    {
        if (message.StartsWith(ResizePrefix, StringComparison.Ordinal))
        {
            // 格式：\u0000resize:cols:rows
            var parts = message[ResizePrefix.Length..].Split(':');
            if (parts.Length == 2 &&
                uint.TryParse(parts[0], out var cols) &&
                uint.TryParse(parts[1], out var rows) &&
                cols is > 0 and <= 500 && rows is > 0 and <= 300)
            {
                SshSession.Instance.ResizeShell(cols, rows);
            }
            return;
        }

        var shell = SshSession.Instance.Shell;
        if (shell == null) return;
        try
        {
            shell.Write(message);
            shell.Flush();
        }
        catch { }
    }

    public void Dispose() => Stop();
}
