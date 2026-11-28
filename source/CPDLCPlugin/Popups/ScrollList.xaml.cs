using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CPDLCPlugin.ViewModels;
using vatSysToolbox.Controls;

namespace CPDLCPlugin.Popups;

// A list of options for a popup. It is always ItemCount items high, and it shows the default option in the middle.
public partial class ScrollList : UserControl
{
    public const double ItemHeight = 20;
    public const int ItemCount = 9;

    // The scroll viewer style puts the items in a sunken bevel, so the list is higher than the items
    public static readonly double ListHeight = ItemHeight * ItemCount + Bevel.BorderThickness.Top + Bevel.BorderThickness.Bottom;

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

    public static readonly DependencyProperty SelectCommandProperty = DependencyProperty.Register(
        nameof(SelectCommand),
        typeof(ICommand),
        typeof(ScrollList));

    // Runs when the controller selects an option. The parameter is the ScrollListOption.
    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

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
            // The scroll viewer style shows the items in an internal scroll viewer, so scroll that one
            if (List.Template.FindName("ListScrollViewer", List) is not ScrollViewer listScrollViewer ||
                listScrollViewer.Template.FindName("PART_InternalScrollViewer", listScrollViewer) is not ScrollViewer scrollViewer)
                return;

            // The style does not pass CanContentScroll to the internal scroll viewer.
            // Without it, the list scrolls by pixels, and the scroll bar buttons do not move one item at a time.
            scrollViewer.CanContentScroll = true;
            scrollViewer.UpdateLayout();

            var count = Options.Length;
            var defaultIndex = Array.FindIndex(Options, o => o.IsDefault);
            var middleIndex = defaultIndex >= 0 ? defaultIndex : count / 2;
            var firstIndex = Math.Max(0, Math.Min(middleIndex - ItemCount / 2, count - ItemCount));

            scrollViewer.ScrollToVerticalOffset(firstIndex);
        }, DispatcherPriority.Loaded);
    }
}
