# SSH快连（Fasst_ssh）

基于 .NET MAUI 的安卓 SSH 客户端，在安卓设备上像真实终端一样连接并操作服务器。

## 功能

- **连接页**：输入主机（IP 或 `IP:端口`）、端口（默认 22）、用户名、密码，支持「记住输入」，连接后进入终端页。
- **终端页**：基于 [xterm.js](https://xtermjs.org/) 的完整终端模拟器（WebView 内渲染），
  通过本地 WebSocket 桥（Fleck）+ [SSH.NET](https://github.com/sshnet/SSH.NET) 的 ShellStream 与服务器交互，
  支持 ANSI 颜色、光标、滚动、窗口自适应（旋转屏幕 / 软键盘弹出自动调整远端 `stty size`）。
- 固定深色主题；终端页顶栏提供手机键盘上没有的功能键（Ctrl / Alt / 方向键）。

## 技术架构

```
ConnectPage ──SSH连接──> SshSession(SshClient + ShellStream)
                              ▲   │  数据
                              │   ▼
                        TerminalBridge（Fleck 本地 WebSocket 服务器，127.0.0.1:22886）
                              ▲   │  ws://
                              │   ▼
TerminalPage（WebView + xterm.js，页面来自 Resources/Raw/www/index.html）
```

- 终端页面 `file:///android_asset/www/index.html` 由 `Resources/Raw/www/` 打包进安卓 assets。
- 协议：JS→C# 裸字符串为终端输入，`\u0000resize:cols:rows` 为窗口尺寸通知；C#→JS 裸字符串为终端输出。

## 详细技术栈

> 想了解完整的技术栈、每个文件的作用与关键设计思路？请查看
> [MarkDown/技术文档.md](MarkDown/技术文档.md)。

## 使用

1. 打开应用，输入服务器 IP、用户名、密码，点击「连接」。
2. 进入终端页后即可执行命令（`ls`、`top`、`vim`、`htop` 等）。
3. 点击顶栏「断开」或按返回键回到连接页并断开 SSH。

## 许可证

本项目基于 [MIT](LICENSE) 许可开源。
