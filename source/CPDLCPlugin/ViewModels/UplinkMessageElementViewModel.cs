using CommunityToolkit.Mvvm.ComponentModel;

namespace CPDLCPlugin.ViewModels;

public partial class UplinkMessageElementViewModel : ObservableObject
{
    public UplinkMessageElementViewModel()
    {
        ElementId = null;
        Parts = [];
    }

    public UplinkMessageElementViewModel(int elementId, IUplinkMessageElementComponentViewModel[] parts)
    {
        ElementId = elementId;
        Parts = parts;
    }

    // The FANS 1/A uplink element ID. It is null when the element is empty.
    [ObservableProperty] int? elementId;
    [ObservableProperty] IUplinkMessageElementComponentViewModel[] parts = [];

    public bool IsEmpty => Parts.Length == 0;

    public void Replace(int elementId, IUplinkMessageElementComponentViewModel[] parts)
    {
        ElementId = elementId;
        Parts = parts;
    }

    public void Clear()
    {
        ElementId = null;
        Parts = [];
    }
}
