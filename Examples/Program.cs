using ValueExtensions;


Console.WriteLine(ValueOf<int, UserId>.TryCreateFrom(10, out var userId, out var userError) ? userId : $"Error: {userError}");

Console.WriteLine(EmailAddress.TryCreateFrom("sdfadsf", out EmailAddress? email, out var emailError) ? email : $"Error{emailError}");


public readonly record struct UserId : ValueOf<int, UserId>.AsVal
{
    public int Value { get; }

    private UserId(int value)
    {
        Value = value;
    }
    public static bool CanCreateFrom(int value, out string? error)
    {
        error = "Drop the attitude!";

        return false;
    }
}

public record EmailAddress : ValueOf<string, EmailAddress>.AsRef
{
    private EmailAddress(string value) : base(value)
    {
    }
}
