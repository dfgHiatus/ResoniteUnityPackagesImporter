using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.FinalIK;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityPackageImporter.Extractor;
using UnityPackageImporter.FrooxEngineRepresentation;
using UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes;
using UnityPackageImporter.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace UnityPackageImporter;

public class UnityPackageImporter : ResoniteMod
{
    public override string Name => "UnityPackageImporter";
    public override string Author => "dfgHiatus, eia485, delta, Frozenreflex, benaclejames";
    public override string Version => "2.0.0";
    public override string Link => "https://github.com/dfgHiatus/ResoniteUnityPackagesImporter";

    internal const string UNITY_PACKAGE_EXTENSION = ".unitypackage";
    internal const string UNITY_PREFAB_EXTENSION = ".prefab";
    internal const string UNITY_META_EXTENSION = ".meta";
    internal const string TEMP_ORPHAN_NODES_SLOT_NAME = "temp889347298";

    internal static ModConfiguration Config;
    internal static string cachePath = Path.Combine(
        Engine.Current.CachePath, 
        "Cache", 
        "DecompressedUnityPrefabs");

    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importAsRawFiles =
     new ModConfigurationKey<bool>("importAsRawFiles", "Import files as raw binaries", () => false);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> ImportPrefab =
         new ModConfigurationKey<bool>("importPrefab", "Import Prefabs", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importText =
         new ModConfigurationKey<bool>("importText", "Import Text", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importTexture =
         new ModConfigurationKey<bool>("importTexture", "Import Textures", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importDocument =
         new ModConfigurationKey<bool>("importDocument", "Import Documents", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importMesh =
         new ModConfigurationKey<bool>("importMesh", "Import Meshes", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importPointCloud =
         new ModConfigurationKey<bool>("importPointCloud", "Import Point Clouds", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importAudio =
         new ModConfigurationKey<bool>("importAudio", "Import Audio", () => true);
    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<bool> importFont =
         new ModConfigurationKey<bool>("importFont", "Import Fonts", () => true);
    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<bool> importVideo =
         new ModConfigurationKey<bool>("importVideo", "Import Videos", () => true);

    public override void OnEngineInit()
    {
        new Harmony("net.dfgHiatus.UnityPackageImporter").PatchAll();
        Config = GetConfiguration();
        Directory.CreateDirectory(cachePath);
    }

    public static string[] DecomposeUnityPackages(string[] files)
    {
        var fileToHash = files.ToDictionary(file => file, Utils.GenerateMD5);
        HashSet<string> dirsToImport = new HashSet<string>();
        HashSet<string> unityPackagesToDecompress = new HashSet<string>();
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
            if (Utils.ContainsUnicodeCharacter(modelName))
            {
                Error("Imported unity prefab cannot have unicode characters in its file name.");
                continue;
            }
            var extractedPath = Path.Combine(cachePath, fileToHash[package]);
            UnityPackageExtractor.Unpack(package, extractedPath);
            dirsToImport.Add(extractedPath);
        }
        return dirsToImport.ToArray();
    }

    /* 
    Maybe the importer could be made smarter to detect a Unity project with just the prefab's PC path as reference? Then use all of those files to find the dependencies I guess?
    Though, this is fine for now, since the package might have all the dependencies (sometimes)
    If the package doesn't, we just skip those files. Later, a unity project dependency finder should be implemented. So use a way to see the prefab is in a unity project and act from there.
    */
    [HarmonyPatch(typeof(UniversalImporter), "Import", typeof(AssetClass), typeof(IEnumerable<string>),
        typeof(World), typeof(float3), typeof(floatQ), typeof(bool))]
    public partial class UniversalImporterPatch
    {
        


        public static bool Prefix(ref IEnumerable<string> files)
        {

            //instanciate our variables
            //TODO: If the user is able to reimport something while the tasks in the mulitthreading is running, these variables will get reset and cause a multithreading crash.
            //TODO: we need to stop this!
            //TODO: FIX ME FOR THE LOVE OF GOD!!!
            List<string> hasUnityPackage = new List<string>();
            List<string> notUnityPackage = new List<string>();


            foreach (var file in files)
            {
                if (Path.GetExtension(file).ToLower() == UNITY_PACKAGE_EXTENSION)
                    hasUnityPackage.Add(file);
                else
                    notUnityPackage.Add(file);
            }
        
            List<string> allDirectoriesToBatchImport = new List<string>();
            var filespackage = DecomposeUnityPackages(hasUnityPackage.ToArray());
            foreach (var dir in filespackage)
                allDirectoriesToBatchImport.AddRange(Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
                    .Where(ShouldImportFile).ToArray());

            var slot = Engine.Current.WorldManager.FocusedWorld.AddSlot("Unity Package Import");
            slot.PositionInFrontOfUser();



            List<string> scanthesefiles = new List<string>();





            scanthesefiles.AddRange(notUnityPackage);
            //add everything in the unity package for our file scan
            foreach (var dir in DecomposeUnityPackages(hasUnityPackage.ToArray()))
                scanthesefiles.AddRange(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).ToArray());

            /*DebugMSG*/Msg("CALLING FindPrefabsAndMetas");
            scanthesefiles = FindPrefabsAndMetas(slot,
                scanthesefiles,
                Config.GetValue(importAsRawFiles), out PrefabImporter importer).ToList();



            
            

            List<string> noprefabs = scanthesefiles.ToList();
            noprefabs.RemoveAll(i => scanthesefiles.Union(importer.ListOfPrefabs.Values).Union(importer.ListOfMetas.Values).Contains(i));
            scanthesefiles = noprefabs;
            //once we have removed the prefabs, now we let the original stuff go through so we have the files normally
            //idk if we really need this if the stuff above is going to eventually just import prefabs and textures already set up... - @989onan
            BatchFolderImporter.BatchImport(
                slot,
                scanthesefiles,
                Config.GetValue(importAsRawFiles));

            IEnumerable<string> allfiles = files.ToArray();
            slot.StartGlobalTask(async () => await importer.startImports(allfiles, slot, Engine.Current.WorldManager.FocusedWorld.AssetsSlot.AddSlot("UnityPackageImport - Assets")));

            if (notUnityPackage.Count <= 0) return false;
            files = notUnityPackage.ToArray();
            return true;
        }

        private static IEnumerable<string> FindPrefabsAndMetas(Slot root, IEnumerable<string> files, bool forceUnknown, out PrefabImporter importer)
        {
            bool shouldBeRaw = forceUnknown;
            /*DebugMSG*/Msg("Start Finding Prefabs and Metas");
            //remove the meta files from the rest of the code later on in the return statements, since we don't want to let the importer bring in fifty bajillion meta files...
            List<string> ListOfNotMetasAndPrefabs = new List<string>();
            foreach (var file in files)
            {
                var ending = Path.GetExtension(file).ToLower();
                if (!(ending == UNITY_PREFAB_EXTENSION || ending == UNITY_META_EXTENSION))
                {
                    ListOfNotMetasAndPrefabs.Add(file);
                }
            }

            importer = new PrefabImporter();

            if (shouldBeRaw)
            {
                return ListOfNotMetasAndPrefabs.ToArray();
            }

            //first we iterate over every file to find metas and prefabs

            //we make a dictionary that associates the GUID of unity files with their paths. The files given to us are in a cache, with the directories already structured properly and the names fixed.
            // all we do is read the meta file and steal the GUID from there to get our identifiers in the Prefabs
            foreach (var file in files)
            {
                var ending = Path.GetExtension(file).ToLower();
                switch (ending)
                {
                    case UNITY_PREFAB_EXTENSION:
                        break;
                    case UNITY_META_EXTENSION:
                        string filename = file.Substring(0, file.Length - Path.GetExtension(file).Length); //since every meta is filename + extension + ".meta" we can cut off the extension and have the original file name and path.
                        string fileGUID = File.ReadLines(file).ToArray()[1].Split(':')[1].Trim(); // the GUID is on the first line in the file (not 0th) after a colon and space, so trim it to get id.
                        Msg("Adding Asset id with id " + fileGUID + " and file name " + filename);
                        importer.AssetIDDict.Add(fileGUID, filename);
                        if (Path.GetExtension(filename).ToLower() == UNITY_PREFAB_EXTENSION)//if our meta coorisponds to a prefab
                        {
                            importer.ListOfPrefabs.Add(fileGUID, filename);
                        }
                        importer.ListOfMetas.Add(fileGUID, file);
                        break;
                }
            }
            Msg("end Finding Prefabs and Metas");
            return ListOfNotMetasAndPrefabs.ToArray(); 
        }
    }

    private static bool ShouldImportFile(string file)
    {
        var extension = Path.GetExtension(file).ToLower();
        var assetClass = AssetHelper.ClassifyExtension(Path.GetExtension(file));
        return (Config.GetValue(importText) && assetClass == AssetClass.Text) 
            || (Config.GetValue(importTexture) && assetClass == AssetClass.Texture) 
            || (Config.GetValue(importDocument) && assetClass == AssetClass.Document) 
            || (Config.GetValue(importPointCloud) && assetClass == AssetClass.PointCloud) 
            || (Config.GetValue(importAudio) && assetClass == AssetClass.Audio) 
            || (Config.GetValue(importFont) && assetClass == AssetClass.Font) 
            || (Config.GetValue(importVideo) && assetClass == AssetClass.Video)
            /* Handle an edge case where assimp will try to import .xml files as 3D models*/
            || (Config.GetValue(importMesh) && assetClass == AssetClass.Model && extension != ".xml")
            /* Add file if it is a prefab / Add the .meta files into the pile so we can read them to find models later.*/
            || (Config.GetValue(ImportPrefab) && (extension == UNITY_PREFAB_EXTENSION ||  (extension == UNITY_META_EXTENSION)))
            /* Handle recursive unity package imports */
            || extension == UNITY_PACKAGE_EXTENSION;                                                            
    }
}