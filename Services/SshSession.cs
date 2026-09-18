using Renci.SshNet;

namespace Fast_ssh.Services;

/// <summary>
/// 全局唯一的 SSH 会话：负责连接服务器并创建交互式终端流（ShellStream）。
/// 数据的双向搬运由 TerminalBridge（本地 WebSocket 桥）完成。
/// </summary>
public sealed class SshSession : IDisposable
{
    public static SshSession Instance { get; } = new();

    private SshClient? _client;
    private readonly object _lock = new();

    /// <summary>当前交互式终端流（连接后由 OpenShell 创建）。</summary>
    public ShellStream? Shell { get; private set; }

    public string Host { get; private set; } = "";
    public string User { get; private set; } = "";
    public bool IsConnected => _client?.IsConnected == true;

    private SshSession() { }

    /// <summary>建立 SSH 连接（阻塞网络操作，调用方需放到后台线程）。</summary>
    public (bool ok, string? message) Connect(string host, int port, string user, string pass)
    {
        lock (_lock)
        {
            DisconnectLocked();
            try
            {
                var client = new SshClient(host, port, user, pass);
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(15);
                client.KeepAliveInterval = TimeSpan.FromSeconds(30);
                client.Connect();
                _client = client;
                Host = host;
                User = user;
                return (true, null);
            }
            catch (Exception ex)
            {
                _client = null;
                return (false, ex.Message);
            }
        }
    }

    /// <summary>在已连接的基础上创建交互式终端流（阻塞，需在后台线程调用）。</summary>
    public (bool ok, string? message) OpenShell(uint cols, uint rows)
    {
        lock (_lock)
        {
            try
            {
                if (_client is not { IsConnected: true })
                    return (false, "尚未连接到服务器");
                CloseShellLocked();
                Shell = _client.CreateShellStream("xterm-256color", cols, rows, 0, 0, 8192);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }

    /// <summary>调整远端终端窗口大小（xterm.js 自适应时调用）。</summary>
    public void ResizeShell(uint cols, uint rows)
    {
        try { Shell?.ChangeWindowSize(cols, rows, 0, 0); } catch { }
    }

    public void Disconnect()
    {
        lock (_lock) DisconnectLocked();
    }

    private void DisconnectLocked()
    {
        CloseShellLocked();
        try { _client?.Disconnect(); } catch { }
        try { _client?.Dispose(); } catch { }
        _client = null;
    }

    private void CloseShellLocked()
    {
        try { Shell?.Dispose(); } catch { }
        Shell = null;
    }

    public void Dispose() => Disconnect();
}
