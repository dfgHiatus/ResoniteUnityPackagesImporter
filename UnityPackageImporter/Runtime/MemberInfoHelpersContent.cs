// NOTE: This file will be included in the receiver project as source code, so we disable nullable warning context, because nullable behavior is changing too frequently
// between consecutive .NET versions, and we do not want this code to fail at compile time due to nullable problems.
// Also, we should avoid using the newest C# language features (like "file-scoped namespace"), unless they are necessary or extremely useful.

#if !IS_INSIDE_PUBLICIZER
#nullable enable annotations
#nullable disable warnings
#endif

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Publicizer.Runtime
{
    internal static class MemberInfoHelpersContent
    {
        public static TDelegate CreateSetActionByEmittingIL<TDelegate>(FieldInfo fieldInfo)
            where TDelegate : Delegate
        {
            // NOTE: this method works for setting readonly fields too

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicMethodAssembly"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            // Set restrictedSkipVisibility to true to avoid any pesky "visibility" checks being made (in other
            // words, let the IL in the generated method access any private types or members that it tries to)
            var method = new DynamicMethod(
                name: "SetField",
                returnType: null,
                parameterTypes: fieldInfo.IsStatic
                    ? new Type[] { fieldInfo.FieldType }
                    : new Type[] { fieldInfo.ReflectedType, fieldInfo.FieldType },
                moduleBuilder,
                skipVisibility: true
            );

            var gen = method.GetILGenerator();
            if (fieldInfo.IsStatic)
            {
                gen.Emit(OpCodes.Ldnull);
                gen.Emit(OpCodes.Ldarg_0);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg_1);
            }
            gen.Emit(OpCodes.Stfld, fieldInfo);
            gen.Emit(OpCodes.Ret);

            return (TDelegate)method.CreateDelegate(typeof(TDelegate));
        }
    }
}
