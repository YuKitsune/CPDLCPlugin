using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace TrackSymbolViewer;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();

            // The first command-line argument is the path to a Tracks.xml file.
            var path = desktop.Args?.FirstOrDefault();
            if (path is not null)
                window.OpenFile(path);

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
