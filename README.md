# ValueOf

A helper to deal with primitive obsession. Enables creation of types with value object semantics. Inspired by https://github.com/mcintyre321/ValueOf. This version has the following enhancements:
 - Doesn't use exceptions to communicate validation failures - cleaner code, easier to integrate with validation frameworks.
 - Supports structs - no pressure on GC .

## Scenarios

### Scenario 1 - no validation is needed, **reference type** value object.

Steps:

- Create a record **class** derived from `ValueOf<TValue, TThis>.AsClass`.
- Create a single-argument **private** constructor.

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

- Create a record **class** derived from `ValueOf<TValue, TThis>.AsClass` class.
- Create a single-argument **private** constructor.
- Define a bool-returning **public static** method named _IsValid_ with the signature `(TValue value, out string? error)`.
- Alternatively, you can create an arbitrarily named method with the same signature and mark it with the `[Validator]` attribute.

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
string someString = ...;

if (!EmailAddress.TryFrom(someString, out EmailAddress? email))
{//validation failed
    Console.WriteLine($"Error occurred.");
}
else
{
    // validation passed, use the 'email' instance
}
```

`EmailAddress.IsValid(...)` validation method will be discovered and used by the `TryFrom(...)` method to validate the passed in value parameter.

You can also hook up your validation framework of choice to the `EmailAddress.IsValid(...)` method. This is a way to keep validation logic inside your domain classes.

If validation **error message** is needed, the following API should be used:

```csharp
if (!EmailAddress.TryFrom(someString, out EmailAddress? email, out string? error))
{
    Console.WriteLine($"Error occurred. {error}");
}
```
### Scenario 3 - validation is needed, **value type** value object.
- Create a **readonly record struct** implementing `ValueOf<TValue, TThis>.AsStruct` interface.
- Create a single-argument **private** constructor.
- Implement the interface - create **public readonly** property named _Value_ of type `TValue`. Unfortunately, this boilerplate can't be implemented in the interface as it requires storing instance-specific state.
- Define a bool-returning **public static** method named _IsValid_ with the signature `(TValue value, out string? error)`.
- Alternatively, you can create an arbitrarily named method with the same signature and mark it with the `[Validator]` attribute.

```csharp
public readonly record struct UserId : ValueOf<int, UserId>.AsStruct
{
    public int Value { get; }

    private UserId(int value)
    {
        Value = value;
    }
    public static bool IsValid(int value, out string? error)
    {
        if (value < 0)
        {
            error = "UserId cannot be a negative value.";
            return false;
        }

        error = null;
        return false;
    }
}
```

Due to how the ```TryFrom/From``` methods are 'mixed in' to the struct (by means of default interface implementation), they end up unavailable to be called directly from the implementing type. Hence the API for instance creation is not as pretty as for reference-based `ValueOf` types:

```csharp
ValueOf<int, UserId>.TryFrom(10, out UserId userId);
```

To slightly improve the situation a 'forwarding' method can be added to a value type:

```csharp
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

    public static bool IsValid(int value, out string? error)
    {
        if (value < 0)
        {
            error = "UserId cannot be a negative value.";
            return false;
        }

        error = null;
        return false;
    }
}
```

Now the object creation syntax is much cleaner and fully matches the synteax for referece-based value objects:

```csharp
UserId.TryFrom(10, out UserId userId);
```

The obvious downside of this approach is that it requires users to implement extra boilerplate.

A totaly different alternative approach would be to use .NET 6 source code generators. When/If the new API proves to be the one to go forward with ;)