using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using NepTunnel.Services;

namespace NepTunnel;

public class App : Application
{
	private bool _contentLoaded;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		Logger.Log("NepTunnel Application Started.");
		PluginInstaller.EnsurePluginInstalled();
		AppDomain.CurrentDomain.ProcessExit += delegate
		{
			EmergencyCleanup();
		};
	}

	protected override void OnExit(ExitEventArgs e)
	{
		EmergencyCleanup();
		base.OnExit(e);
	}

	private static void EmergencyCleanup()
	{
		try
		{
			Logger.Log("Emergency cleanup executing - saving session logs...");
			Logger.FetchLatestRobloxStudioLog();
			UdpProxy.StopProxy(wait: false);
			RobloxStudioService.StopAllStudioProcesses();
			RbxmBridgeServer.Stop();
			Logger.Log("NepTunnel Session Ended cleanly.");
		}
		catch
		{
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.10.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			base.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
			Uri resourceLocator = new Uri("/NepTunnel;component/app.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.10.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
