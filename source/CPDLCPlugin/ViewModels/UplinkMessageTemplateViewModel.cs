using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CPDLCPlugin.Configuration;

namespace CPDLCPlugin.ViewModels;

public partial class UplinkMessageTemplateViewModel : ObservableObject
{
    [ObservableProperty] string content;
    [ObservableProperty] int elementId;

    [ObservableProperty] string displayText;
    [ObservableProperty] int maxCharacters;

    public UplinkMessageReference MessageReference { get; }

    public UplinkMessageTemplateViewModel(string content, int elementId, UplinkMessageReference messageReference)
    {
        Content = content;
        ElementId = elementId;
        MessageReference = messageReference;
        MaxCharacters = 250; // TODO: Calculate based on view width
        DisplayText = GetDisplayText(Content, ElementId, MaxCharacters);
    }

    static string GetDisplayText(string fullContent, int elementId, int maxCharacters)
    {
        var sb = new StringBuilder();

        sb.Append(" ");

        // "F:" for free text, and "E:" for distress free text
        sb.Append(elementId switch
        {
            UplinkElementIds.FreeText => "F:",
            UplinkElementIds.DistressFreeText => "E:",
            _ => "  "
        });

        var remainingLength = maxCharacters - sb.Length;
        if (fullContent.Length > remainingLength)
        {
            sb.Append(fullContent.Substring(0, remainingLength));
        }
        else
        {
            sb.Append(fullContent);
        }

        var totalLength = sb.Length + fullContent.Length;
        if (totalLength > maxCharacters)
        {
            sb[0] = '*';
        }

        return sb.ToString();
    }
}
