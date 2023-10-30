// NOTE: This file will be included in the receiver project as source code, so we disable nullable warning context, because nullable behavior is changing too frequently
// between consecutive .NET versions, and we do not want this code to fail at compile time due to nullable problems.
// Also, we should avoid using the newest C# language features (like "file-scoped namespace"), unless they are necessary or extremely useful.

#if !IS_INSIDE_PUBLICIZER
#nullable enable annotations
#nullable disable warnings
#endif

using System;
using System.Reflection;

namespace Publicizer.Annotation
{
    /// <summary>
    /// Lifetime of members inside a type.
    /// </summary>
    [Flags]
    internal enum MemberLifetime
    {
        /// <summary>
        /// Static lifetime.
        /// </summary>
        Static = 1 << 1,

        /// <summary>
        /// Instance lifetime.
        /// </summary>
        Instance = 1 << 2,

        /// <summary>
        /// Represents all lifetimes.
        /// </summary>
        All = Static | Instance
    }

    /// <summary>
    /// Visibility of members inside a type.
    /// </summary>
    [Flags]
    internal enum MemberVisibility
    {
        /// <summary>
        /// Public visibility.
        /// </summary>
        Public = 1 << 1,

        /// <summary>
        /// Non-public visibility (e.g. private, internal, etc.).
        /// </summary>
        NonPublic = 1 << 2,

        /// <summary>
        /// Represents all visibilities.
        /// </summary>
        All = Public | NonPublic
    }

    /// <summary>
    /// Handling of accessors for fields (readonly vs. read/write) and for properties (<c>get</c> and <c>set</c> accessors).
    /// </summary>
    [Flags]
    internal enum AccessorHandling
    {
        /// <summary>
        /// Keep everything as in the original class.
        /// </summary>
        KeepOriginal = 0,

        /// <summary>
        /// Readonly fields and readonly auto-implemented properties will be writable through the proxy.
        /// </summary>
        ForceWriteOnReadonly = 1 << 1,

        /// <summary>
        /// Writeonly auto-implemented properties will be readable through the proxy.
        /// </summary>
        ForceReadOnWriteonly = 1 << 2,

        /// <summary>
        /// Combination of <see cref="ForceWriteOnReadonly"/> and <see cref="ForceReadOnWriteonly"/>.
        /// </summary>
        ForceReadAndWrite = ForceWriteOnReadonly | ForceReadOnWriteonly,
    }

    internal static class MemberTraitsConverterExtensions
    {
        public static BindingFlags ToBindingFlags(this MemberLifetime memberLifetime)
        {
            var bindingFlags = default(BindingFlags);

            if (memberLifetime.HasFlag(MemberLifetime.Static))
                bindingFlags |= BindingFlags.Static;

            if (memberLifetime.HasFlag(MemberLifetime.Instance))
                bindingFlags |= BindingFlags.Instance;

            return bindingFlags;
        }

        public static BindingFlags ToBindingFlags(this MemberVisibility memberVisibility)
        {
            var bindingFlags = default(BindingFlags);

            if (memberVisibility.HasFlag(MemberVisibility.Public))
                bindingFlags |= BindingFlags.Public;

            if (memberVisibility.HasFlag(MemberVisibility.NonPublic))
                bindingFlags |= BindingFlags.NonPublic;

            return bindingFlags;
        }
    }
}