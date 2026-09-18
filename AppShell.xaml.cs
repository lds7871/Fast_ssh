using Fast_ssh.Pages;

namespace Fast_ssh;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("terminal", typeof(TerminalPage));
	}
}
