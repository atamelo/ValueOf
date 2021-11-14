using System.Diagnostics.CodeAnalysis;

namespace ValueExtensions;

public record ValueOf<TValue, TThis> : Value.Of<TValue, TThis>
        where TThis : notnull, ValueOf<TValue, TThis>
        where TValue : notnull
{
    public TValue Value { get; }

    protected ValueOf(TValue value)
    {
        Value = value;
    }

    public static bool TryCreateFrom(
        TValue value,
        [NotNullWhen(true)] out TThis? newInstance)
    {
        return ValueOf<TValue, TThis>.TryCreateFrom(value, out newInstance);
    }

    public static bool TryCreateFrom(
        TValue value,
        [NotNullWhen(true)] out TThis? newInstance,
        [NotNullWhen(false)] out string? errorDescription)
    {
        return ValueOf<TValue, TThis>.TryCreateFrom(value, out newInstance, out errorDescription);
    }

    public static TThis CreateFrom(TValue value)
    {
        return ValueOf<TValue, TThis>.CreateFrom(value);
    }
}

