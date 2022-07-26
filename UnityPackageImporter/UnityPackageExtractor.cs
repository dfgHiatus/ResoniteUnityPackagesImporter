using BaseX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityPackageImporter.Extractor
{
    public class UnityPackageExtractor
    {

        private readonly static string extractor64Bit = Path.Combine("nml_mods", "unityPackageExtractor", "extractor64.exe");
        private readonly static string extractor86Bit = Path.Combine("nml_mods", "unityPackageExtractor", "extractor86.exe");

        public static readonly List<string> invalidFileExtensions = new List<string>()
        {
            ".meta",
            ".asmdef",
            ".dll",
            ".mat",
        };

        public void Unpack(string pathToPackage, string outputPath)
        {
            var process = new Process();
            var fileName = Environment.Is64BitOperatingSystem ? extractor64Bit : extractor86Bit;
            process.StartInfo.FileName = fileName;
            var args = string.Join(" ", new string[] { pathToPackage, outputPath });
            process.StartInfo.Arguments = args;
            UniLog.Log($"{fileName} {args}");
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.Start();
            process.WaitForExit();
        }
    }

}
