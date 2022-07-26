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

        public static readonly List<string> validFileExtensions = new List<string>()
        {
            ".jpeg",
            ".jpg",
            ".png",
            ".fbx"
        };

        public void Unpack(string pathToPackage, string outputPath)
        {
            var process = new Process();
            process.StartInfo.FileName = Environment.Is64BitOperatingSystem ? extractor64Bit : extractor86Bit;
            process.StartInfo.Arguments = string.Join(" ", new string[] { pathToPackage, outputPath });
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.OutputDataReceived += OnOutput;
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }

        private void OnOutput(object sender, DataReceivedEventArgs e)
        {
            UniLog.Log(e.Data);
        }
    }

}
