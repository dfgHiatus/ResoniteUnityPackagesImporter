using System;
using System.IO;
using System.Diagnostics;

namespace UnityPackageImporter.Extractor
{
    public class UnityPackageExtractor
    {
        private readonly static string extractor64Bit = Path.Combine("nml_mods", "unityPackageExtractor", "extractor64.exe");
        private readonly static string extractor86Bit = Path.Combine("nml_mods", "unityPackageExtractor", "extractor86.exe");

        public void Unpack(string pathToPackage, string outputPath)
        {
            var process = new Process();
            var fileName = Environment.Is64BitOperatingSystem ? extractor64Bit : extractor86Bit;
            process.StartInfo.FileName = fileName;
            var args = string.Join(" ", new string[] { pathToPackage, outputPath });
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();
        }
    }

}
