using Fasst_ssh.Services;

namespace Fasst_ssh.Pages;

public partial class ConnectPage : ContentPage
{
    private static string _lastHost = "";
    private static string _lastPort = "22";
    private static string _lastUser = "";
    private static string _lastPass = "";

    public ConnectPage()
    {
        InitializeComponent();

        // 读取「记住输入」的已存内容
        if (Preferences.Default.ContainsKey("remember_host"))
        {
            _lastHost = Preferences.Default.Get("remember_host", "");
            _lastPort = Preferences.Default.Get("remember_port", "22");
            _lastUser = Preferences.Default.Get("remember_user", "");
            _lastPass = Preferences.Default.Get("remember_pass", "");
            RememberCheck.IsChecked = true;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        HostEntry.Text = _lastHost;
        PortEntry.Text = _lastPort;
        UserEntry.Text = _lastUser;
        PassEntry.Text = _lastPass;

        // 主题按钮图标：当前暗色显示 🌙，亮色显示 ☀️
        ThemeBtn.Text = Application.Current?.UserAppTheme == AppTheme.Light ? "☀️" : "🌙";

        // 回到连接页时断开旧会话
        if (SshSession.Instance.IsConnected)
            SshSession.Instance.Disconnect();
    }

    // ---------- 主题切换 ----------

    private void OnThemeToggleClicked(object? sender, EventArgs e)
    {
        var next = Theme.Toggle();
        ThemeBtn.Text = next == AppTheme.Light ? "☀️" : "🌙";
    }

    private void OnRememberLabelTapped(object? sender, TappedEventArgs e)
    {
        RememberCheck.IsChecked = !RememberCheck.IsChecked;
    }

    // ---------- 连接 ----------

    private async void OnConnectClicked(object? sender, EventArgs e)
    {
        var hostInput = HostEntry.Text?.Trim() ?? "";
        var portInput = PortEntry.Text?.Trim() ?? "";
        var user = UserEntry.Text?.Trim() ?? "";
        var pass = PassEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(hostInput))
        {
            ShowError("请输入服务器 IP");
            return;
        }
        if (string.IsNullOrWhiteSpace(user))
        {
            ShowError("请输入用户名");
            return;
        }
        if (string.IsNullOrWhiteSpace(pass))
        {
            ShowError("请输入密码");
            return;
        }

        // 主机栏支持 "IP" | "IP:端口"
        var host = hostInput;
        var port = 22;
        if (hostInput.Contains(':'))
        {
            var parts = hostInput.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out var p) && p is > 0 and < 65536)
            {
                host = parts[0].Trim();
                port = p;
            }
            else
            {
                ShowError("端口格式不正确");
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(portInput))
        {
            if (!int.TryParse(portInput, out port) || port is <= 0 or >= 65536)
            {
                ShowError("端口格式不正确");
                return;
            }
        }

        SaveRemember(hostInput, portInput, user, pass);

        _lastHost = hostInput;
        _lastPort = portInput;
        _lastUser = user;
        _lastPass = pass;
        SetBusy(true);

        try
        {
            var result = await Task.Run(() => SshSession.Instance.Connect(host, port, user, pass));
            if (!result.ok)
            {
                ShowError(result.message ?? "连接失败");
                return;
            }

            PassEntry.Text = "";
            StatusLabel.IsVisible = false;
            await Shell.Current.GoToAsync("terminal");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SaveRemember(string host, string port, string user, string pass)
    {
        if (RememberCheck.IsChecked)
        {
            Preferences.Default.Set("remember_host", host);
            Preferences.Default.Set("remember_port", port);
            Preferences.Default.Set("remember_user", user);
            Preferences.Default.Set("remember_pass", pass);
        }
        else
        {
            Preferences.Default.Remove("remember_host");
            Preferences.Default.Remove("remember_port");
            Preferences.Default.Remove("remember_user");
            Preferences.Default.Remove("remember_pass");
        }
    }

    private void ShowError(string message)
    {
        StatusLabel.Text = message;
        StatusLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        ConnectBtn.IsEnabled = !busy;
        Busy.IsRunning = busy;
        Busy.IsVisible = busy;
    }
}
