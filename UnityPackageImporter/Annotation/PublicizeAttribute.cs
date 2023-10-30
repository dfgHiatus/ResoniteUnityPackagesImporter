// NOTE: This file will be included in the receiver project as source code, so we disable nullable warning context, because nullable behavior is changing too frequently
// between consecutive .NET versions, and we do not want this code to fail at compile time due to nullable problems.
// Also, we should avoid using the newest C# language features (like "file-scoped namespace"), unless they are necessary or extremely useful.

#if !IS_INSIDE_PUBLICIZER
#nullable enable annotations
#nullable disable warnings
#endif

using System;
using Publicizer.Runtime;

namespace Publicizer.Annotation
{
    /// <summary>
    /// Generates public members into the decorated proxy class which forward to the private members of <see cref="TypeToPublicize"/>.
    /// This way the private members of <see cref="TypeToPublicize"/> can be accessed with compile-time safety.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    internal class PublicizeAttribute : Attribute
    {
        internal const MemberLifetime DefaultMemberLifetime = MemberLifetime.All;
        internal const MemberVisibility DefaultMemberVisibility = MemberVisibility.All;
        internal const AccessorHandling DefaultAccessorHandling = AccessorHandling.KeepOriginal;

        /// <summary>
        /// The type of which private members need to be accessed.
        /// </summary>
        public Type TypeToPublicize { get; }

        /// <summary>
        /// The lifetime of the members which needs to be generated and forwarded.
        /// </summary>
        public MemberLifetime MemberLifetime { get; set; } = DefaultMemberLifetime;

        /// <summary>
        /// The visibility of the members which needs to be generated and forwarded.
        /// </summary>
        public MemberVisibility MemberVisibility { get; set; } = DefaultMemberVisibility;

        /// <summary>
        /// The handling of generated accessors for fields (readonly vs. read/write) and for properties (<c>get</c> and <c>set</c> accessors).
        /// </summary>
        public AccessorHandling AccessorHandling { get; set; } = DefaultAccessorHandling;

        /// <summary>
        /// Optional member accessor type which implements <see cref="IMemberAccessor"/>.
        /// If missing, then the default compiled expression tree generation will be used, which is very fast.
        /// </summary>
        public Type? CustomMemberAccessorType { get; set; }

        /// <summary>
        /// Generates public members into the decorated proxy class which forward to the private members of <paramref name="typeToPublicize"/>.
        /// </summary>
        /// <param name="typeToPublicize">The type of which private members need to be accessed.</param>
        public PublicizeAttribute(Type typeToPublicize)
        {
            TypeToPublicize = typeToPublicize;
        }
    }
}