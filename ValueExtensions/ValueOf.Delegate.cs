using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ValueExtensions;

public class ValueOfDelegate<TValue, TThis>
        where TValue : notnull
        where TThis : notnull, ValueOfDelegate<TValue, TThis>.AsVal
{
    public static bool TryCreateFrom(
    TValue value,
    [NotNullWhen(true)] out TThis? newInstance)
    {
        return AsVal.TryCreateFrom(value, out newInstance, out _);
    }

    public static bool TryCreateFrom(
        TValue value,
        [NotNullWhen(true)] out TThis? newInstance,
        [NotNullWhen(false)] out string? errorDescription)
    {
        return AsVal.TryCreateFrom(value, out newInstance, out errorDescription);
    }

    public static TThis CreateFrom(TValue value)
    {
        return AsVal.CreateFrom(value);
    }

    public interface AsVal
    {
        private delegate bool CanCreateFromDelegate(TValue value, out string? error);
        private delegate bool CanCreateFromShortDelegate(TValue value);

        private static Func<TValue, TThis>? _newInstance;

        private static CanCreateFromDelegate? _canCreateFrom;

        public TValue Value { get; }

        public static bool TryCreateFrom(
            TValue value,
            [NotNullWhen(true)] out TThis? newInstance)
        {
            return TryCreateFrom(value, out newInstance, out _);
        }

        public static bool TryCreateFrom(
            TValue value,
            [NotNullWhen(true)] out TThis? newInstance,
            [NotNullWhen(false)] out string? errorDescription)
        {
            if (!CanCreateFrom(value, out errorDescription))
            {
                newInstance = default;
                return false;
            }

            newInstance = CreateNewInstance(value);

            return true;
        }

        public static TThis CreateFrom(TValue value)
        {
            static string EscapeIfNull<T>(T value) => $"{(value is not null ? $"[{value}]" : "<NULL>")}";

            if (!TryCreateFrom(value, out TThis? newInstance, out string? errorDescription))
            {
                throw new ArgumentException($"Can't create an instance of {typeof(TThis).FullName} type " +
                    $"from value {EscapeIfNull(value)} - validation failed with error: {EscapeIfNull(errorDescription)}");
            }

            return newInstance;
        }

        private static bool CanCreateFrom(TValue value, [NotNullWhen(false)] out string? errorDescription)
        {
            if (_canCreateFrom is not null)
            {
                return _canCreateFrom(value, out errorDescription);
            }

            MethodInfo[] methods = typeof(TThis).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);

            // First look for the extended version
            _canCreateFrom = TryCreateExtendedValidator(methods);

            if (_canCreateFrom is not null)
            {
                return _canCreateFrom(value, out errorDescription);
            }

            // then for the shorthand one
            _canCreateFrom = TryCreateShortValidator(methods);

            if (_canCreateFrom is not null)
            {
                return _canCreateFrom(value, out errorDescription);
            }

            // default
            _canCreateFrom = static (TValue _, out string? errorDescription) =>
            {
                errorDescription = null;
                return true;
            };

            return _canCreateFrom(value, out errorDescription);
        }

        private static CanCreateFromDelegate? TryCreateExtendedValidator(MethodInfo[] methods)
        {
            var validator =
                TryCreateValidator<CanCreateFromDelegate>(
                    methods,
                    new[] { typeof(TValue), typeof(string).MakeByRefType() }
                );

            return validator;
        }

        private static CanCreateFromDelegate? TryCreateShortValidator(MethodInfo[] methods)
        {
            CanCreateFromShortDelegate? shortValidator =
                TryCreateValidator<CanCreateFromShortDelegate>(
                    methods,
                    new[] { typeof(TValue) }
                );

            if (shortValidator is null)
            {
                return null;
            }

            CanCreateFromDelegate validator = (TValue value, out string? errorDescripton) =>
            {
                errorDescripton = shortValidator(value) ? null : "<NOT SPECIFIED>";

                return errorDescripton is null;
            };

            return validator;
        }

        private static TValidator? TryCreateValidator<TValidator>(MethodInfo[] methods, Type[] signature)
            where TValidator : Delegate
        {
            static bool ParametersMatch(MethodInfo method, Type[] signature)
                => Enumerable.SequenceEqual(method.GetParameters().Select(p => p.ParameterType), signature);

            // First try by convention
            MethodInfo? validationMethod =
                methods
                    .Where(method =>
                        method.Name == "CanCreateFrom" &&
                        ParametersMatch(method, signature))
                    .SingleOrDefault();

            if (validationMethod is null)
            {
                // then by configuration
                MethodInfo[] validationMethodsByConfiguration =
                    methods
                        .Where(method =>
                            method.GetCustomAttribute<ValidatorAttribute>() is not null &&
                            ParametersMatch(method, signature))
                        .ToArray();

                if (validationMethodsByConfiguration.Length == 0)
                {
                    return null;
                }

                if (validationMethodsByConfiguration.Length > 1)
                {
                    throw new ValueCreationException($"More than one validator is configured for target type '{typeof(TThis).FullName}'.");
                }

                validationMethod = validationMethodsByConfiguration[0];
            }

            var validator = CreateValidator<TValidator>(validationMethod);

            return validator;
        }

        private static TValidator CreateValidator<TValidator>(MethodInfo validationMethod)
            where TValidator : Delegate
        {
            try
            {
                var validator = (TValidator)Delegate.CreateDelegate(typeof(TValidator), validationMethod);

                return validator;
            }
            catch (Exception reason)
            {
                throw ValueCreationException(reason);
            }
        }

        private static TThis CreateNewInstance(TValue value)
        {
            if (_newInstance is null)
            {
                ConstructorInfo? ctor =
                    typeof(TThis)
                        .GetConstructor(
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly |
                            BindingFlags.Instance,
                            null,
                            new[] { typeof(TValue) },
                            null);

                if (ctor is null)
                {
                    throw new ValueCreationException($"Constructor with argument of type '{typeof(TValue).FullName}'" +
                        $"was not found for target type '{typeof(TThis).FullName}'.");
                }

                if (ctor.IsPublic)
                {
                    throw new ValueCreationException($"Constructor with argument of type '{typeof(TValue).FullName}'" +
                        $"for target type '{typeof(TThis).FullName}' cannot be public.");
                }

                // TODO: call to an existing method => use Delegate.CreateDelegate ?
                ParameterExpression constructorParameter = Expression.Parameter(typeof(TValue), "value");
                NewExpression newExpression = Expression.New(ctor, constructorParameter);

                try
                {
                    LambdaExpression newInsanceLambda = Expression.Lambda<Func<TValue, TThis>>(newExpression, constructorParameter);
                    _newInstance = (Func<TValue, TThis>)newInsanceLambda.Compile();
                }
                catch (Exception reason)
                {
                    throw ValueCreationException(reason);
                }
            }

            TThis newInstance = _newInstance(value);

            return newInstance;
        }

        private static Exception ValueCreationException(Exception reason)
        {
            return new ValueCreationException($"Error creation value for target type {typeof(TThis).FullName}", reason);
        }
    }

    public record AsRef : AsVal
    {
        public TValue Value { get; }

        protected AsRef(TValue value)
        {
            Value = value;
        }

        public static bool TryCreateFrom(
            TValue value,
            [NotNullWhen(true)] out TThis? newInstance)
        {
            return AsVal.TryCreateFrom(value, out newInstance);
        }

        public static bool TryCreateFrom(
            TValue value,
            [NotNullWhen(true)] out TThis? newInstance,
            [NotNullWhen(false)] out string? errorDescription)
        {
            return AsVal.TryCreateFrom(value, out newInstance, out errorDescription);
        }

        public static TThis CreateFrom(TValue value)
        {
            return AsVal.CreateFrom(value);
        }
    }
}


