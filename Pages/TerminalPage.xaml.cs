using Fasst_ssh.Services;

namespace Fasst_ssh.Pages;

public partial class TerminalPage : ContentPage
{
    private readonly TerminalBridge _bridge = new();
    private bool _webViewLoaded;
    private bool _started;
    private bool _ctrlOn;
    private bool _altOn;

    public TerminalPage()
    {
        InitializeComponent();
        SessionLabel.Text = $"{SshSession.Instance.User}@{SshSession.Instance.Host}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await StartTerminalAsync();
        }
        catch (Exception ex)
        {
            await ShowFatalErrorAsync(ex.Message);
        }
    }

    private async Task StartTerminalAsync()
    {
        if (_started) return;
        _started = true;

        // 1) 启动本地 WebSocket 桥（WebView 里 xterm.js 连接到这里）
        if (!_bridge.Start())
        {
            await ShowFatalErrorAsync("无法启动本地终端服务");
            return;
        }

        // 2) 打开 SSH 交互式终端流（先 80x24，等 xterm fit 后再调整真实尺寸）
        var open = await Task.Run(() => SshSession.Instance.OpenShell(80, 24));
        if (!open.ok)
        {
            _bridge.Stop();
            _started = false;
            await ShowFatalErrorAsync(open.message ?? "无法打开终端");
            return;
        }

        // 3) 启动读线程：ShellStream → WebSocket → xterm.js
        _ = Task.Run(PumpShellToClients);

        // 4) 加载/重载 xterm.js 页面
        if (!_webViewLoaded)
        {
            _webViewLoaded = true;
            TerminalWebView.Source = new UrlWebViewSource { Url = "file:///android_asset/www/index.html" };
        }
        else
        {
            await TerminalWebView.EvaluateJavaScriptAsync("window.__reconnect && window.__reconnect()");
        }
    }

    /// <summary>后台线程：把 SSH 终端输出推给 WebView（阻塞读取 ShellStream）。</summary>
    private void PumpShellToClients()
    {
        var shell = SshSession.Instance.Shell;
        if (shell == null) return;
        try
        {
            using var reader = new StreamReader(shell, System.Text.Encoding.UTF8, false, 8192, leaveOpen: true);
            var buf = new char[4096];
            while (_bridge.IsRunning)
            {
                int n;
                try { n = reader.Read(buf, 0, buf.Length); }
                catch { break; }
                if (n <= 0) break;
                _bridge.Broadcast(new string(buf, 0, n));
            }
        }
        catch { }
    }

    private async Task ShowFatalErrorAsync(string message)
    {
        try
        {
            await DisplayAlertAsync("错误", message, "返回");
            await Shell.Current.GoToAsync("..");
        }
        catch { }
    }

    // ---------- 功能键（Ctrl / Alt / 方向键） ----------

    private void OnCtrlClicked(object? sender, EventArgs e)
    {
        _ctrlOn = !_ctrlOn;
        UpdateKeyVisual(CtrlBtn, _ctrlOn);
        _ = TerminalWebView.EvaluateJavaScriptAsync("window.__key('ctrl')");
    }

    private void OnAltClicked(object? sender, EventArgs e)
    {
        _altOn = !_altOn;
        UpdateKeyVisual(AltBtn, _altOn);
        _ = TerminalWebView.EvaluateJavaScriptAsync("window.__key('alt')");
    }

    private void OnArrowClicked(object? sender, EventArgs e)
    {
        var key = (sender as Button)?.Text switch
        {
            "↑" => "up",
            "↓" => "down",
            "←" => "left",
            "→" => "right",
            _ => null
        };
        if (key != null)
            _ = TerminalWebView.EvaluateJavaScriptAsync($"window.__key('{key}')");
    }

    /// <summary>更新功能键激活视觉：激活时用强调色背景，未激活时普通面板色。</summary>
    private void UpdateKeyVisual(Button btn, bool on)
    {
        btn.BackgroundColor = on ? Res("AccentDark") : Res("Surface2Dark");
        btn.TextColor = on ? Res("TextOnAccentDark") : Res("TextPrimaryDark");
    }

    private static Color Res(string key)
        => Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c ? c : Colors.White;

    private async void OnDisconnectClicked(object? sender, EventArgs e)
    {
        await DisconnectAndBackAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        // Android 返回键：断开并返回连接页
        _ = DisconnectAndBackAsync();
        return true;
    }

    private async Task DisconnectAndBackAsync()
    {
        try
        {
            SshSession.Instance.Disconnect();
            _bridge.Stop();
            await Shell.Current.GoToAsync("..");
        }
        catch { }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // 无论以何种方式离开本页，都清理 SSH 会话与本地桥
        SshSession.Instance.Disconnect();
        _bridge.Stop();
    }
}
