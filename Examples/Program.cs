using System.Text.RegularExpressions;
using ValueExtensions;


Console.WriteLine(ValueOf<int, UserId>.TryFrom(10, out var userId, out var userError) ? userId : $"Error: {userError}");

Console.WriteLine(EmailAddress.TryFrom("sdfadsf", out EmailAddress? email, out var emailError) ? email : $"Error{emailError}");

EmailAddress firstName = EmailAddress.From("sdf");


public readonly record struct UserId : ValueOf<int, UserId>.AsStruct
{
    public int Value { get; }

    private UserId(int value)
    {
        Value = value;
    }

    public static bool TryFrom(int value, out UserId userId)
    {
        return ValueOf<int, UserId>.TryFrom(10, out userId);
    }

    public static bool IsValid(int value)
    {
        return false;
    }
}

public record EmailAddress : ValueOf<string, EmailAddress>.AsClass
{
    private EmailAddress(string value) : base(value)
    {
    }

    public static bool IsValid(string value, out string? error)
    {
        bool isValid = Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

        error = isValid ? null : $"Invalid email: '{value}'.";

        return isValid;
    }
}
