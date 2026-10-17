using System.Xml;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace TrackSymbolViewer;

public partial class MainWindow : Window
{
    static readonly IBrush CardBrush = new SolidColorBrush(Color.Parse("#1F2A31"));
    static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FF7A5C"));

    readonly List<SymbolView> zoomedViews = [];

    // Text editors can write a file in more than one step. The timer waits for the last change before a reload.
    readonly DispatcherTimer reloadTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };

    string? filePath;
    FileSystemWatcher? watcher;

    public MainWindow()
    {
        InitializeComponent();

        ZoomText.Text = $"{ZoomSlider.Value}x";
        ZoomSlider.ValueChanged += (_, _) => SetZoom(ZoomSlider.Value);

        reloadTimer.Tick += (_, _) =>
        {
            reloadTimer.Stop();
            Reload();
        };

        StatusText.Text = "Open a Tracks.xml file.";
    }

    public void OpenFile(string path)
    {
        filePath = Path.GetFullPath(path);
        FilePathText.Text = filePath;
        Watch(filePath);
        Reload();
    }

    async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Tracks.xml",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("XML files") { Patterns = ["*.xml"] }]
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is not null)
            OpenFile(path);
    }

    void Watch(string path)
    {
        watcher?.Dispose();

        watcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Renamed += OnFileChanged;
        watcher.EnableRaisingEvents = true;
    }

    void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            reloadTimer.Stop();
            reloadTimer.Start();
        });
    }

    void Reload()
    {
        if (filePath is null)
            return;

        List<TrackSymbol> symbols;
        try
        {
            symbols = TrackSymbolReader.Read(filePath);
        }
        catch (Exception exception) when (exception is IOException or XmlException or InvalidDataException)
        {
            // Keep the last symbols on the screen, so that a typing error does not clear the view.
            StatusText.Text = $"Cannot read the file: {exception.Message}";
            StatusText.Foreground = ErrorBrush;
            return;
        }

        SymbolsPanel.Children.Clear();
        zoomedViews.Clear();
        foreach (var symbol in symbols)
            SymbolsPanel.Children.Add(CreateCard(symbol));

        var errorCount = symbols.Count(s => s.Error is not null);
        StatusText.Text = $"{symbols.Count} symbols, {errorCount} with errors. Loaded at {DateTime.Now:HH:mm:ss}.";
        StatusText.ClearValue(TextBlock.ForegroundProperty);
    }

    Border CreateCard(TrackSymbol symbol)
    {
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = symbol.Title, FontWeight = FontWeight.Bold });

        if (symbol.Error is not null)
        {
            panel.Children.Add(new TextBlock
            {
                Text = symbol.Error,
                Foreground = ErrorBrush,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 280
            });
        }
        else
        {
            panel.Children.Add(new TextBlock { Text = symbol.Details, Opacity = 0.7 });

            var zoomedView = new SymbolView { Symbol = symbol, Zoom = ZoomSlider.Value };
            zoomedViews.Add(zoomedView);

            // vatSys draws track symbols without anti-aliasing. The actual size view does the same.
            var actualSizeView = new SymbolView { Symbol = symbol, Zoom = 1, VerticalAlignment = VerticalAlignment.Top };
            RenderOptions.SetEdgeMode(actualSizeView, EdgeMode.Aliased);

            panel.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children = { zoomedView, actualSizeView }
            });
        }

        return new Border
        {
            Child = panel,
            Background = CardBrush,
            CornerRadius = new Avalonia.CornerRadius(4),
            Padding = new Avalonia.Thickness(8),
            Margin = new Avalonia.Thickness(4)
        };
    }

    void SetZoom(double zoom)
    {
        ZoomText.Text = $"{zoom}x";
        foreach (var view in zoomedViews)
            view.Zoom = zoom;
    }

    protected override void OnClosed(EventArgs e)
    {
        watcher?.Dispose();
        reloadTimer.Stop();
        base.OnClosed(e);
    }
}
