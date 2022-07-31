using BaseX;
using System.IO;
using System.IO.Compression;

namespace UnityPackageImporter.Extractor
{
    public class UnityPackageExtractor
    {
        private static readonly char[] theAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

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
                if (tarEntry.Data.Length == 0) // If the data length is 0, this is a directory entry
                {
                    if (!Directory.Exists(dirPath))
                        Directory.CreateDirectory(dirPath);
                }
                else
                    File.WriteAllBytes(dirPath, tarEntry.Data);
            }
            
            b.Close();
            a.Close();
            foreach (var dir in Directory.GetDirectories(temp))
            {
                var assetPath = Path.Combine(dir, "asset");
                var rawPathName = Path.Combine(dir, "pathname");
                if (!File.Exists(assetPath) || !File.Exists(rawPathName)) continue;

                // Essentially, we are given a string with a lot of added spaces that represents a path to an asset
                // The easiest thing to do is Trim() the string (which worked for v1.0.0 and v1.1.0?!), but for some
                // eldritch reason it no longer wants to remove trailing spaces. I love this language.
                var rawText = File.ReadAllText(rawPathName);
                var lastIndex = rawText.LastIndexOfAny(theAlphabet);
                var pathName = rawText.Substring(0, lastIndex + 1);

                var fileName = Path.GetFileName(pathName); 
                var outFile = Path.Combine(outputDir, fileName);
                if (!File.Exists(outFile)) File.Copy(assetPath, outFile);
            }
            Directory.Delete(temp, true);
        }
    }
}
