using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ValueExtensions
{
    public record ValueOf<TValue, TThis>
            where TThis : notnull, ValueOf<TValue, TThis>
            where TValue : notnull
    {
        private static Func<TValue, TThis>? _newInstance;

        private static Func<TValue, bool>? _canBeCreatedFrom;

        protected ValueOf(TValue value)
        {
            Value = value;
        }

        public TValue Value { get; init; }

        public static bool TryCreateFrom(TValue value, [NotNullWhen(true)] out TThis? newInstance)
        {
            newInstance = CanBeCreatedFrom(value) ? CreateNewInstance(value) : null;

            return newInstance is not null;
        }

        [return: NotNull]
        public static TThis From(TValue value)
        {
            if (!CanBeCreatedFrom(value))
            {
                throw new ArgumentException($"Can't create an instance of {typeof(TThis).FullName} type from value '{value}' - validation failed.");
            }

            return CreateNewInstance(value);
        }

        private static bool CanBeCreatedFrom(TValue value)
        {
            if (_canBeCreatedFrom is null)
            {
                MethodInfo? validationMethod = typeof(TThis).GetMethod("CanBeCreatedFrom",
                    BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(TValue) }, null);

                if (validationMethod == null)
                {
                    MethodInfo[] validators =
                        typeof(TThis)
                            .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
                            .Where(m => m.GetCustomAttribute<ValidationMethodAttribute>() is not null)
                            .ToArray();

                    if (validators.Length == 0)
                    {
                        _canBeCreatedFrom = static _ => true;
                        
                        return true;
                    }

                    if (validators.Length > 1)
                    {
                        throw new ValueCreationException($"More than one validation method found for target type '{typeof(TThis).FullName}'.");
                    }

                    validationMethod = validators[0];
                }

                ParameterExpression methodParameter = Expression.Parameter(typeof(TValue), "value");
                MethodCallExpression callExp = Expression.Call(validationMethod, methodParameter);

                try
                {
                    LambdaExpression canBeCreatedFromLambda = Expression.Lambda<Func<TValue, bool>>(callExp, methodParameter);
                    _canBeCreatedFrom = (Func<TValue, bool>)canBeCreatedFromLambda.Compile();
                }
                catch (Exception reason)
                {
                    throw ValueCreationException(reason);
                }
            }

            bool result = _canBeCreatedFrom(value);

            return result;
        }

        private static TThis CreateNewInstance(TValue value)
        {
            if (_newInstance is null)
            {
                ConstructorInfo? ctor = typeof(TThis).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance,
                    null, new[] { typeof(TValue) }, null);

                if (ctor is null)
                {
                    throw new ValueCreationException($"Constructor with argument of type '{typeof(TValue).FullName}'" +
                        $"not found for target type '{typeof(TThis).FullName}'.");
                }

                ParameterExpression constructorParameter = Expression.Parameter(typeof(TValue), "value");
                NewExpression newExp = Expression.New(ctor, constructorParameter);

                try
                {
                    LambdaExpression newInsanceLambda = Expression.Lambda<Func<TValue, TThis>>(newExp, constructorParameter);
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
    public class ValidationMethodAttribute : Attribute
    {
    }

    public class ValueCreationException : Exception
    {
        public ValueCreationException(string message, Exception? inner = null) : base(message, inner)
        {
        }
    }
}

