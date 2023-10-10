using Elements.Core;
using Elements.Assets;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityPackageImporter.Extractor;

namespace UnityPackageImporter;

public class UnityPackageImporter : ResoniteMod
{
    public override string Name => "UnityPackageImporter";
    public override string Author => "dfgHiatus, eia485, delta, Frozenreflex, benaclejames";
    public override string Version => "2.0.0";
    public override string Link => "https://github.com/dfgHiatus/ResoniteUnityPackagesImporter";

    internal const string UNITY_PACKAGE_EXTENSION = ".unitypackage";
    private static ModConfiguration config;
    private static string cachePath = Path.Combine(Engine.Current.CachePath, "Cache", "DecompressedUnityPackages");

    [AutoRegisterConfigKey]
    private readonly static ModConfigurationKey<bool> importAsRawFiles = 
        new("importAsRawFiles",
        "Import files directly into Resonite. Unity Packages can be very large, keep this true unless you know what you're doing!",
        () => true);
    [AutoRegisterConfigKey]
    private readonly static ModConfigurationKey<bool> importText = 
        new("importText", "Import Text", () => true);
    [AutoRegisterConfigKey]
    private readonly static ModConfigurationKey<bool> importTexture = 
        new("importTexture", "Import Textures", () => true);
    [AutoRegisterConfigKey]
    private readonly static ModConfigurationKey<bool> importDocument = 
        new("importDocument", "Import Documents", () => true);
    [AutoRegisterConfigKey]
    private readonly static ModConfigurationKey<bool> importMesh = 
        new("importMesh", "Import Meshes", () => true);
    [AutoRegisterConfigKey]
    private readonly static ModConfigurationKey<bool> importPointCloud =
        new("importPointCloud", "Import Point Clouds", () => true);
    [AutoRegisterConfigKey]
    private readonly static ModConfigurationKey<bool> importAudio = 
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
        config = GetConfiguration();
        Directory.CreateDirectory(cachePath);
    }
    
    public static string[] DecomposeUnityPackages(string[] files)
    {
        var fileToHash = files.ToDictionary(file => file, GenerateMD5);
        HashSet<string> dirsToImport = new();
        HashSet<string> unityPackagesToDecompress = new();
        foreach (var element in fileToHash)
        {
            var dir = Path.Combine(cachePath, element.Value);
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
            var extractedPath = Path.Combine(cachePath, fileToHash[package]);
            UnityPackageExtractor.Unpack(package, extractedPath);
            dirsToImport.Add(extractedPath);
        }
        return dirsToImport.ToArray();
    }


    [HarmonyPatch(typeof(UniversalImporter), "Import", typeof(AssetClass), typeof(IEnumerable<string>),
        typeof(World), typeof(float3), typeof(floatQ), typeof(bool))]
    public class UniversalImporterPatch
    {
        static bool Prefix(ref IEnumerable<string> files)
        {
            List<string> hasUnityPackage = new();
            List<string> notUnityPackage = new();
            foreach (var file in files)
            {
                if (Path.GetExtension(file).ToLower() == UNITY_PACKAGE_EXTENSION)
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
            BatchFolderImporter.BatchImport(slot, allDirectoriesToBatchImport, config.GetValue(importAsRawFiles));

            if (notUnityPackage.Count <= 0) return false;
            files = notUnityPackage.ToArray();
            return true;
        }
    }
    
    private static bool ShouldImportFile(string file)
    {
        var extension = Path.GetExtension(file).ToLower();
        var assetClass = AssetHelper.ClassifyExtension(Path.GetExtension(file));
        return (config.GetValue(importText) && assetClass == AssetClass.Text) 
            || (config.GetValue(importTexture) && assetClass == AssetClass.Texture) 
            || (config.GetValue(importDocument) && assetClass == AssetClass.Document) 
            || (config.GetValue(importPointCloud) && assetClass == AssetClass.PointCloud) 
            || (config.GetValue(importAudio) && assetClass == AssetClass.Audio) 
            || (config.GetValue(importFont) && assetClass == AssetClass.Font) 
            || (config.GetValue(importVideo) && assetClass == AssetClass.Video) 
            || (config.GetValue(importMesh) && assetClass == AssetClass.Model && extension != ".xml")   // Handle an edge case where assimp will try to import .xml files as 3D models
            || extension == UNITY_PACKAGE_EXTENSION;                                                            // Handle recursive unity package imports
    }
    
    private static bool ContainsUnicodeCharacter(string input)
    {
        const int MaxAnsiCode = 255;
        return input.Any(c => c > MaxAnsiCode);
    }

    // Credit to delta for this method https://github.com/XDelta/
    private static string GenerateMD5(string filepath)
    {
        using var hasher = MD5.Create();
        using var stream = File.OpenRead(filepath);
        var hash = hasher.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "");
    }
}
