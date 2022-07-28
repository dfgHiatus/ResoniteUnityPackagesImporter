using BaseX;
using CodeX;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityPackageImporter.Extractor;

namespace UnityPackageImporter
{
    public class UnityPackageImporter : NeosMod
    {
        public override string Name => "UnityPackageImporter";
        public override string Author => "dfgHiatus, eia485, delta, Frozenreflex";
        public override string Version => "1.1.0";
        public override string Link => "https://github.com/dfgHiatus/NeosUnityPackagesImporter";

        public static ModConfiguration Config;
        private static string _cachePath = Path.Combine(Engine.Current.CachePath, "Cache", "DecompressedUnityPackages");
        private static UnityPackageExtractor _extractor = new();

        public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
        {
            builder
                .Version(new Version(1, 1, 0))
                .AutoSave(true); 
        }

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> importAsRawFiles = 
            new("importAsRawFiles",
            "Import files directly into Neos. Unity Packages can be very large, keep this true unless you know what you're doing!",
            () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> importText = 
            new("importText", "Import Text", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> importTexture = 
            new("importTexture", "Import Textures", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> importDocument = 
            new("importDocument", "Import Documents", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> importMesh = 
            new("importMesh", "Import Mesh", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> importPointCloud =
            new("importPointCloud", "Import Point Clouds", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> importAudio = 
            new("importAudio", "Import Audio", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> importFont = 
            new("importFont", "Import Fonts", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> importVideo = 
            new("importVideo", "Import Videos", () => true);

        public override void OnEngineInit()
        {
            new Harmony("net.dfgHiatus.UnityPackageImporter").PatchAll();
            Config = GetConfiguration();
            Directory.CreateDirectory(_cachePath);
        }
        public static string[] DecomposeUnityPackages(string[] files)
        {
            var fileToHash = files.ToDictionary(file => file, GenerateMD5);
            HashSet<string> dirsToImport = new();
            HashSet<string> unityPackagesToDecompress = new();
            foreach (var element in fileToHash)
            {
                var dir = Path.Combine(_cachePath, element.Value);
                if (!Directory.Exists(dir))
                    unityPackagesToDecompress.Add(element.Key);
                else
                    dirsToImport.Add(dir);
            }
            foreach (var package in unityPackagesToDecompress)
            {
                var modelName = Path.GetFileNameWithoutExtension(package);
                if (ContainsUnicodeCharacter(modelName))
                {
                    Error("Imported unity package cannot have unicode characters in its file name.");
                    continue;
                }
                var extractedPath = Path.Combine(_cachePath, fileToHash[package]);
                _extractor.Unpack(package, extractedPath);
                dirsToImport.Add(extractedPath);
            }
            return dirsToImport.ToArray();
        }

        private static bool ContainsUnicodeCharacter(string input)
        {
            const int MaxAnsiCode = 255;
            return input.Any(c => c > MaxAnsiCode);
        }

        [HarmonyPatch(typeof(UniversalImporter), "Import", typeof(AssetClass), typeof(IEnumerable<string>),
            typeof(World), typeof(float3), typeof(floatQ), typeof(bool))]
        class UniversalImporterPatch
        {
            static bool Prefix(ref IEnumerable<string> files)
            {
                Msg("file size " + files.Count());
                List<string> hasUnityPackage = new();
                List<string> notUnityPackage = new();
                foreach (var file in files)
                {
                    if (Path.GetExtension(file).ToLower().Equals(".unitypackage"))
                        hasUnityPackage.Add(file);
                    else
                        notUnityPackage.Add(file);
                }
                
                List<string> allDirectoriesToBatchImport = new();
                foreach (var dir in DecomposeUnityPackages(hasUnityPackage.ToArray()))
                    allDirectoriesToBatchImport.AddRange(Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
                        .Where(ShouldImportFile).ToArray());

                var slot = Engine.Current.WorldManager.FocusedWorld.AddSlot("Unity Package import");
                slot.PositionInFrontOfUser();
                BatchFolderImporter.BatchImport(slot, allDirectoriesToBatchImport, Config.GetValue(importAsRawFiles));

                if (notUnityPackage.Count <= 0) return false;
                files = notUnityPackage.ToArray();
                return true;
            }
        }

        private static bool ShouldImportFile(string file)
        {
            var assetClass = AssetHelper.ClassifyExtension(Path.GetExtension(file));
            return (Config.GetValue(importText) && assetClass == AssetClass.Text) ||
            (Config.GetValue(importTexture) && assetClass == AssetClass.Texture) ||
            (Config.GetValue(importDocument) && assetClass == AssetClass.Document) ||
            (Config.GetValue(importMesh) && assetClass == AssetClass.Model) ||
            (Config.GetValue(importPointCloud) && assetClass == AssetClass.PointCloud) ||
            (Config.GetValue(importAudio) && assetClass == AssetClass.Audio) ||
            (Config.GetValue(importFont) && assetClass == AssetClass.Font) ||
            (Config.GetValue(importVideo) && assetClass == AssetClass.Video);
        }

        //credit to delta for this method https://github.com/XDelta/
        private static string GenerateMD5(string filepath)
        {
            using var hasher = MD5.Create();
            using var stream = File.OpenRead(filepath);
            var hash = hasher.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}
