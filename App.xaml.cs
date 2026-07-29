using System;
using System.Windows;
using NepTunnel.Services;

namespace NepTunnel
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Register emergency process exit handler for Task Manager / ALT+F4 force close
            AppDomain.CurrentDomain.ProcessExit += (s, ev) =>
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
                UdpProxy.StopProxy(wait: false);
                RobloxStudioService.StopAllStudioProcesses();
                RbxmBridgeServer.Stop();
            }
            catch { }
        }
    }
}
