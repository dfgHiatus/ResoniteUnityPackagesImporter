using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace UnityPackageImporter;

internal class Utils
{
    internal static bool ContainsUnicodeCharacter(string input)
    {
        const int MaxAnsiCode = 255;
        return input.Any(c => c > MaxAnsiCode);
    }

    // Credit to delta for this method https://github.com/XDelta/
    internal static string GenerateMD5(string filepath)
    {
        var hasher = MD5.Create();
        var stream = File.OpenRead(filepath);
        var hash = hasher.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "");
    }
}
