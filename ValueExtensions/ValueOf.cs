using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ValueExtensions
{
    public interface ValueOf<TValue, TThis>
        where TValue : notnull
        where TThis : notnull, ValueOf<TValue, TThis>
    {
        private delegate bool CanBeCreatedFromDelegate(TValue value, out string? error);
        private delegate bool CanBeCreatedFromShortDelegate(TValue value);

        private static Func<TValue, TThis>? _newInstance;

        private static CanBeCreatedFromDelegate? _canBeCreatedFrom;

        public TValue Value { get; }

        public static bool TryFrom(
            TValue value,
            [NotNullWhen(true)] out TThis? newInstance)
        {
            return TryFrom(value, out newInstance, out _);
        }

        public static bool TryFrom(
            TValue value,
            [NotNullWhen(true)] out TThis? newInstance,
            [NotNullWhen(false)] out string? errorDescription)
        {
            if (!CanBeCreatedFrom(value, out errorDescription))
            {
                newInstance = default;
                return false;
            }

            newInstance = CreateNewInstance(value);

            return true;
        }

        public static TThis From(TValue value)
        {
            static string EscapeIfNull<T>(T value) => $"{(value is not null ? $"[{value}]" : "<NULL>")}";

            if (!TryFrom(value, out TThis? newInstance, out string? errorDescription))
            {
                throw new ArgumentException($"Can't create an instance of {typeof(TThis).FullName} type " +
                    $"from value {EscapeIfNull(value)} - validation failed with error: {EscapeIfNull(errorDescription)}");
            }

            return newInstance;
        }

        private static bool CanBeCreatedFrom(TValue value, [NotNullWhen(false)] out string? errorDescription)
        {
            if (_canBeCreatedFrom is not null)
            {
                return _canBeCreatedFrom(value, out errorDescription);
            }

            MethodInfo[] methods = typeof(TThis).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);

            // First look for the extended version
            _canBeCreatedFrom = TryCreateExtendedValidator(methods);

            if (_canBeCreatedFrom is not null)
            {
                return _canBeCreatedFrom(value, out errorDescription);
            }

            // then for the shorthand one
            _canBeCreatedFrom = TryCreateShortValidator(methods);

            if (_canBeCreatedFrom is not null)
            {
                return _canBeCreatedFrom(value, out errorDescription);
            }

            // default
            _canBeCreatedFrom = static (TValue _, out string? errorDescription) =>
            {
                errorDescription = null;
                return true;
            };

            return _canBeCreatedFrom(value, out errorDescription);
        }

        private static CanBeCreatedFromDelegate? TryCreateExtendedValidator(MethodInfo[] methods)
        {
            var validator =
                TryCreateValidator<CanBeCreatedFromDelegate>(
                    methods,
                    new[] { typeof(TValue), typeof(string).MakeByRefType() },
                    new[] { "value", "errorDescription" }
                );

            return validator;
        }

        private static CanBeCreatedFromDelegate? TryCreateShortValidator(MethodInfo[] methods)
        {
            CanBeCreatedFromShortDelegate? shortValidator =
                TryCreateValidator<CanBeCreatedFromShortDelegate>(
                    methods,
                    new[] { typeof(TValue) },
                    new[] { "value" }
                );

            if (shortValidator is null)
            {
                return null;
            }

            CanBeCreatedFromDelegate validator = (TValue value, out string? errorDescripton) =>
            {
                errorDescripton = shortValidator(value) ? null : "<NOT SPECIFIED>";

                return errorDescripton is null;
            };

            return validator;
        }

        private static TValidator? TryCreateValidator<TValidator>(MethodInfo[] methods, Type[] signature, string[] parameterNames)
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

            var validator = CreateValidator<TValidator>(validationMethod, signature, parameterNames);

            return validator;
        }

        private static TValidator CreateValidator<TValidator>(MethodInfo validationMethod, Type[] signature, string[] parameterNames)
            where TValidator : Delegate
        {
            // TODO: .Zip?
            var parameters = new ParameterExpression[signature.Length];

            for (int i = 0; i < signature.Length; i++)
            {
                parameters[i] = Expression.Parameter(signature[i], parameterNames[i]);
            }

            MethodCallExpression call = Expression.Call(validationMethod, parameters);

            try
            {
                LambdaExpression canBeCreatedFromLambda = Expression.Lambda<TValidator>(call, parameters);
                var validator = (TValidator)canBeCreatedFromLambda.Compile();

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

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ValidatorAttribute : Attribute
    {
    }

    public class ValueCreationException : Exception
    {
        public ValueCreationException(string message, Exception? inner = null) : base(message, inner)
        {
        }
    }
}
