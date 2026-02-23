namespace CPDLCPlugin.LabelItems;

public class LabelItemProviderRegistry
{
    readonly IReadOnlyDictionary<string, ILabelItemProvider> _providers;

    public LabelItemProviderRegistry(IEnumerable<ILabelItemProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ItemTypePrefix);
    }

    public ILabelItemProvider? GetProvider(string itemType)
    {
        var prefix = itemType.EndsWith("_BG") ? itemType[..^3] : itemType;
        return _providers.TryGetValue(prefix, out var provider) ? provider : null;
    }
}
