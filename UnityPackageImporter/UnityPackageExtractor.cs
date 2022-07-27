using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace UnityPackageImporter.Extractor
{
    public class UnityPackageExtractor
    {
        public void Unpack(string pathToPackage, string outputPath)
        {
            var a = File.OpenRead(pathToPackage);
            var b = new GZipInputStream(a);
            var c = TarArchive.CreateInputTarArchive(b);
            var temp = Path.Combine(outputPath, "temp");
            c.ExtractContents(temp);
            c.Close();
            b.Close();
            a.Close();
            foreach (var dir in Directory.GetDirectories(temp))
            {
                var assetPath = Path.Combine(dir, "asset");
                var pathName = Path.Combine(dir, "pathname");
                if (!File.Exists(assetPath) || !File.Exists(pathName)) continue;
                var fileName = Path.GetFileName(File.ReadAllLines(pathName)[0]);
                var outFile = Path.Combine(outputPath, fileName);
                if (!File.Exists(outFile)) File.Copy(assetPath, outFile);
            }
            Directory.Delete(temp, true);
        }
    }

}
