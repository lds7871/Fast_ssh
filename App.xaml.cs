using Microsoft.Extensions.DependencyInjection;

namespace Fast_ssh;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// 固定深色主题（不提供切换）
		UserAppTheme = AppTheme.Dark;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
