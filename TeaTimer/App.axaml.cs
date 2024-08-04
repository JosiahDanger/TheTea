using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using TeaTimer.ViewModels;
using TeaTimer.Views;

namespace TeaTimer
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