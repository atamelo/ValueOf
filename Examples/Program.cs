using ValueExtensions;


Console.WriteLine(ValueOf<int, UserId>.TryFrom(10, out var userId, out var userError) ? userId : $"Error: {userError}");

Console.WriteLine(EmailAddress.TryFrom("sdfadsf", out EmailAddress? email, out var emailError) ? email : $"Error{emailError}");


public readonly record struct UserId : ValueOf<int, UserId>.AsStruct
{
    public int Value { get; }

    private UserId(int value)
    {
        Value = value;
    }
    public static bool IsValid(int value, out string? error)
    {
        error = "Drop the attitude!";

        return false;
    }
}

public record EmailAddress : ValueOf<string, EmailAddress>.AsClass
{
    private EmailAddress(string value) : base(value)
    {
    }
}
