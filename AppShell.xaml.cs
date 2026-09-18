using Fasst_ssh.Pages;

namespace Fasst_ssh;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("terminal", typeof(TerminalPage));
	}
}
