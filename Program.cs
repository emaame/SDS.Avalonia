using Avalonia;
using System;

namespace SDS.Avalonia
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            JumpListManager.Initialize();

            if (args.Length >= 2 && args[0] == "--switch-device")
            {
                var deviceName = args[1];
                var devices = AudioController.GetPlaybackDevices();
                foreach (var device in devices)
                {
                    if (device.FriendlyName == deviceName)
                    {
                        AudioController.SetDefaultDevice(device.Id);
                        break;
                    }
                }
                return;
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp() => 
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .UseR3()
                .LogToTrace();
    }
}
