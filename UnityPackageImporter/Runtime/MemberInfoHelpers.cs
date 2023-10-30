// NOTE: This file will be included in the receiver project as source code, so we disable nullable warning context, because nullable behavior is changing too frequently
// between consecutive .NET versions, and we do not want this code to fail at compile time due to nullable problems.
// Also, we should avoid using the newest C# language features (like "file-scoped namespace"), unless they are necessary or extremely useful.

#if !IS_INSIDE_PUBLICIZER
#nullable enable annotations
#nullable disable warnings
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Publicizer.Runtime
{
    internal static class MemberInfoHelpers
    {
        public static IEnumerable<TEnum> GetFlagsValues<TEnum>(this TEnum enumValue)
            where TEnum : Enum =>
            typeof(TEnum)
                .GetEnumValues()
                .Cast<TEnum>()
                .Where(primitiveEnumValue => enumValue.HasFlag(primitiveEnumValue));

        public static bool IsStatic(this PropertyInfo property) => property.GetAccessors(nonPublic: true)[0].IsStatic;

        public static TDelegate CreateGetFuncByExpression<TDelegate>(FieldInfo fieldInfo)
            where TDelegate : Delegate
        {
            Expression body;
            ParameterExpression[] parameters;

            if (fieldInfo.IsStatic)
            {
                body = Expression.Field(null, fieldInfo);
                parameters = Array.Empty<ParameterExpression>();
            }
            else
            {
                var instance = Expression.Parameter(fieldInfo.ReflectedType, "instance");
                body = Expression.Field(Expression.Convert(instance, fieldInfo.DeclaringType), fieldInfo);
                parameters = new[] { instance };
            }

            return Expression.Lambda<TDelegate>(body, parameters).Compile();
        }

        public static TDelegate CreateGetFuncByExpression<TDelegate>(PropertyInfo propertyInfo)
            where TDelegate : Delegate
        {
            if (!propertyInfo.CanRead)
                throw new ArgumentException($"The property '{propertyInfo.DeclaringType}.{propertyInfo.Name}' has not getter", nameof(propertyInfo));

            Expression body;
            ParameterExpression[] parameters;

            if (propertyInfo.IsStatic())
            {
                body = Expression.Property(null, propertyInfo);
                parameters = Array.Empty<ParameterExpression>();
            }
            else
            {
                var instance = Expression.Parameter(propertyInfo.ReflectedType, "instance");
                body = Expression.Property(Expression.Convert(instance, propertyInfo.DeclaringType), propertyInfo);
                parameters = new[] { instance };
            }

            return Expression.Lambda<TDelegate>(body, parameters).Compile();
        }

        public static TDelegate CreateSetActionByExpression<TDelegate>(FieldInfo fieldInfo)
            where TDelegate : Delegate
        {
            if (fieldInfo.IsInitOnly)
                throw new ArgumentException($"The field '{fieldInfo.DeclaringType}.{fieldInfo.Name}' is readonly", nameof(fieldInfo));

            Expression body;
            ParameterExpression[] parameters;

            if (fieldInfo.IsStatic)
            {
                var value = Expression.Parameter(fieldInfo.FieldType, "value");

                body = Expression.Assign(
                    Expression.Field(null, fieldInfo),
                    Expression.Convert(value, fieldInfo.FieldType)
                );

                parameters = new[] { value };
            }
            else
            {
                var instance = Expression.Parameter(fieldInfo.ReflectedType, "instance");
                var value = Expression.Parameter(fieldInfo.FieldType, "value");

                body = Expression.Assign(
                    Expression.Field(Expression.Convert(instance, fieldInfo.DeclaringType), fieldInfo),
                    Expression.Convert(value, fieldInfo.FieldType)
                );

                parameters = new[] { instance, value };
            }

            return Expression.Lambda<TDelegate>(body, parameters).Compile();
        }

        public static TDelegate CreateSetActionByExpression<TDelegate>(PropertyInfo propertyInfo)
            where TDelegate : Delegate
        {
            if (!propertyInfo.CanWrite)
            {
                throw new ArgumentException($"The property '{propertyInfo.DeclaringType}.{propertyInfo.Name}' has not setter", nameof(propertyInfo));
            }

            Expression body;
            ParameterExpression[] parameters;

            if (propertyInfo.IsStatic())
            {
                var value = Expression.Parameter(propertyInfo.PropertyType, "value");

                body = Expression.Assign(
                    Expression.Property(null, propertyInfo),
                    Expression.Convert(value, propertyInfo.PropertyType)
                );

                parameters = new[] { value };
            }
            else
            {
                var instance = Expression.Parameter(propertyInfo.ReflectedType, "instance");
                var value = Expression.Parameter(propertyInfo.PropertyType, "value");

                body = Expression.Assign(
                    Expression.Property(Expression.Convert(instance, propertyInfo.DeclaringType), propertyInfo),
                    Expression.Convert(value, propertyInfo.PropertyType)
                );

                parameters = new[] { instance, value };
            }

            return Expression.Lambda<TDelegate>(body, parameters).Compile();
        }

        public static TDelegate CreateInvokeByExpression<TDelegate>(MethodInfo methodInfo)
            where TDelegate : Delegate
        {
            Expression body;
            ParameterExpression[] parameters;

            if (methodInfo.IsStatic)
            {
                parameters = methodInfo
                    .GetParameters()
                    .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
                    .ToArray();

                body = Expression.Call(methodInfo, parameters);
            }
            else
            {
                var instance = Expression.Parameter(methodInfo.ReflectedType, "instance");

                var methodParameters = methodInfo
                    .GetParameters()
                    .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
                    .ToArray();

                body = Expression.Call(
                    Expression.Convert(instance, methodInfo.DeclaringType),
                    methodInfo,
                    methodParameters
                );

                parameters = methodParameters.Prepend(instance).ToArray();
            }

            return typeof(TDelegate) == typeof(Delegate)
                ? (TDelegate)Expression.Lambda(body, parameters).Compile()
                : Expression.Lambda<TDelegate>(body, parameters).Compile();
        }
    }
}
