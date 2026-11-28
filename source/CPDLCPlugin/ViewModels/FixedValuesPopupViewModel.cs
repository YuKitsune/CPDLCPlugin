using CommunityToolkit.Mvvm.Input;
using CPDLC.Messages;

namespace CPDLCPlugin.ViewModels;

// The fixed values of a parameter. The controller can select only one of these values.
public partial class FixedValuesPopupViewModel
{
    readonly UplinkMessageTemplateElementComponentViewModel _templatePart;
    readonly Action _close;

    public string[] Values { get; }

#if DEBUG
    // For testing in the designer
    public FixedValuesPopupViewModel()
        : this(new UplinkMessageTemplateElementComponentViewModel("[to/from]", ParameterType.ToFrom, ["TO", "FROM"]), () => { })
    {
    }
#endif

    // close closes the popup after the controller selects a value
    public FixedValuesPopupViewModel(UplinkMessageTemplateElementComponentViewModel templatePart, Action close)
    {
        _templatePart = templatePart;
        _close = close;
        Values = templatePart.FixedValues;
    }

    [RelayCommand]
    void Select(string value)
    {
        _templatePart.Value = value;
        _close();
    }
}
