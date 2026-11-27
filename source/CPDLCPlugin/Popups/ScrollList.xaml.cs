using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Popups;

// A list of options for a popup. It is always ItemCount items high, and it shows the default option in the middle.
public partial class ScrollList : UserControl
{
    public const double ItemHeight = 20;
    public const int ItemCount = 9;
    public const double ListHeight = ItemHeight * ItemCount;

    public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(
        nameof(Options),
        typeof(ScrollListOption[]),
        typeof(ScrollList),
        new PropertyMetadata(Array.Empty<ScrollListOption>(), (d, _) => ((ScrollList)d).ScrollToDefault()));

    public ScrollListOption[] Options
    {
        get => (ScrollListOption[])GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    // Occurs when the controller selects an option
    public event EventHandler<ScrollListOption>? OptionSelected;

    public ScrollList()
    {
        InitializeComponent();
    }

    // Puts the default option in the middle of the list. If there is no default option, shows the middle of the list.
    // The list scrolls by item, so the offset is a number of items.
    void ScrollToDefault()
    {
        // The items are made after the layout, so scroll after the layout is complete
        Dispatcher.InvokeAsync(() =>
        {
            if (List.Template.FindName("ListScrollViewer", List) is not ScrollViewer scrollViewer)
                return;

            var count = Options.Length;
            var defaultIndex = Array.FindIndex(Options, o => o.IsDefault);
            var middleIndex = defaultIndex >= 0 ? defaultIndex : count / 2;
            var firstIndex = Math.Max(0, Math.Min(middleIndex - ItemCount / 2, count - ItemCount));

            scrollViewer.ScrollToVerticalOffset(firstIndex);
        }, DispatcherPriority.Loaded);
    }

    void OptionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ScrollListOption option })
            OptionSelected?.Invoke(this, option);
    }
}
