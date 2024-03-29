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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Extractor;
using UnityPackageImporter.FrooxEngineRepresentation;
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

        private static List<ImportTaskClass> Tasks = new List<ImportTaskClass>();
        private static List<FileImportHelperTaskMaterial> TasksMaterials = new List<FileImportHelperTaskMaterial>();

        public static bool Prefix(ref IEnumerable<string> files)
        {
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

            SharedData __state = new SharedData();


            List<string> scanthesefiles = new List<string>();


            //scan literally everything selected with our code for importing prefabs

            scanthesefiles.AddRange(notUnityPackage);
            //add everything in the unity package for our file scan
            foreach (var dir in DecomposeUnityPackages(hasUnityPackage.ToArray()))
                scanthesefiles.AddRange(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).ToArray());

            /*DebugMSG*/Msg("CALLING FindPrefabsAndMetas");
            scanthesefiles = FindPrefabsAndMetas(slot,
                scanthesefiles,
                Config.GetValue(importAsRawFiles), out __state).ToList();



            

            List<string> yeetfiles = ImportPrefabStructures(slot, scanthesefiles, Config.GetValue(importAsRawFiles), __state).ToList();


            //our patch can run ourselves using batch import if we are importing files alongside our prefabs
            //because of the checks above, when we run batch import which runs ourselves again, we won't have prefabs in our state.
            //using this, we can detect if we are importing prefabs (Which would be the first pass of ourselves)
            //and not cause multithreading badness
            //if this was not here, we would run our prefab handler every time another model/texture is imported, causing
            //huge performance issues and errors.
            if(__state.ListOfPrefabs.Count > 0)
            {
                /*DebugMSG*/Msg("Start handling Import Tasks");
                slot.StartGlobalTask(async () => await AttachAssetsToPrefabWrapper(slot, scanthesefiles, __state));
            }
            


            scanthesefiles = yeetfiles;

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

        private static async Task AttachAssetsToPrefabWrapper(Slot root, IEnumerable<string> files, SharedData __state)
        {
            await default(ToBackground);
            await AttachAssetsToPrefab(root, files, __state);
            
        }

        private static async Task AttachAssetsToPrefab(Slot root, IEnumerable<string> files, SharedData __state)
        {
            await default(ToBackground);

            Msg("Gathering wait jobs");
            List<Task> waiters = new List<Task>();
            foreach (var t in Tasks)
            {
                waiters.Add(t.fileImportTask.task);
            }
            foreach(var mat in TasksMaterials)
            {
               waiters.Add(mat.task);
            }
            Msg("Waiting on asset import tasks now...");
            await default(ToBackground);
            Task.WaitAll(waiters.ToArray());
            await default(ToBackground);

            //Now time to merge.
            Msg("Merging bones");

            await default(ToWorld);
            List<Slot> oldSlots = new List<Slot>();
            foreach (ImportTaskClass task in Tasks)
            {
                Msg("Start task finalization");
                Msg("Task is valid?" + (task != null).ToString());

                Msg("Grabbing Task Slot for task " + task.meshFileID);
                Slot taskSlot = task.ImportRoot;
                Msg("is task slot valid? " + (null != taskSlot).ToString());
                Msg("is task prefab root slot valid? " + (null != task.Prefabdata.RootSlot).ToString());

                //EXPLAINATION OF THIS CODE:
                //we are using the froox engine data made from assimp to force our model onto what we generated instead of
                //what froox engine made. This is better than asking froox engine to fully import the model for us
                //we get more control this way.



                Rig rig = task.Prefabdata.RootSlot.GetComponent<Rig>();
                if(rig == null) {
                    Msg("rig missing, attaching.");
                    Msg("slot to string is: " + task.Prefabdata.RootSlot.ToString());
                    rig = task.Prefabdata.RootSlot.AttachComponent<Rig>();
                }

                Msg("finding this task's Prefab Data");
                var prefab = task.Prefabdata;
                foreach (SkinnedMeshRenderer skinnedMeshrender in task.fileImportTask.data.skinnedRenderers)
                {
                    if (!(skinnedMeshrender.Bones.Count() > 0)) {
                        skinnedMeshrender.Bones.Clear();
                        Msg("setting up mesh: \"" + skinnedMeshrender.Slot.Name + "\"");
                        for (int index = 0; index < task.BoneArrayIDs.Count(); index++)
                        {
                            //this takes the id of the current bone slot, which is the transform component, gets it's parent which is the game object id, then turns that into a slot using entities.
                            Slot otherBone = (Slot)prefab.Entities[
                                prefab.EntityChildID_To_EntityParentID[task.BoneArrayIDs[index]]
                            ];
                            rig.Bones.AddUnique(otherBone);
                            skinnedMeshrender.Bones.Add().Target = otherBone;
                            
                        }
                        Msg("Waiting for mesh assets for: \"" + skinnedMeshrender.Slot.Name + "\"");
                        while (!skinnedMeshrender.Mesh.IsAssetAvailable)
                        {
                            await Task.Delay(1000);
                        }
                        
                        Msg("getting material objects for: \"" + skinnedMeshrender.Slot.Name + "\"");
                        skinnedMeshrender.Materials.Clear();
                        for (int index = 0; index < task.materialArrayIDs.Count(); index++)
                        {
                            try
                            {
                                skinnedMeshrender.Materials[index] = TasksMaterials.First(i => i.myID == task.materialArrayIDs[index]).finalMaterial;
                            }
                            catch
                            {
                                
                                try
                                {
                                    skinnedMeshrender.Materials.Add().Target = TasksMaterials.First(i => i.myID == task.materialArrayIDs[index]).finalMaterial;
                                }
                                catch(Exception e)
                                {
                                    Msg("Could not attach material \""+ task.materialArrayIDs[index] + "\" from prefab data. It's probably not in the project or in the files you dragged over.");
                                    Msg("stacktrace for material \"" + task.materialArrayIDs[index] + "\"");
                                    Msg(e.StackTrace);
                                }
                            }
                            
                        }
                        

                        skinnedMeshrender.SetupBlendShapes();


                    }

                }

                Msg("Finding our mesh for this task");
                SkinnedMeshRenderer ournewmesh;

                try
                {
                    //this allows us to pull the skinned mesh renderers we imported and then delete their slots later.
                    SkinnedMeshRenderer originalSkinnedRenderer = task.fileImportTask.data.skinnedRenderers.First(i => i.Slot.Name == taskSlot.Name);
                    Slot oldSlot = originalSkinnedRenderer.Slot;
                    ournewmesh = taskSlot.CopyComponent(originalSkinnedRenderer);
                    if (!oldSlots.Contains(oldSlot))
                    {
                        oldSlots.Add(oldSlot);
                    }
                }
                catch (Exception e)
                {
                    Msg("Imported mesh failed to find it's prefab counterpart!");
                    Msg("Import task for file \"" + task.fileImportTask.file + "\" tried to put its skinnedMeshRenderer Component called \"" + taskSlot.Name + "\" under a slot imported by the prefab importer, but it errored! Here's the stacktrace:");
                    Msg(e.StackTrace);
                }

                //var meshname = taskSlot.Name;
                var foundvrik = null != task.Prefabdata.RootSlot.GetComponent(typeof(VRIK));
                MetaDataFile metadata = null;
                if (!task.fileImportTask.isBiped.HasValue)
                {
                    Msg("checking biped data once for the first time, and only once for file: \""+ task.fileImportTask.file+"\"");
                    Msg("Reading this Model File's MetaData");
                    //this creates our biped rig under the slot we specify. in this case, it is the root.
                    //it scans the file's metadata to make it work.
                    //this MetaDataFile object class gives us access to the biped rig component directly if desired
                    //but we're using it to also get our global scale.
                    metadata = MetaDataFile.ScanFile(task.fileImportTask.file + UNITY_META_EXTENSION, task.Prefabdata.RootSlot);
                    task.fileImportTask.isBiped = metadata.modelBoneHumanoidAssignments.IsBiped;
                }
                Msg("Finding if we set up VRIK and are biped.");
                //this has value shouldn't throw an error because it was assigned above.
                if (!foundvrik && (task.fileImportTask.isBiped.Value))
                {
                    Msg("We have not set up vrik!");


                    //put our stuff under a slot called rootnode so froox engine can set this model up as an avatar
                    await default(ToWorld);
                    Slot rootnode = task.Prefabdata.RootSlot.AddSlot("RootNode");
                    rootnode.LocalScale *= metadata.GlobalScale;
                    foreach (Slot prefabImmediateChild in task.Prefabdata.RootSlot.Children.ToArray())
                    {
                        prefabImmediateChild.SetParent(rootnode, false);
                    }



                    Msg("Scaling up/down armature to file's global scale.");
                    //make the bone distances bigger since this model's file may have been exported 100X smaller
                    foreach (Slot slot in rootnode.GetAllChildren(false).ToArray()) {
                        if(null == slot.GetComponent<SkinnedMeshRenderer>())
                        {
                            await default(ToWorld);
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

                    



                    Msg("attaching VRIK");
                    await default(ToWorld);
                    VRIK vrik = prefab.RootSlot.AttachComponent<VRIK>();

                    Msg("Initializing VRIK");
                    await default(ToWorld);
                    vrik.Solver.SimulationSpace.Target = prefab.RootSlot.Parent;
                    vrik.Solver.OffsetSpace.Target = prefab.RootSlot.Parent;
                    vrik.Initiate(); //create our vrik component using our custom biped rig as our humanoid. Since it's on the same slot, 
                    prefab.RootSlot.AttachComponent<DestroyRoot>();
                    prefab.RootSlot.AttachComponent<ObjectRoot>();

                }

                // This makes sure the bones related to this skinned mesh renderer from the prefab get put onto the skinned mesh renderer that FrooxEngine made
                
            }
            foreach (Slot oldSlot in oldSlots.ToArray())
            {
                await default(ToWorld);
                oldSlot.Destroy();
            }


            await default(ToBackground);
            Msg("Finished Handling Lagged Import Tasks");
            Tasks.Clear(); // Very important since it's a static variable! This needs to be called at least at the end of importing.
            TasksMaterials.Clear(); // Very important since it's a static variable! This needs to be called at least at the end of importing.
        }

        private static IEnumerable<string> FindPrefabsAndMetas(Slot root, IEnumerable<string> files, bool forceUnknown, out SharedData __state)
        {
            bool shouldBeRaw = forceUnknown;
            Slot parentUnder = root;
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

            __state = new SharedData
            {
                ListOfPrefabs = new List<string>(),
                ListOfMetas = new List<string>(),
                AssetIDDict = new Dictionary<string, string>(),
                FileName_To_AssetIDDict = new Dictionary<string, string>()
            };



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
                        __state.ListOfPrefabs.Add(file);
                        break;
                    case UNITY_META_EXTENSION:
                        string filename = file.Substring(0, file.Length - Path.GetExtension(file).Length); //since every meta is filename + extension + ".meta" we can cut off the extension and have the original file name and path.
                        string fileGUID = File.ReadLines(file).ToArray()[1].Split(':')[1].Trim();
                        __state.AssetIDDict.Add(fileGUID, filename); // the GUID is on the first line (not 0th) after a colon and space, so trim it to get id.
                        __state.FileName_To_AssetIDDict.Add(filename, fileGUID); //have a flipped one for laters. I know I'm not very efficient with this one.
                        __state.ListOfMetas.Add(file);
                        if (flag)
                        {//create our unity import assets root if we are importing metas in this file list.
                            __state.importTaskAssetRoot = Engine.Current.WorldManager.FocusedWorld.AssetsSlot.AddSlot("UnityPackageImportAssets");
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


        private static void LoadPrefabUnity(IEnumerable<string> files, SharedData __state, Slot parentUnder, string PrefabID)
        {
            string prefab = __state.AssetIDDict.GetValueSafe(PrefabID);
            var prefabslot = Engine.Current.WorldManager.FocusedWorld.AddSlot(Path.GetFileName(prefab));
            prefabslot.SetParent(parentUnder,false);

            //begin the parsing of our prefabs.
            //begin the parsing of our prefabs.

            //parse loop
            //now using the power of yaml we can make this a bit more reliable and hopefully smaller.
            //reading unity prefabs as yaml allows us to much more easily obtain the data we need.
            //Unity yamls are different, but with a little trickery we can still read them with a library.



            using var sr = File.OpenText(prefab);

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
                    unityprefabobjects.Add(doc.id, doc);
                    debugPrefab.Append(doc.ToString());
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
                        debugPrefab.Append(doc.ToString());
                    }
                    catch (ArgumentException e2) {/*idc.*/
                        Msg("Duplicate key probably. just ignore this.");
                        Warn(e2.Message + e2.StackTrace);
                    }
                    
                }
                
                
                
            }

            //some debugging for the user to show them it worked or failed.
            Msg(debugPrefab.ToString());
            Msg(unityprefabobjects.Count);
            //instanciate our objects to generate our prefab entirely, using the ids we assigned ealier to identify our prefab elements in our list.
            foreach (var obj in unityprefabobjects)
            {
                obj.Value.instanciate(unityprefabobjects);
            }

            
            

            Msg("Yaml generation done");
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

        private static IEnumerable<string> ImportPrefabStructures(Slot root, IEnumerable<string> files, bool shouldBeRaw, SharedData __state)
        {
            Msg("Start Prefab Importing unitypackage");
            //skip if raw files since we didn't do our setup and the user wants raw files.
            if (shouldBeRaw) return files;
            List<string> noprefabs = files.ToList();
            noprefabs.RemoveAll(i => files.Union(__state.ListOfPrefabs).Union(__state.ListOfMetas).Contains(i));
            IEnumerable<string> returnedfiles = noprefabs;

            //now we have a full list of meta files and prefabs regarding this import file list from our prefix (where ever this is even if not a unity package folder) we now begin the hard part
            // *drums* making the files go onto the model! 

            foreach (string Prefab in __state.ListOfPrefabs)
            {
                Msg("Start prefab import");
                LoadPrefabUnity(files, __state, root, __state.FileName_To_AssetIDDict[Prefab]); //did this so something can be done with it later.
                
                Msg("End prefab import");
            }

            Msg("end Prefab Importing patch unitypackage");
            return returnedfiles;
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