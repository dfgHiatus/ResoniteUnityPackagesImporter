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
    /// The member accessor logic to access the members of a type during runtime.
    /// </summary>
    /// <remarks>
    /// It is used by the forwarding code of the generated public members inside the proxy class to access the (typically private) members of the original class.
    /// </remarks>
    internal interface IMemberAccessor
    {
        /// <summary>
        /// Gets the value of a field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="instance">The instance which contains <paramref name="field"/>. It is <c>null</c> if the field is static.</param>
        /// <returns>The value of the <paramref name="field"/>.</returns>
        object? GetValue(FieldInfo field, object? instance);

        /// <summary>
        /// Sets the value of a field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="instance">The instance which contains <paramref name="field"/>. It is <c>null</c> if the field is static.</param>
        /// <param name="value">The new value of <paramref name="field"/>.</param>
        void SetValue(FieldInfo field, object? instance, object? value);

        /// <summary>
        /// Gets the value of a property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="instance">The instance which contains <paramref name="property"/>. It is <c>null</c> if the property is static.</param>
        /// <returns>The value of the <paramref name="property"/>.</returns>
        object? GetValue(PropertyInfo property, object? instance);

        /// <summary>
        /// Sets the value of a property.
        /// </summary>
        /// <param name="property">The field.</param>
        /// <param name="instance">The instance which contains <paramref name="property"/>. It is <c>null</c> if the property is static.</param>
        /// <param name="value">The new value of <paramref name="property"/>.</param>
        void SetValue(PropertyInfo property, object? instance, object? value);

        /// <summary>
        /// Invokes a method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="instance">The instance which contains <paramref name="method"/>. It is <c>null</c> if the method is static.</param>
        /// <param name="parameterValues">The values of the parameters of <paramref name="method"/>.</param>
        /// <returns></returns>
        object? InvokeMethod(MethodInfo method, object? instance, object?[] parameterValues);
    }
}
