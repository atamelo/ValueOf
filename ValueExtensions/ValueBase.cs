using System.Diagnostics.CodeAnalysis;

namespace ValueExtensions
{
    public record ValueBase<TValue, TThis> : ValueOf<TValue, TThis>
            where TThis : notnull, ValueBase<TValue, TThis>
            where TValue : notnull
    {
        public TValue Value { get; init; }

        protected ValueBase(TValue value)
        {
            Value = value;
        }

        public static bool TryFrom(
            TValue value,
            [NotNullWhen(true)] out TThis? newInstance)
        {
            return ValueOf<TValue, TThis>.TryFrom(value, out newInstance);
        }

        public static bool TryFrom(
            TValue value,
            [NotNullWhen(true)] out TThis? newInstance,
            [NotNullWhen(false)] out string? errorDescription)
        {
            return ValueOf<TValue, TThis>.TryFrom(value, out newInstance, out errorDescription);
        }

        public static TThis From(TValue value)
        {
            return ValueOf<TValue, TThis>.From(value);
        }
    }
}
