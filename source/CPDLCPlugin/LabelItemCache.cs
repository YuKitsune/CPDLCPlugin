using CPDLCPlugin.LabelItems;

namespace CPDLCPlugin;

/// <summary>
///     In-memory cache of <see cref="CustomStripOrLabelItem"/>, so that label items can be generated asynchronously,
///     and vatSys can look them up when required without having to re-generate them each time.
/// </summary>
public class LabelItemCache
{
    readonly object _gate = new();
    IDictionary<string, CustomStripOrLabelItem> _items = new Dictionary<string, CustomStripOrLabelItem>();

    public CustomStripOrLabelItem? Find(string key)
    {
        lock (_gate)
        {
            return _items.TryGetValue(key, out var item) ? item : null;
        }
    }

    public void Replace(IDictionary<string, CustomStripOrLabelItem> newItems)
    {
        lock (_gate)
        {
            _items = newItems;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _items.Clear();
        }
    }
}
