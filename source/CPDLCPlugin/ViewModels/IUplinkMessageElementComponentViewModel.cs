using CommunityToolkit.Mvvm.ComponentModel;
using CPDLC.Messages;

namespace CPDLCPlugin.ViewModels;

public interface IUplinkMessageElementComponentViewModel;

public partial class UplinkMessageTextElementComponentViewModel : ObservableObject, IUplinkMessageElementComponentViewModel
{
    [ObservableProperty]
    string value = string.Empty;

    public UplinkMessageTextElementComponentViewModel(string value)
    {
        Value = value;
    }
}

public partial class UplinkMessageTemplateElementComponentViewModel : ObservableObject, IUplinkMessageElementComponentViewModel
{
    [ObservableProperty]
    string placeholder = string.Empty;

    [ObservableProperty]
    string? value;

    [ObservableProperty]
    bool isEditing;

    public ParameterType ParameterType { get; }

    public int MaxLength => ParameterInputRules.MaxLength(ParameterType);

    public bool AllowsManualEntry => ParameterInputRules.AllowsManualEntry(ParameterType);

    // The values in the popup menu. An empty list means that there is no popup menu.
    public string[] FixedValues { get; }

#if DEBUG
    // For testing in the designer
    public UplinkMessageTemplateElementComponentViewModel()
        : this("[to/from]", ParameterType.ToFrom, ["TO", "FROM"])
    {
    }
#endif

    public UplinkMessageTemplateElementComponentViewModel(string placeholder, ParameterType parameterType, string[] fixedValues)
    {
        Placeholder = placeholder;
        ParameterType = parameterType;
        FixedValues = fixedValues;
        Value = string.Empty;
        IsEditing = false;
    }
}
