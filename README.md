# ValueOf

A helper to deal with primitive obsession. Enables creation of types with value object semantics. Inspired by https://github.com/mcintyre321/ValueOf. But this version doesn't use exceptions to communicate validation failures.

## Scenarios

### Scenario 1 - no validation is needed, **reference type** value object.

Steps:

- Create a record **class** derived from `ValueOf<TValue, TThis>.AsClass`
- Create a single-argument **private** contructor

```csharp
public record FirstName : ValueOf<string, FirstName>.AsClass
{
    private FirstName(string value) : base(value)
    {
    }
}
```

To construct an instance, use the following API:

```csharp
FirstName firstName = FirstName.From("John");
```

### Scenario 2 - validation is needed, **reference type** value object.

Steps:

- Create a record **class** derived from `ValueOf<TValue, TThis>.AsClass`
- Create a single-argument **private** contructor
- Define a bool-returning **public static** method named _IsValid_ with the signature `(TValue value, out string? error)`
- Alternatively, you can create an arbitrarily named method with the same signature and mark it with the `[Validator]` attribute

```csharp
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
```

To construct an instance, use the following API:

```csharp
if (!EmailAddress.TryFrom("my@my.com", out var email, out var error))
{
    Console.WriteLine($"Error occurred. {error}");
}
else
{
    // use the 'email' instance
}
```

You can also hook up your validation framework of choice to the `EmailAddress.IsValid(...)` method. This is a way to keep validation logic inside your domain classes.
