using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ValueExtensions;


//Console.WriteLine(ValueOf<int, UserId>.TryCreateFrom(10, out var userId, out var userError) ? userId : $"Error: {userError}");

//Console.WriteLine(EmailAddress.TryCreateFrom("sdfadsf", out EmailAddress? email, out var emailError) ? email : $"Error{emailError}");


//Console.WriteLine(ValueOfDelegate<int, UserIdDelegate>.TryCreateFrom(10, out var userId2, out var userError2) ? userId2 : $"Error: {userError2}");

//Console.WriteLine(EmailAddress.TryCreateFrom("sdfadsf", out EmailAddress? email2, out var emailError2) ? email2 : $"Error{emailError2}");

var summary = BenchmarkRunner.Run(typeof(Program).Assembly);


[MemoryDiagnoser]
public class DelegateVsExpressionTree
{
    [Benchmark]
    public void WithExpressionTree()
    {
        ValueOf<int, UserId>.TryCreateFrom(10, out var userId, out var userError);
        EmailAddress.TryCreateFrom("sdfadsf", out EmailAddress? email, out var emailError);
    }

    [Benchmark]
    public void WithDelegate()
    {
        ValueOfDelegate<int, UserIdDelegate>.TryCreateFrom(10, out var userId2, out var userError2);
        EmailAddressDelegate.TryCreateFrom("sdfadsf", out EmailAddressDelegate? email2, out var emailError2);
    }
}

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

public readonly record struct UserIdDelegate : ValueOfDelegate<int, UserIdDelegate>.AsVal
{
    public int Value { get; }

    private UserIdDelegate(int value)
    {
        Value = value;
    }
    public static bool CanCreateFrom(int value, out string? error)
    {
        error = "Drop the attitude!";

        return false;
    }
}

public record EmailAddressDelegate : ValueOfDelegate<string, EmailAddressDelegate>.AsRef
{
    private EmailAddressDelegate(string value) : base(value)
    {
    }
}