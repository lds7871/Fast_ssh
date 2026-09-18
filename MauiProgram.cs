using Microsoft.Extensions.Logging;

namespace Fasst_ssh;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if ANDROID
		// 让 WebView 在触摸点击后能获得焦点，xterm.js 的隐藏输入框才能弹出软键盘
		builder.ConfigureMauiHandlers(handlers =>
		{
			handlers.AddHandler<Microsoft.Maui.Controls.WebView, FocusableWebViewHandler>();
		});
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

#if ANDROID
/// <summary>可聚焦的 WebView：保证点击终端区域后软键盘能弹出。</summary>
public sealed class FocusableWebViewHandler : Microsoft.Maui.Handlers.WebViewHandler
{
	protected override Android.Webkit.WebView CreatePlatformView()
	{
		var view = base.CreatePlatformView();
		view.Focusable = true;
		view.FocusableInTouchMode = true;
		return view;
	}
}
#endif
