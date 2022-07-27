using System.IO;
using System.IO.Compression;

namespace UnityPackageImporter.Extractor
{
    public class UnityPackageExtractor
    {
        public void Unpack(string input, string outputDir)
        {
            var a = File.OpenRead(input);
            var b = new GZipStream(a, CompressionMode.Decompress);
            var temp = Path.Combine(outputDir, "temp");
            while (true)
            {
                var tarEntry = TarTarSource.ReadTarEntry(b);
                if (string.IsNullOrEmpty(tarEntry.Name))
                    break;
                
                string dirPath = Path.Combine(temp, tarEntry.Name);
                // If the data length is 0, this is a directory entry
                if (tarEntry.Data.Length == 0)
                {
                    // Create new dir if it doesn't exist
                    if (!Directory.Exists(dirPath))
                        Directory.CreateDirectory(dirPath);
                }
                else
                    File.WriteAllBytes(dirPath, tarEntry.Data);
            }
            
            b.Close();
            a.Close();
            foreach (var dir in Directory.GetDirectories(temp)) // Get all dirs in extracted file
            {
                var assetPath = Path.Combine(dir, "asset");
                var pathName = Path.Combine(dir, "pathname");
                if (!File.Exists(assetPath) || !File.Exists(pathName)) continue;
                var fileName = Path.GetFileName(File.ReadAllLines(pathName)[0]);
                var outFile = Path.Combine(outputDir, fileName);
                if (!File.Exists(outFile)) File.Copy(assetPath, outFile);
            }
            Directory.Delete(temp, true);
        }
    }
}
