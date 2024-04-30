using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Leap.Unity;
using ResoniteModLoader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using UnityPackageImporter.Extractor;

namespace UnityPackageImporter;

public class UnityPackageImporter : ResoniteMod
{
    public override string Name => "UnityPackageImporter";
    public override string Author => "dfgHiatus, eia485, delta, Frozenreflex, benaclejames, 989onan";
    public override string Version => "2.1.0";
    public override string Link => "https://github.com/dfgHiatus/ResoniteUnityPackagesImporter";

    internal const string UNITY_PACKAGE_EXTENSION = ".unitypackage";
    internal const string UNITY_PREFAB_EXTENSION = ".prefab";
    internal const string UNITY_SCENE_EXTENSION = ".unity";
    internal const string UNITY_META_EXTENSION = ".meta";
    internal static readonly HashSet<string> UNITY_PACKAGE_EXTENSIONS = new() { "unitypackage" }; //must not have a "." in the name anywhere!!- @989onan

    internal static ModConfiguration Config;
    internal static string cachePath = Path.Combine(
        Engine.Current.CachePath, 
        "Cache", 
        "DecompressedUnityPrefabs");

    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> dumpPackageContents =
     new ModConfigurationKey<bool>("dumpPackageContents", "Import files inside of unity packages: import all files inside unity packages instead of just prefabs when \"importPrefab\" is enabled.", () => false);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importAsRawFiles =
     new ModConfigurationKey<bool>("importAsRawFiles", "Import Binaries: Import files as raw binaries", () => false);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> ImportPrefab =
         new ModConfigurationKey<bool>("importPrefab", "Import Prefabs and Scenes: Import prefabs inside unity packages (DISABLES ALL UNLESS \"Import files inside of unity packages\" IS ENABLED)", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importText =
         new ModConfigurationKey<bool>("importText", "Import Text: Import text inside packages", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importTexture =
         new ModConfigurationKey<bool>("importTexture", "Import Textures: Import textures inside packages", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importDocument =
         new ModConfigurationKey<bool>("importDocument", "Import Documents: Import documents inside packages", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importMesh =
         new ModConfigurationKey<bool>("importMesh", "Import Meshes: Import meshes inside packages", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importPointCloud =
         new ModConfigurationKey<bool>("importPointCloud", "Import Point Clouds: Import point clouds inside packages", () => true);
    [AutoRegisterConfigKey]
    internal readonly static ModConfigurationKey<bool> importAudio =
         new ModConfigurationKey<bool>("importAudio", "Import Audio: Import audio files inside packages", () => true);
    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<bool> importFont =
         new ModConfigurationKey<bool>("importFont", "Import Fonts: Import fonts inside packages", () => true);
    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<bool> importVideo =
         new ModConfigurationKey<bool>("importVideo", "Import Videos: Import videos inside packages", () => true);

    public override void OnEngineInit()
    {
        new Harmony("net.dfgHiatus.UnityPackageImporter").PatchAll();
        Config = GetConfiguration();
        Directory.CreateDirectory(cachePath);
        Engine.Current.RunPostInit(AssetPatch); //patch unity packages in
    }

    public static string[] DecomposeUnityPackage(string file)
    {
        var fileToHash = new Dictionary<string,string>(){ { file, Utils.GenerateMD5(file) } };
        HashSet<string> dirsToImport = new HashSet<string>();
        HashSet<string> unityPackagesToDecompress = new HashSet<string>();
        foreach (var element in fileToHash)
        {
            var dir = Path.Combine(cachePath, element.Value);
            if (!Directory.Exists(dir)) //find if we have extracted a package like this before
            {
                unityPackagesToDecompress.Add(element.Key);
            }
            else//else we have imported a unity package exactly like this. skip importing the unity package and use our cache.
            {//if there were two different files with the same MD5, then you should be collecting cash money awards.
                var extractedPath = Path.Combine(cachePath, fileToHash[element.Key], "Assets");
                var allfiles = Directory.GetFiles(extractedPath, "*", SearchOption.AllDirectories).ToArray();
                foreach (string i in allfiles)
                {
                    dirsToImport.Add(i); //add the list of already extracted files to our cache.
                }
            }
        }
        foreach (var package in unityPackagesToDecompress) //extract the package directory if it doesn't exist
        {
            var modelName = Path.GetFileNameWithoutExtension(package);
            if (Utils.ContainsUnicodeCharacter(modelName))
            {
                Error("Imported unity prefab cannot have unicode characters in its file name.");
                continue;
            }
            var extractedPath = Path.Combine(cachePath, fileToHash[package]);
            List<string> paths = UnityPackageExtractor.Unpack(package, extractedPath); //unpack each unity directory individually (it's a huge list of folders each one has only one asset)
            foreach (string i in paths)
            {
                dirsToImport.Add(i); //add all the paths it found to the list of files
            }
            
        }
        return dirsToImport.ToArray();
    }

    //patch unity packages into file metadata stuff so we can use them
    private static void AssetPatch()
    {
        var aExt = Traverse.Create(typeof(AssetHelper)).Field<Dictionary<AssetClass, List<string>>>("associatedExtensions");
        foreach(string hash in UNITY_PACKAGE_EXTENSIONS)
        {
            aExt.Value[AssetClass.Model].Add(hash);
        }
        
    }

    private static async Task scanfiles(List<string> hasUnityPackage, Slot slot, World world)
    {


        List<Task> imports = new List<Task>();
        await default(ToBackground);
        foreach (string unitypackage in hasUnityPackage)
        {
            List<string> scanthesefiles = new List<string>(DecomposeUnityPackage(unitypackage));



            Msg("CALLING FindPrefabsAndMetas");
            List<string> notprefabsandmetas = (await FindPrefabsAndMetas(scanthesefiles, slot, imports, world)).ToList();
            if (Config.GetValue(dumpPackageContents))
            {
                if (Config.GetValue(ImportPrefab))
                {
                    //get all files that don't have metas
                    BatchFolderImporter.BatchImport(slot, scanthesefiles.FindAll(i => !Path.GetExtension(i).ToLower().Equals(UNITY_META_EXTENSION)), Config.GetValue(importAsRawFiles));
                }
                else
                {
                    //bring in no prefabs or metas
                    BatchFolderImporter.BatchImport(slot, notprefabsandmetas, Config.GetValue(importAsRawFiles));
                }
            }


        }

        await default(ToWorld);
        await Task.WhenAll(imports);
        await default(ToBackground);
        Msg("FINISHED ALL IMPORTS AND DONE WITH ALL TASKS!!");
    }

    private static async Task<IEnumerable<string>> FindPrefabsAndMetas(IEnumerable<string> files, Slot importSlotContainment, List<Task> imports, World world)
    {
        /*DebugMSG*/
        Msg("Start Finding Prefabs and Metas");
        //remove the meta files from the rest of the code later on in the return statements, since we don't want to let the importer bring in fifty bajillion meta files...
        List<string> ListOfNotMetasAndPrefabs = new List<string>();
        foreach (var file in files)
        {
            var ending = Path.GetExtension(file).ToLower();
            if (!(ending.Equals(UNITY_PREFAB_EXTENSION) || ending.Equals(UNITY_META_EXTENSION) || ending.Equals(UNITY_SCENE_EXTENSION)))
            {
                ListOfNotMetasAndPrefabs.Add(file);
            }
        }


        Dictionary<string, string> AssetIDDict = new Dictionary<string, string>();
        Dictionary<string, string> ListOfPrefabs = new Dictionary<string, string>();
        Dictionary<string, string> ListOfMetas = new Dictionary<string, string>();
        Dictionary<string, string> ListOfUnityScenes = new Dictionary<string, string>();
        //first we iterate over every file to find metas and prefabs

        //we make a dictionary that associates the GUID of unity files with their paths. The files given to us are in a cache, with the directories already structured properly and the names fixed.
        // all we do is read the meta file and steal the GUID from there to get our identifiers in the Prefabs
        foreach (var file in files)
        {
            //UnityPackageImporter.Msg("A file being imported is \""+file+"\"");
            var ending = Path.GetExtension(file).ToLower();

            switch (ending)
            {
                case UNITY_META_EXTENSION:
                    string filename = file.Substring(0, file.Length - Path.GetExtension(file).Length); //since every meta is filename + extension + ".meta" we can cut off the extension and have the original file name and path.
                    string fileGUID = File.ReadLines(file).ToArray()[1].Split(':')[1].Trim(); // the GUID is on the first line in the file (not 0th) after a colon and space, so trim it to get id.
                    AssetIDDict.Add(fileGUID, filename);
                    if (Path.GetExtension(filename).ToLower() == UNITY_PREFAB_EXTENSION)//if our meta coorisponds to a prefab
                    {
                        ListOfPrefabs.Add(fileGUID, filename);
                    }
                    if (Path.GetExtension(filename).ToLower() == UNITY_SCENE_EXTENSION)//if our meta coorisponds to a scene
                    {
                        ListOfUnityScenes.Add(fileGUID, filename);
                    }
                    ListOfMetas.Add(fileGUID, file);
                    break;
            }
        }
        Msg("Creating importer object");

        if (Config.GetValue(ImportPrefab))
        {
            await default(ToWorld);
            imports.Add(new UnityProjectImporter(files, AssetIDDict, ListOfPrefabs, ListOfMetas, ListOfUnityScenes, importSlotContainment, world.AssetsSlot.AddSlot("UnityPackageImport - Assets"), world).startImports());
            await default(ToBackground);
        }


        Msg("end Finding Prefabs and Metas");
        return ListOfNotMetasAndPrefabs.ToArray();
    }


    [HarmonyPatch(typeof(UniversalImporter),
        "ImportTask",
        typeof(AssetClass),
        typeof(IEnumerable<string>),
        typeof(World),
        typeof(float3),
        typeof(floatQ),
        typeof(float3),
        typeof(bool))]
    public partial class UniversalImporterPatch
    {
        public static bool Prefix(ref IEnumerable<string> files, ref World world)
        {

            List<string> hasUnityPackage = new List<string>();
            List<string> notUnityPackage = new List<string>();

            Msg("Run UnityPackageImporter patch.");
            foreach (var file in files)
            {
                if (Path.GetExtension(file).ToLower() == UNITY_PACKAGE_EXTENSION)
                    hasUnityPackage.Add(file);
                else
                    notUnityPackage.Add(file);
            }

            World curworld = world.RootSlot.World;
            if (hasUnityPackage.Count > 0)
            {
                Msg("Start import of unity packages.");
                var slot = world.AddSlot("Unity Package Import");
                slot.GlobalPosition = new float3(0, 0, 0); //we want scenes to position themselves at 0,0,0. There is an edge case where the thing this is parented under would be moving, but that's just a skill issue on the user's part. - @989onan
                slot.SetParent(world.LocalUserSpace, true); //let user managers not freak out that we're doing stuff in root. - @989onan
                slot.StartGlobalTask(async () => await scanfiles(hasUnityPackage, slot, curworld));
                
            }


            //once we have removed the prefabs, now we let the original stuff go through so we have the files normally
            //idk if we really need this if the stuff above is going to eventually just import prefabs and textures already set up... - @989onan


            files = notUnityPackage;
            
            if(notUnityPackage.Count > 0)
            {
                return true;
            }
            else{
                return false; //we have only unity packages, so don't run the rest and make some random model import dialogue
            }
            
        }
    }


    //unused, should we keep? - @989onan
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
            /* Handle recursive unity package imports */
            || extension == UNITY_PACKAGE_EXTENSION;                                                            
    }
}