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
        public static Dictionary<string, string> AssetIDDict;
        public static Dictionary<string, string> ListOfMetas;
        public static Dictionary<string, string> ListOfPrefabs;
        public static Slot importTaskAssetRoot;
        public static Dictionary<ulong, string> MeshRendererID_To_FileGUID = new Dictionary<ulong, string>();
        public static List<FileImportHelperTaskMaterial> TasksMaterials = new List<FileImportHelperTaskMaterial>();
        public static List<Slot> oldSlots = new List<Slot>();


        public static bool Prefix(ref IEnumerable<string> files)
        {

            //instanciate our variables
            //TODO: If the user is able to reimport something while the tasks in the mulitthreading is running, these variables will get reset and cause a multithreading crash.
            //TODO: we need to stop this!
            //TODO: FIX ME FOR THE LOVE OF GOD!!!
            List<string> hasUnityPackage = new List<string>();
            List<string> notUnityPackage = new List<string>();
            AssetIDDict = new Dictionary<string, string>();
            ListOfMetas = new Dictionary<string, string>();
            ListOfPrefabs = new Dictionary<string, string>();
            MeshRendererID_To_FileGUID = new Dictionary<ulong, string>();
            List<FileImportHelperTaskMaterial> TasksMaterials = new List<FileImportHelperTaskMaterial>();
            List<Slot> oldSlots = new List<Slot>();


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


            //scan literally everything selected with our code for importing prefabs

            scanthesefiles.AddRange(notUnityPackage);
            //add everything in the unity package for our file scan
            foreach (var dir in DecomposeUnityPackages(hasUnityPackage.ToArray()))
                scanthesefiles.AddRange(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).ToArray());

            /*DebugMSG*/Msg("CALLING FindPrefabsAndMetas");
            scanthesefiles = FindPrefabsAndMetas(slot,
                scanthesefiles,
                Config.GetValue(importAsRawFiles)).ToList();



            IEnumerable<string>  allfiles = files.ToArray();
            slot.StartGlobalTask(async () => await ImportPrefabStructures(slot, allfiles));

            if (Config.GetValue(importAsRawFiles))
            {
                List<string> noprefabs = scanthesefiles.ToList();
                noprefabs.RemoveAll(i => scanthesefiles.Union(ListOfPrefabs.Values).Union(ListOfMetas.Values).Contains(i));
                scanthesefiles = noprefabs;
            }
            //once we have removed the prefabs, now we let the original stuff go through so we have the files normally
            //idk if we really need this if the stuff above is going to eventually just import prefabs and textures already set up... - @989onan
            BatchFolderImporter.BatchImport(
                slot,
                allDirectoriesToBatchImport,
                Config.GetValue(importAsRawFiles));



            if (notUnityPackage.Count <= 0) return false;
            files = notUnityPackage.ToArray();
            return true;
        }

        private static async Task AttachAssetsToPrefabWrapper(Dictionary<ulong, IUnityObject> unityprefabobjects, Slot root, IEnumerable<string> files)
        {
            await default(ToBackground);
            await AttachAssetsToPrefab(unityprefabobjects, root, files);
            
        }

        private static async Task AttachAssetsToPrefab(Dictionary<ulong, IUnityObject> unityprefabobjects, Slot root, IEnumerable<string> files)
        {
            await default(ToBackground);

            
            List<ImportMeshTask> Tasks = new List<ImportMeshTask>();

            List<string> StartedImports_GUIDs = new List<string>();

            Msg("Generating tasks for imports for this prefab: "+root.ToString());
            foreach (var requestedTask in MeshRendererID_To_FileGUID)
            {
                FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer requestingMeshRenderer = ((FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer)unityprefabobjects[requestedTask.Key]);
                ulong meshid = ulong.Parse(requestingMeshRenderer.m_Mesh["fileID"]);
                unityprefabobjects.TryGetValue(requestingMeshRenderer.m_GameObject["fileID"], out IUnityObject parentobj);
                GameObject SkinnedMeshSlot = ((GameObject)parentobj);
                ulong meshRendererID = requestingMeshRenderer.id;
                if (!StartedImports_GUIDs.Contains(requestedTask.Value))
                {
                    Msg("Generating tasks for import: " + requestedTask.Value);
                    StartedImports_GUIDs.Add(requestedTask.Value);
                    Tasks.Add(new ImportMeshTask(requestingMeshRenderer, new FileImportTask(root, requestedTask.Value), root));
                }
            }
            Msg("Gathering wait jobs");
            List<Task> waiters = new List<Task>();

            foreach (var t in Tasks)
            {
                waiters.Add(t.fileImportTask.runImportFileMeshesAsync());
            }
            foreach(var mat in TasksMaterials)
            {
               waiters.Add(mat.task);
            }
            Msg("Waiting on asset import tasks now...");
            await default(ToBackground);

            await default(ToWorld);
            Msg("Waiting on asset mesh attaching import tasks now...");
            await default(ToBackground);
            Task.WaitAll(waiters.ToArray());

            waiters = new List<Task>();

            foreach (var t in Tasks)
            {
                waiters.Add(t.ImportMeshTaskASync());
            }
            Task.WaitAll(waiters.ToArray());
            await default(ToWorld);

            //Now time to merge.
            Msg("Merging bones");
            
            await default(ToWorld);
            oldSlots = new List<Slot>();
            foreach (ImportMeshTask task in Tasks)
            {
                await default(ToWorld);
                Msg("Start task finalization");
                Msg("Task is valid?" + (task != null).ToString());

                Msg("Grabbing Task Slot for task " + task.requestingMeshRenderer.createdMeshRenderer.Slot.Name);
                Slot taskSlot = task.PrefabRoot;
                Msg("is task slot valid? " + (null != taskSlot).ToString());
                Msg("is task prefab root slot valid? " + (null != taskSlot).ToString());

                //EXPLAINATION OF THIS CODE:
                //we are using the froox engine data made from assimp to force our model onto what we generated instead of
                //what froox engine made. This is better than asking froox engine to fully import the model for us
                //we get more control this way.


                
                Rig rig = taskSlot.GetComponent<Rig>();
                if(rig == null) {
                    Msg("rig missing, attaching.");
                    Msg("slot to string is: " + taskSlot.ToString());
                    rig = taskSlot.AttachComponent<Rig>();
                }
                await default(ToBackground);

                Msg("finding this task's Prefab Data");

                await default(ToWorld);
                //var meshname = taskSlot.Name;
                var foundvrik = null != taskSlot.GetComponent(typeof(VRIK));
                MetaDataFile metadata = null;
                if (!task.fileImportTask.isBiped.HasValue)
                {
                    Msg("checking biped data once for the first time, and only once for file: \""+ task.fileImportTask.file+"\"");
                    Msg("Reading this Model File's MetaData");
                    //this creates our biped rig under the slot we specify. in this case, it is the root.
                    //it scans the file's metadata to make it work.
                    //this MetaDataFile object class gives us access to the biped rig component directly if desired
                    //but we're using it to also get our global scale.
                    metadata = await MetaDataFile.ScanFile(task.fileImportTask.file + UNITY_META_EXTENSION, taskSlot);
                    task.fileImportTask.isBiped = metadata.modelBoneHumanoidAssignments.IsBiped;
                }
                await default(ToBackground);

                await default(ToWorld);
                Msg("Finding if we set up VRIK and are biped.");
                //this has value shouldn't throw an error because it was assigned above.
                if (!foundvrik && (task.fileImportTask.isBiped.Value))
                {
                    Msg("We have not set up vrik!");


                    //put our stuff under a slot called rootnode so froox engine can set this model up as an avatar
                    await default(ToWorld);
                    Slot rootnode = taskSlot.AddSlot("RootNode");
                    rootnode.LocalScale *= metadata.GlobalScale;
                    foreach (Slot prefabImmediateChild in taskSlot.Children.ToArray())
                    {
                        prefabImmediateChild.SetParent(rootnode, false);
                    }



                    Msg("Scaling up/down armature to file's global scale.");
                    //make the bone distances bigger since this model's file may have been exported 100X smaller
                    await default(ToWorld);
                    foreach (Slot slot in rootnode.GetAllChildren(false).ToArray()) {
                        if(null == slot.GetComponent<FrooxEngine.SkinnedMeshRenderer>())
                        {
                            
                            Msg("scaling bone " + slot.Name);
                            slot.LocalPosition /= metadata.GlobalScale;
                        }


                        Msg("creating bone colliders for bone " + slot.Name);
                        BodyNode node = BodyNode.NONE;
                        try
                        {
                            node = metadata.modelBoneHumanoidAssignments.Bones.FirstOrDefault(i => i.Value.Target.Name.Equals(slot.Name)).key;
                        }
                        catch (Exception) { } //this is to catch key not found so we shouldn't handle this.

                        if (BodyNode.NONE != node)
                        {
                            Msg("finding length of bone " + slot.Name);
                            float3 v = float3.Zero;
                            Msg("finding bone length " + slot.Name);
                            foreach (Slot child in slot.Children.ToArray())
                            {
                                float3 globalPoint = child.GlobalPosition;
                                v = slot.GlobalPointToLocal(in globalPoint);
                                if (v.Magnitude > 0.1f)
                                {
                                    break;
                                }
                            }
                            foreach (Slot child in slot.Children.ToArray())
                            {
                                await default(ToWorld);
                                float3 globalPoint = child.GlobalPosition;
                                float value = v.Magnitude * 0.125f;
                                float magnitude = v.Magnitude;

                                v = slot.GlobalPointToLocal(in globalPoint);
                                float3 localPosition = v * 0.5f;
                                Slot slotcollider = slot.AddSlot(slot.Name + " Collider");
                                slotcollider.LocalPosition = localPosition;
                                floatQ a = floatQ.LookRotation(in v);
                                float3 globalPoint2 = float3.Up;
                                float3 to = float3.Forward;
                                floatQ b = floatQ.FromToRotation(in globalPoint2, in to);
                                slotcollider.LocalRotation = a * b;

                                await default(ToWorld);
                                CapsuleCollider capsuleCollider = slotcollider.AttachComponent<CapsuleCollider>();
                                capsuleCollider.Radius.Value = value;
                                capsuleCollider.Height.Value = magnitude;
                            }
                        }
                        
                    }

                    await default(ToBackground);



                    Msg("attaching VRIK");
                    await default(ToWorld);
                    VRIK vrik = taskSlot.AttachComponent<VRIK>();
                    await default(ToBackground);
                    Msg("Initializing VRIK");
                    await default(ToWorld);
                    vrik.Solver.SimulationSpace.Target = taskSlot.Parent;
                    vrik.Solver.OffsetSpace.Target = taskSlot.Parent;
                    vrik.Initiate(); //create our vrik component using our custom biped rig as our humanoid. Since it's on the same slot, 
                    taskSlot.AttachComponent<DestroyRoot>();
                    taskSlot.AttachComponent<ObjectRoot>();
                    await default(ToBackground);

                }
                await default(ToBackground);

            }

            foreach (Slot oldSlot in oldSlots.ToArray())
            {
                await default(ToWorld);
                oldSlot.Destroy();
            }


            await default(ToBackground);
            Msg("Finished Handling Lagged Import Tasks");
            MeshRendererID_To_FileGUID.Clear(); // Very important since it's a static variable! This needs to be called at least at the end of importing.
            TasksMaterials.Clear(); // Very important since it's a static variable! This needs to be called at least at the end of importing.
        }

        private static IEnumerable<string> FindPrefabsAndMetas(Slot root, IEnumerable<string> files, bool forceUnknown)
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



            if (shouldBeRaw)
            {
                return ListOfNotMetasAndPrefabs.ToArray();
            }

            //first we iterate over every file to find metas and prefabs

            //we make a dictionary that associates the GUID of unity files with their paths. The files given to us are in a cache, with the directories already structured properly and the names fixed.
            // all we do is read the meta file and steal the GUID from there to get our identifiers in the Prefabs
            bool flag = true;
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
                        AssetIDDict.Add(fileGUID, filename); 
                        if (Path.GetExtension(filename).ToLower() == UNITY_PREFAB_EXTENSION)//if our meta coorisponds to a prefab
                        {
                            ListOfPrefabs.Add(fileGUID, filename);
                        }
                        ListOfMetas.Add(fileGUID, file);
                        if (flag)
                        {//create our unity import assets root if we are importing metas in this file list.
                            importTaskAssetRoot = Engine.Current.WorldManager.FocusedWorld.AssetsSlot.AddSlot("UnityPackageImportAssets");
                            flag = false;
                        }
                        break;
                }
            }

            //now the files are found, we will use these in our PostFix. We want to yeet these from the normal FrooxEngine import process, but keep them for last
            //so we can read them and do some wizard magic with the files corrosponding with our prefabs at the end

            Msg("end Finding Prefabs and Metas");
            return ListOfNotMetasAndPrefabs.ToArray(); 
        }


        private static async Task LoadPrefabUnity(IEnumerable<string> files, Slot parentUnder, KeyValuePair<string,string> PrefabID)
        {




            await default(ToWorld);
            var prefabslot = Engine.Current.WorldManager.FocusedWorld.AddSlot(Path.GetFileName(PrefabID.Value));
            prefabslot.SetParent(parentUnder,false);
            await default(ToBackground);
            //begin the parsing of our prefabs.
            //begin the parsing of our prefabs.

            //parse loop
            //now using the power of yaml we can make this a bit more reliable and hopefully smaller.
            //reading unity prefabs as yaml allows us to much more easily obtain the data we need.
            //Unity yamls are different, but with a little trickery we can still read them with a library.



            using var sr = File.OpenText(PrefabID.Value);

            Dictionary<ulong, IUnityObject> unityprefabobjects = new Dictionary<ulong, IUnityObject>();

            var deserializer = new DeserializerBuilder().WithNodeTypeResolver(new UnityNodeTypeResolver()).IgnoreUnmatchedProperties().Build();

            var parser = new Parser(sr);

            StringBuilder debugPrefab = new StringBuilder();
            parser.Consume<StreamStart>();
            DocumentStart variable;
            while (parser.Accept<DocumentStart>(out variable) == true)
            {
                // Deserialize the document
                try
                {
                    UnityEngineObjectWrapper docWrapped = deserializer.Deserialize<UnityEngineObjectWrapper>(parser);
                    IUnityObject doc = docWrapped.Result();
                    doc.id = UnityNodeTypeResolver.anchor; //this works because they're separate documents and we're deserializing them one by one. Not nessarily in order, we're just gathering them.
                    //since deserializing happens before adding to the list and those are done syncronously with each other, it is fine.
                    unityprefabobjects.Add(doc.id, doc);
                }
                catch(Exception e)
                {
                    Msg("Couldn't evaluate node type. stacktrace below");
                    Warn(e.Message + e.StackTrace);
                    try
                    {
                        IUnityObject doc = new FrooxEngineRepresentation.GameObjectTypes.NullType();
                        doc.id = UnityNodeTypeResolver.anchor;
                        unityprefabobjects.Add(doc.id, doc);
                    }
                    catch (ArgumentException e2) {/*idc.*/
                        Msg("Duplicate key probably. just ignore this.");
                        Warn(e2.Message + e2.StackTrace);
                    }
                    
                }
                
                
                
            }

            //some debugging for the user to show them it worked or failed.
            
            Msg("Loaded "+unityprefabobjects.Count.ToString()+" Unity objects/components/meshes!");



            //instanciate our objects to generate our prefab entirely, using the ids we assigned ealier to identify our prefab elements in our list.
            
            foreach (var obj in unityprefabobjects)
            {
                await obj.Value.instanciateAsync(unityprefabobjects);
                debugPrefab.Append(obj.ToString());
            }
            
            Debug("now debugging every object after instanciation!");
            Debug(debugPrefab.ToString());
            


            Msg("Yaml generation done");
            Msg("Importing models");

            await AttachAssetsToPrefabWrapper(unityprefabobjects, prefabslot, files);
            await default(ToBackground);
            Msg("Prefab finished!");
        }

        // private static Task ImportModelUnity(Slot slot, string v)
        // {
        //     ModelImportSettings result = ModelImportSettings.PBS(
        //         generateColliders: true,
        //         importBones: true,
        //         importAnimations: true,
        //         pbsUnlit: false,
        //         mapExternalTextures: true,
        //         preferSpecular: false);
        //     result.SetupIK = true;
        //     result.ImportBones = true;

        //     Task importThread = ModelImporter.ImportModelAsync(v, slot, result);
        //     return importThread;
        // }

        private static async Task ImportPrefabStructures(Slot root, IEnumerable<string> files)
        {
            Msg("Start Prefab Importing unitypackage");
            //skip if raw files since we didn't do our setup and the user wants raw files.
            

            //now we have a full list of meta files and prefabs regarding this import file list from our prefix (where ever this is even if not a unity package folder) we now begin the hard part
            // *drums* making the files go onto the model! 

            foreach (var Prefab in ListOfPrefabs)
            {
                Msg("Start prefab import");
                await LoadPrefabUnity(files, root, Prefab); //did this so something can be done with it later.
                
                Msg("End prefab import");
            }
            Msg("end Prefab Importing patch unitypackage");
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