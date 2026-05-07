using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace SDS.Avalonia
{
    public partial class App : Application
    {
        public override void Initialize() =>
            AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.Args is { Length: > 0 } && desktop.Args[0] == "--volume-popup")
                {
                    desktop.MainWindow = new VolumePopupWindow();
                }
                else
                {
                    desktop.MainWindow = new MainWindow()
                    {
                        DataContext = new MainWindowViewModel(),
                    };
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}