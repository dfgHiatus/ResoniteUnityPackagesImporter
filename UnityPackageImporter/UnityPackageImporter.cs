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
            }

            HashSet<string> dirsToImport = new();
            HashSet<string> unityPackagesToDecompress = new();
            foreach (var element in fileToHash)
            {
                var dir = Path.Combine(CachePath, element.Value);
                if (!Directory.Exists(dir))
                {
                    unityPackagesToDecompress.Add(element.Key);
                }
                else
                {
                    dirsToImport.Add(dir);
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

                // Make a new directory for the extracted files under CachePath
                UnityPackageExtractor extractor = new UnityPackageExtractor();
                var extractedPath = Path.Combine(CachePath, fileToHash[package]);
                extractor.Unpack(package, extractedPath);


                // Delete all Files we don't want
                var extractedfiles = Directory.GetFiles(extractedPath);
                foreach (var file in extractedfiles)
                {
                    var fileExt = Path.GetExtension(file);
                    if (!UnityPackageExtractor.validFileExtensions.Contains(fileExt))
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
                string[] hasUnityPackage = new string[] { };
                string[] notUnityPackage = new string[] { };
                foreach (string file in files)
                {
                    if (Path.GetExtension(file).ToLower().Equals(".unitypackage"))
                    {
                        hasUnityPackage.AddItem(file);
                    }
                    else
                    {
                        notUnityPackage.AddItem(file);
                    }
                }

                string[] allFilesToBatchImport = new string[] { };
                foreach (string dir in DecomposeUnityPackages(hasUnityPackage))
                {
                    allFilesToBatchImport.AddRangeToArray(Directory.EnumerateFiles(dir).ToArray());
                }
                BatchFolderImporter.BatchImport(Engine.Current.WorldManager.FocusedWorld.AddSlot("Unity Package import"), allFilesToBatchImport);

                if (notUnityPackage.Length > 0)
                {
                    files = notUnityPackage.ToArray();
                    return true;
                }
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