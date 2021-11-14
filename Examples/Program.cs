using ValueExtensions;

UserId userId = ValueOf.AsVal<int, UserId>.CreateFrom(10);

Console.WriteLine(userId);

public readonly record struct UserId : ValueOf.AsVal<int, UserId>
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

public record EmailAddress : ValueOf.OfAsRef<string, EmailAddress>
{
    private EmailAddress(string value) : base(value)
    {
    }
}
