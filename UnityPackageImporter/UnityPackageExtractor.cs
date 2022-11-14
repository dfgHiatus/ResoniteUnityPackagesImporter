using BaseX;
using System;
using System.IO;
using System.IO.Compression;

namespace UnityPackageImporter.Extractor
{
    public class UnityPackageExtractor
    {
        private static readonly char[] TheAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

        public static void Unpack(string input, string outputDir)
        {
            // Fun fact! Unity packages are actually just tar.gz files with a different extension
            var a = File.OpenRead(input);
            var b = new GZipStream(a, CompressionMode.Decompress);

            try
            {
                // Make a temp folder to house the extracted files
                var temp = Path.Combine(outputDir, "temp");
                while (true)
                {
                    var tarEntry = TarTarSource.ReadTarEntry(b);
                    if (string.IsNullOrEmpty(tarEntry.Name))
                        break;

                    var dirPath = Path.Combine(temp, tarEntry.Name);

                    // If the data length is 0, this is a directory entry
                    if (tarEntry.Data.Length == 0) 
                    {
                        if (!Directory.Exists(dirPath))
                            Directory.CreateDirectory(dirPath);
                    }
                    else
                        File.WriteAllBytes(dirPath, tarEntry.Data);
                }

                foreach (var dir in Directory.GetDirectories(temp))
                {
                    // Knowing this isn't a directory entry, we can treat this as an asset (with an optional path)
                    var assetPath = Path.Combine(dir, "asset");
                    var rawPathName = Path.Combine(dir, "pathname");
                    if (!File.Exists(assetPath) || !File.Exists(rawPathName)) continue;

                    // This asset can have a lot of spaces AND other unreadable characters appended. As such, trimming is not enough
                    // To get around this, we look for the last character (IE in the file extension)
                    var rawText = File.ReadAllText(rawPathName);
                    var lastIndex = rawText.LastIndexOfAny(TheAlphabet);
                    var pathName = rawText.Substring(0, lastIndex + 1);

                    // Now we can get the path and name of the asset. 
                    var fileName = Path.GetFileName(pathName);
                    var outFile = Path.Combine(outputDir, fileName);
                    if (!File.Exists(outFile)) File.Copy(assetPath, outFile);
                }
                Directory.Delete(temp, true);
            }
            catch (Exception e)
            {
                UniLog.Error(e.Message);
            }
            finally
            {
                b.Close();
                a.Close();
            }
        }
    }
}
