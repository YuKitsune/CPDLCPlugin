using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace CPDLCServer.Settings;

/// <summary>
/// The contact of the operator for privacy requests: an email address, or an http or https URL.
/// </summary>
public static class PrivacyContactFormat
{
    public static bool IsEmail(string contact)
    {
        return !contact.Contains("://") && MailAddress.TryCreate(contact, out var address) && address.Address == contact;
    }

    public static bool IsUrl(string contact)
    {
        return Uri.TryCreate(contact, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    // The link of the contact on the privacy policy
    public static string ToHref(string contact)
    {
        return IsEmail(contact) ? $"mailto:{contact}" : contact;
    }
}

public class PrivacyContactAttribute : ValidationAttribute
{
    public PrivacyContactAttribute()
        : base("Enter an email address, or a URL that starts with https://")
    {
    }

    // An empty value is valid. Use [Required] when the value is necessary.
    public override bool IsValid(object? value)
    {
        return value is not string contact ||
               string.IsNullOrWhiteSpace(contact) ||
               PrivacyContactFormat.IsEmail(contact.Trim()) ||
               PrivacyContactFormat.IsUrl(contact.Trim());
    }
}
