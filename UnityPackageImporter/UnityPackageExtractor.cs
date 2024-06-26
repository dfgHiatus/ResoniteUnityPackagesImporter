﻿using Elements.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace UnityPackageImporter.Extractor;

public class UnityPackageExtractor
{
    private static readonly char[] TheAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

    public static List<string> Unpack(string input, string outputDir)
    {
        // Fun fact! Unity packages are actually just tar.gz files with a different extension
        var a = File.OpenRead(input);
        var b = new GZipStream(a, CompressionMode.Decompress);
        var filenames = new List<string>();

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
                var metafile = Path.Combine(dir, "asset.meta");
                var rawPathName = Path.Combine(dir, "pathname");
                if (!File.Exists(assetPath) || !File.Exists(rawPathName)) continue;

                // This asset can have a lot of spaces AND other unreadable characters appended. As such, trimming is not enough
                // To get around this, we look for the last character (IE in the file extension)
                var rawText = File.ReadAllText(rawPathName);
                var lastIndex = rawText.LastIndexOfAny(TheAlphabet);
                var pathName = rawText.Substring(0, lastIndex + 1);

                // Now we can get the path and name of the asset. 
                // @989onan - Improved the temporary directory to respect file paths. So if the asset would go under Assets/mymodel/images/filename.png it would go there but prepended by outputDir.
                // Instead of a giant bucket of files in one folder. this will avoid file name conflicts. Probably while the file exists checks are here to begin with because of
                // That and user possibly having previous imports of the same package with the same contents according to the MD5 hash 
                var outPath = Path.Combine(outputDir, Path.GetDirectoryName(pathName));
                if (!Directory.Exists(outPath))
                    Directory.CreateDirectory(outPath);

                var outFile = Path.Combine(outPath, Path.GetFileName(pathName));

                File.Copy(assetPath, outFile, true);
                filenames.Add(outFile);

                if (UnityPackageImporter.Config.GetValue(UnityPackageImporter.ImportPrefab) && File.Exists(metafile))
                {
                    File.Copy(metafile, outFile + ".meta", true);
                    filenames.Add(outFile + ".meta");
                }
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
        return filenames;
    }
}
