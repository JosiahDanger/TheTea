using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TheTea.ViewModels;
using TheTea.Views;

namespace TheTea
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new TimerWindow
                {
                    DataContext = new TimerWindowViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}