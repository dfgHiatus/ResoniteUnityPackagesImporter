// NOTE: This file will be included in the receiver project as source code, so we disable nullable warning context, because nullable behavior is changing too frequently
// between consecutive .NET versions, and we do not want this code to fail at compile time due to nullable problems.
// Also, we should avoid using the newest C# language features (like "file-scoped namespace"), unless they are necessary or extremely useful.

#if !IS_INSIDE_PUBLICIZER
#nullable enable annotations
#nullable disable warnings
#endif

using System.Reflection;

namespace Publicizer.Runtime
{
    /// <summary>
    /// Member accessor logic which uses reflection.
    /// </summary>
    /// <remarks>
    /// It can be used by the forwarding code of the generated public members inside the proxy class to access the (typically private) members
    /// of the original class.
    /// </remarks>
    internal class ReflectionMemberAccessor : IMemberAccessor
    {
        /// <inheritdoc />
        public object? GetValue(FieldInfo field, object? instance) => field.GetValue(instance);

        /// <inheritdoc />
        public void SetValue(FieldInfo field, object? instance, object? value) => field.SetValue(instance, value);

        /// <inheritdoc />
        public object? GetValue(PropertyInfo property, object? instance) => property.GetValue(instance);

        /// <inheritdoc />
        public void SetValue(PropertyInfo property, object? instance, object? value) => property.SetValue(instance, value);

        /// <inheritdoc />
        public object? InvokeMethod(MethodInfo method, object? instance, object?[] parameterValues) => method.Invoke(instance, parameterValues);
    }
}
