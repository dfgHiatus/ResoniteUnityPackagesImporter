using BaseX;
using CodeX;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityPackageImporter.Extractor;

namespace UnityPackageImporter
{
    public class UnityPackageImporter : NeosMod
    {
        public override string Name => "UnityPackageImporter";
        public override string Author => "dfgHiatus, eia485, delta";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/dfgHiatus/NeosUnityPackagesImporter";

        public static ModConfiguration config;
        private static string CachePath = Path.Combine(Engine.Current.CachePath, "Cache", "DecompressedUnityPackages");
        private static UnityPackageExtractor extractor = new UnityPackageExtractor();

        public override void OnEngineInit()
        {
            new Harmony("net.dfgHiatus.UnityPackageImporter").PatchAll();
            config = GetConfiguration();
            Directory.CreateDirectory(CachePath);
        }
        public static string[] DecomposeUnityPackages(string[] files)
        {
            Dictionary<string, string> fileToHash = new();
            foreach (string file in files)
            {
                fileToHash.Add(file, GenerateMD5(file));
                Msg("hash file " + file);
                Msg("hash md5 " + GenerateMD5(file));
            }
            Msg("Hashed");

            HashSet<string> dirsToImport = new();
            HashSet<string> unityPackagesToDecompress = new();
            foreach (var element in fileToHash)
            {
                var dir = Path.Combine(CachePath, element.Value);
                if (!Directory.Exists(dir))
                {
                    unityPackagesToDecompress.Add(element.Key);
                    Msg("decompress " + element.Key);
                }
                else
                {
                    dirsToImport.Add(dir);
                    Msg("dirsToImport " + element.Key);
                }
            }

            foreach (var package in unityPackagesToDecompress)
            {
                var modelName = Path.GetFileNameWithoutExtension(package);
                if (ContainsUnicodeCharacter(modelName))
                {
                    Error("Imported unity package cannot have unicode characters in its file name.");
                    continue;
                }

                var extractedPath = Path.Combine(CachePath, fileToHash[package]);
                extractor.Unpack(package, extractedPath);
                Msg("Unpacked");

                // Delete all Files we don't want
                var extractedfiles = Directory.GetFiles(extractedPath);
                foreach (var file in extractedfiles)
                {
                    var fileExt = Path.GetExtension(file);
                    if (UnityPackageExtractor.invalidFileExtensions.Contains(fileExt))
                    {
                        File.Delete(file);
                    }
                }
                dirsToImport.Add(extractedPath);
            }
            return dirsToImport.ToArray();
        }

        private static bool ContainsUnicodeCharacter(string input)
        {
            const int MaxAnsiCode = 255;
            return input.Any(c => c > MaxAnsiCode);
        }

        [HarmonyPatch(typeof(UniversalImporter), "Import", new[] {typeof(AssetClass), typeof(IEnumerable<string>), typeof(World), typeof(float3), typeof(floatQ), typeof(bool)})]
        class UniversalImporterPatch
        {
            static bool Prefix(ref IEnumerable<string> files)
            {
                Msg("file size " + files.Count());
                List<string> hasUnityPackage = new();
                List<string> notUnityPackage = new();
                foreach (string file in files)
                {
                    if (Path.GetExtension(file).ToLower().Equals(".unitypackage"))
                    {
                        Msg($"hasUnityPackage.AddItem({file})");
                        hasUnityPackage.Add(file);
                    }
                    else
                    {
                        notUnityPackage.Add(file);
                        Msg($"notUnityPackage.AddItem({file})");
                    }
                }

                List<string> allDirectoriesToBatchImport = new ();

                Msg("length of unity files " + hasUnityPackage.Count);
                foreach (string dir in DecomposeUnityPackages(hasUnityPackage.ToArray()))
                {
                    Msg("dir " + dir);
                    allDirectoriesToBatchImport.AddRange(Directory.GetFiles(dir, "*", SearchOption.AllDirectories));
                }
                Msg("total files to import " + allDirectoriesToBatchImport.Count());
                foreach (var item in allDirectoriesToBatchImport)
                {
                    Msg("Unity file - " + item);
                }

                BatchFolderImporter.BatchImport(Engine.Current.WorldManager.FocusedWorld.AddSlot("Unity Package import"), allDirectoriesToBatchImport);
                Msg("Imported");

                if (notUnityPackage.Count > 0)
                {
                    files = notUnityPackage.ToArray();
                    Msg("notUnityPackage.Length > 0");
                    return true;
                }
                Msg("!notUnityPackage.Length > 0");
                return false;
            }
        }

        //credit to delta for this method https://github.com/XDelta/
        private static string GenerateMD5(string filepath)
        {
            using (var hasher = MD5.Create())
            {
                using (var stream = File.OpenRead(filepath))
                {
                    var hash = hasher.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }
    }
}