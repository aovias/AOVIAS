namespace PersonService.Domain.ValueObjects;

public static class PersonNameHelper
{
    public static string ComputeSearchName(
        string? legalFirstName,
        string? legalLastName,
        string? displayName,
        string? preferredName)
    {
        var parts = new[]
        {
            legalFirstName,
            legalLastName,
            displayName,
            preferredName
        }.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim());

        return string.Join(' ', parts).ToUpperInvariant();
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        return new string(phone.Where(char.IsDigit).ToArray());
    }
}
