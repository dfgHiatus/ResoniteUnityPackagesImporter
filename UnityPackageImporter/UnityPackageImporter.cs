using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.FinalIK;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using UnityPackageImporter.Extractor;
using Assimp;
using Assimp.Configs;
using UnityPackageImporter.Models;
using static FrooxEngine.ModelImporter;
using Mono.Cecil.Cil;
using static FrooxEngine.ModelExporter;

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

            /*DebugMSG*/Msg("CALLING The pre import patch");
            scanthesefiles = startmethod(slot,
                scanthesefiles,
                Config.GetValue(importAsRawFiles), out __state).ToList();





            List<string> yeetfiles = EndMethod(slot, scanthesefiles, Config.GetValue(importAsRawFiles), __state).ToList();

            /*DebugMSG*/Msg("Start handling Import Tasks");
            slot.StartCoroutine(FinishPrefabWrapper(slot, scanthesefiles, __state));


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

        private static IEnumerator<Context> FinishPrefabWrapper(Slot root, IEnumerable<string> files, SharedData __state)
        {
            __state.ListOfMetas.Add("hello");
            yield return Context.WaitFor(FinishPrefab(root, files, __state));
            yield break;
        }

        private static IEnumerator<Context> FinishPrefab(Slot root, IEnumerable<string> files, SharedData __state)
        {
            Msg("Waiting on Import Tasks");
            foreach (ImportTaskClass task in Tasks)
            {

                task.fileImportTask.task.Wait();
            }

            //Now time to merge.
            Msg("Merging bones");
            foreach (ImportTaskClass task in Tasks)
            {
                Slot taskSlot = task.ImportRoot;

                //EXPLAINATION OF THIS CODE:
                //we are using the froox engine data made from assimp to force our model onto what we generated instead of
                //what froox engine made. This is better than asking froox engine to fully import the model for us
                //we get more control this way.


                SkinnedMeshRenderer ournewmesh;

                try
                {
                    ournewmesh = taskSlot.CopyComponent<SkinnedMeshRenderer>(task.fileImportTask.data.skinnedRenderers.First(i => i.Slot.Name == taskSlot.Name));
                }
                catch(Exception e)
                {
                    Msg("Import task for file \"" + task.fileImportTask.file + "\" tried to put its skinnedMeshRenderer Component called \"" + taskSlot.Name + "\" under a slot imported by the prefab importer, but it errored! Here's the stacktrace:");
                    Msg(e.StackTrace);
                }

                


                

                

                //var meshname = taskSlot.Name;

                var foundvrik = task.Prefabdata.RootSlot.Components.Any(i => i.GetType() == typeof(VRIK));

                if (!foundvrik)
                {
                    //if we haven't set up VRIK, we also haven't set up our custom bones.
                    foreach (SkinnedMeshRenderer skinnedMeshrender in task.fileImportTask.data.skinnedRenderers)
                    {
                        SkinnedMeshRenderer skinnedmeshrenderobj = skinnedMeshrender;
                        for (int index = 0; index < skinnedmeshrenderobj.Bones.Count(); index++)
                        {
                            Slot otherBone = (Slot)task.Prefabdata.Entities[
                                task.Prefabdata.EntityChildID_To_EntityParentID[task.BoneArrayIDs[index]]
                            ];

                            //this takes the id of the current bone slot, which is the transform component, gets it's parent which is the game object id, then turns that into a slot using entities.
                            skinnedmeshrenderobj.Bones[index] = otherBone;

                        }

                    }
                    //this creates our biped rig under the slot we specify. in this case, it is the root.
                    //it scans the file's metadata to make it work.
                    //this MetaDataFile object class also gives us access to the biped rig component directly if desired.
                    MetaDataFile metadata = MetaDataFile.ScanFile(Path.ChangeExtension(task.fileImportTask.file, ".meta"), task.Prefabdata.RootSlot);
                    VRIK vrik = task.Prefabdata.RootSlot.AttachComponent<VRIK>();

                    
                    
                    
                    vrik.Solver.SimulationSpace.Target = task.Prefabdata.RootSlot.Parent;
                    vrik.Solver.OffsetSpace.Target = task.Prefabdata.RootSlot.Parent;
                    vrik.Initiate(); //create our vrik component using our custom biped rig as our humanoid. Since it's on the same slot, 
                }

                // This makes sure the bones related to this skinned mesh renderer from the prefab get put onto the skinned mesh renderer that FrooxEngine made
                
            }
            Msg("Finished Handling Lagged Import Tasks");
            Tasks.Clear(); // Very important since it's a static variable! This needs to be called at least at the end of importing.
            yield return Context.ToWorld();
            yield break;
        }

        private static IEnumerable<string> startmethod(Slot root, IEnumerable<string> files, bool forceUnknown, out SharedData __state)
        {
            bool shouldBeRaw = forceUnknown;
            Slot parentUnder = root;
            /*DebugMSG*/Msg("Start pre import patch");
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
                        break;
                }
            }

            //now the files are found, we will use these in our PostFix. We want to yeet these from the normal FrooxEngine import process, but keep them for last
            //so we can read them and do some wizard magic with the files corrosponding with our prefabs at the end

            Msg("end preimport patch unitypackage");
            return ListOfNotMetasAndPrefabs.ToArray(); 
        }


        

        private static PrefabData LoadPrefabUnity(IEnumerable<string> files, SharedData __state, Slot parentUnder, string PrefabID)
        {
            string prefab = __state.AssetIDDict.GetValueSafe(PrefabID);
            var prefabslot = Engine.Current.WorldManager.FocusedWorld.AddSlot(Path.GetFileNameWithoutExtension(prefab));
            var tempslot = Engine.Current.WorldManager.FocusedWorld.AddSlot(TEMP_ORPHAN_NODES_SLOT_NAME);
            prefabslot.SetParent(parentUnder);
            tempslot.SetParent(prefabslot);

            //begin the parsing of our prefabs.

            //parse loop
            //how the objects are ordered can be random in a prefab. But the good thing is everything is connected via ID's
            //There is also a header on each object that tells us the ID of the object within this prefab. Weither that be a component or a slot (GameObject)
            //Using this, we can make a flat tree of objects and then construct it into a FrooxEngine object.


            //general component/slot tags
            bool startParseEntity = false;
            string typeofobj = string.Empty;
            string entityID = string.Empty;
            var otherid = string.Empty;
            var foundtransform = false;

            //particle system tags

            //we're gonna use an int with this, here's the key
            /*
             * inStartColor = 0
             * inStartSize = 1
             * inGravityModifier = 2
             * inEmissionModule = 3
             */
            //need to do particle system parsing...
            //int particletag = -1;

            //skinned mesh renderer tags
            bool inBoneSection = false;

            //orphan transforms temp storage
            List<string> tempBoneArrayIDs = new List<string>();
            List<ImportTaskClass> tasksTemp = new();
            Dictionary<string, IWorldElement> entities = new();
            Dictionary<string, string> entityChildID_To_EntityParentID = new();
            Dictionary<string, string> orphanTransforms_Child_To_Parent = new ();
            ImportTaskClass currentEntityImportTaskHolder = new ImportTaskClass();

            foreach (string line in File.ReadLines(prefab))
            {
                //tag if we are in an object
                if (line.StartsWith("--- !u!"))
                {
                    startParseEntity = true;
                    entityID = line.Split('&')[1];
                    continue;
                }

                //startparseentity being set to true means we have found a new object. So time to switch our code.
                if (startParseEntity)
                {
                    typeofobj = line.Split(':')[0];
                    startParseEntity = false; //switch to false so we know we should parse whatever this is until we hit the next component/slot

                    //add this entity to a list of IWorldElements so we can put them where they should go.
                    switch (typeofobj)
                    {
                        case "Transform":
                            Msg("Start TransformComponent interpreting");
                            break;
                        case "GameObject":
                            Msg("Start GameObject interpreting");
                            if (!entityChildID_To_EntityParentID.ContainsKey(entityID))
                                entities.Add(entityID, Engine.Current.WorldManager.FocusedWorld.AddSlot(string.Empty));
                            foundtransform = false;
                            break;
                        case "ParticleSystem":
                            Msg("Start ParticleSystem interpreting");
                            //particletag = -1;
                            break;
                        case "MeshFilter":
                            Msg("Start MeshFilter interpreting");
                            //nothing needed here yet
                            break;
                        case "MeshRenderer":
                            Msg("Start MeshRenderer interpreting");
                            //nothing needed here yet
                            break;
                        case "SkinnedMeshRenderer":
                            Msg("Start SkinnedMeshRenderer interpreting");
                            inBoneSection = false;
                            break;
                    }

                    continue;
                }


                //now we know what kind of entity/obj, time to parse it's data.
                switch (typeofobj)
                {
                    case "GameObject":
                        //this assumes transform is first in the component list which is a good assumption because it always forces itself at the top in unity.
                        //since we handle adding components within the component adding blocks along with the slot, if we add it here we can
                        //assume it will be there at the end of all of this.

                        //if only starts with worked with a switch
                        if (line.StartsWith("  - component") && foundtransform == false)
                        {
                            /*DebugMSG*/Msg("found transform component ref for game object, adding");
                            otherid = line.Split(':')[2].Split('}')[0].Trim();
                            if (!entityChildID_To_EntityParentID.ContainsKey(otherid))
                            {
                                entityChildID_To_EntityParentID.Add(otherid, entityID);
                            }
                            
                            foundtransform = true;
                            break;
                        }
                        else if (line.StartsWith("  - component"))
                        {
                            /*DebugMSG*/Msg("found non transform component ref for game object, adding parent child relationship");
                            otherid = line.Split(':')[2].Split('}')[0].Trim();
                            if (!entityChildID_To_EntityParentID.ContainsKey(otherid))
                            {
                                entityChildID_To_EntityParentID.Add(otherid, entityID);
                            }
                        }
                        else if (line.StartsWith("  m_Name: "))
                        {
                            /*DebugMSG*/Msg("found name for game object, setting");
                            ((Slot)entities[entityID]).Name = line.Remove(0, "  m_Name: ".Length);
                        }
                        else if (line.StartsWith("  m_TagString: "))
                        {
                            /*DebugMSG*/Msg("found tag for game object, setting");
                            ((Slot)entities[entityID]).Tag = line.Remove(0, "  m_TagString: ".Length); //idk if we need this, but it's cool.
                        }
                        else if (line.StartsWith("  m_IsActive: "))
                        {
                            /*DebugMSG*/Msg("found active for game object, setting");
                            ((Slot)entities[entityID]).ActiveSelf = line.Remove(0, "  m_IsActive: ".Length).Trim().Equals("1");
                        }








                        break;
                    case "Transform":

                        //if only starts with worked with a switch
                        if (line.StartsWith("  m_GameObject: "))
                        {
                            /*DebugMSG*/
                            Msg("found game object parent ref id, checking");
                            otherid = line.Split(':')[2].Split('}')[0].Trim();
                            /*DebugMSG*/
                            Msg("id is \"" + otherid + "\"");
                            if (!entityChildID_To_EntityParentID.ContainsKey(entityID))
                            {
                                entityChildID_To_EntityParentID.Add(entityID, otherid);
                            }



                        }
                        else if (line.StartsWith("  m_LocalRotation:"))
                        {
                            float x = 0;
                            float y = 0;
                            float z = 0;
                            float w = 0;

                            //hehe. here's the reference line from a prefab to help who's reading this garbage to understand: "  m_LocalRotation: {x: -0.013095013, y: -0.06099344, z: -0.00031075111, w: 0.99805224}"
                            float.TryParse(line.Split(',')[0].Split(':')[2].Trim(), out x);
                            float.TryParse(line.Split(',')[1].Split(':')[1].Trim(), out y);
                            float.TryParse(line.Split(',')[2].Split(':')[1].Trim(), out z);
                            float.TryParse(line.Split(',')[3].Split(':')[1].Split('}')[0].Trim(), out w);

                            ((Slot)entities[otherid]).LocalRotation = new floatQ(x, y, z, w);
                        }
                        else if (line.StartsWith("  m_LocalPosition:"))
                        {
                            float x = 0;
                            float y = 0;
                            float z = 0;

                            //hehe. here's the reference line from a prefab to help who's reading this garbage to understand: "  m_LocalPosition: {x: -0.013095013, y: -0.06099344, z: -0.00031075111}"
                            float.TryParse(line.Split(',')[0].Split(':')[2].Trim(), out x);
                            float.TryParse(line.Split(',')[1].Split(':')[1].Trim(), out y);
                            float.TryParse(line.Split(',')[2].Split(':')[1].Split('}')[0].Trim(), out z);

                            ((Slot)entities[otherid]).LocalPosition = new float3(x, y, z);


                        }
                        else if (line.StartsWith("  m_LocalScale:"))
                        {
                            float x = 0;
                            float y = 0;
                            float z = 0;

                            //hehe. here's the reference line from a prefab to help who's reading this garbage to understand: "  m_LocalScale: {x: -0.013095013, y: -0.06099344, z: -0.00031075111}"
                            float.TryParse(line.Split(',')[0].Split(':')[2].Trim(), out x);
                            float.TryParse(line.Split(',')[1].Split(':')[1].Trim(), out y);
                            float.TryParse(line.Split(',')[2].Split(':')[1].Split('}')[0].Trim(), out z);

                            ((Slot)entities[otherid]).LocalScale = new float3(x, y, z);
                        }
                        else if (line.StartsWith("  m_Father:"))
                        {

                            //finding this transform object's parent slot. if not, store to a temp array;

                            //here the child transform block could have been made before or after the transform, making finding the slot first impossible. We have to parent later if we can't find it.

                            string parentTransformComponentID = line.Split(':')[2].Split('}')[0].Trim();

                            if (entityChildID_To_EntityParentID.ContainsKey(parentTransformComponentID))
                            {
                                ((Slot)entities[otherid]).SetParent(((Slot)entities[entityChildID_To_EntityParentID[parentTransformComponentID]]), false);
                            }
                            else
                            {
                                if (!orphanTransforms_Child_To_Parent.ContainsKey(entityID))
                                {
                                    orphanTransforms_Child_To_Parent.Add(entityID, parentTransformComponentID);
                                }

                            }

                            //this is garbage, but unity prefabs have forced my hand
                            //basically children transforms can appear after the transform tries to reference it
                            //in coding this is awful. So I have to iterate over freaking everything to find ones that were instantiated
                            //before their children. Pain. - @989onan
                            List<string> removelist = new List<string>();
                            foreach (var pair in orphanTransforms_Child_To_Parent.AsParallel())
                            {
                                if (pair.Value == entityID)
                                {
                                    try
                                    {
                                        ((Slot)entities[entityChildID_To_EntityParentID[pair.Key]]).SetParent(((Slot)entities[otherid]), false);
                                        removelist.Add(pair.Key);
                                    }
                                    catch (Exception e)
                                    {
                                        /*DebugMSG*/
                                        Msg(e.StackTrace);
                                    }
                                }


                            }
                            foreach (string key in removelist)
                            {
                                orphanTransforms_Child_To_Parent.Remove(key);
                            }

                        }
                        break;
                    case "ParticleSystem":
                        if (line.StartsWith("  m_GameObject: "))
                        {
                            /*DebugMSG*/Msg("found game object parent ref id, checking");
                            otherid = line.Split(':')[2].Split('}')[0].Trim();
                            /*DebugMSG*/Msg("id is \"" + otherid+"\"");
                            if (!entityChildID_To_EntityParentID.ContainsKey(entityID))
                            {
                                entityChildID_To_EntityParentID.Add(entityID, otherid);
                            }
                            if (!entities.ContainsKey(otherid))

                            {//now since we add the game object if it appears before this component, and add it prematurely before it appears, now we can assemble 1/2 of the game object in both places
                             //eventually it will be 100% complete at the end.
                             //it's not great coding, but it works.
                             // if we find the component after the game object is created, the slot will be created here and added to when we find the game object so can just add our component right after this.
                             /*DebugMSG*/Msg("id is not made, creating slot");
                                entities.Add(otherid, Engine.Current.WorldManager.FocusedWorld.AddSlot(""));
                                break;
                            }
                        }

                        //this can be fixed later, but looking at a prefab idk what this garbage means - @989onan
                        //this is at least a start on how to handle particle systems. Again idk how to implement this.
                        /*
                              startSize:
                                  serializedVersion: 2
                                  minMaxState: 3
                                  scalar: 0.025
                                  minScalar: 0.01
                                  maxCurve:
                        */


                        ParticleSystem thisComponent = (ParticleSystem)entities[entityID];


                        //use this to know what data you're parsing, since it can repeat the same line in the same component. but they're split by lines that determine the sections like these.
                        if (line.StartsWith("    startColor:"))
                        {
                            //particletag = 0;

                        }
                        else if (line.StartsWith("    startSize:"))
                        {
                            //particletag = 1;
                        }
                        else if (line.StartsWith("    gravityModifier:"))
                        {
                            //particletag = 2;
                        }
                        else if (line.StartsWith("  EmissionModule:"))
                        {
                            //particletag = 3;
                        }








                        break;
                    case "MeshFilter":


                        if (line.StartsWith("  m_GameObject: "))
                        {
                            /*DebugMSG*/Msg("found game object parent ref id, checking");
                            otherid = line.Split(':')[2].Split('}')[0].Trim();
                            /*DebugMSG*/Msg("id is \"" + otherid+"\"");
                            if (!entityChildID_To_EntityParentID.ContainsKey(entityID))
                            {
                                entityChildID_To_EntityParentID.Add(entityID, otherid);
                            }
                            if (!entities.ContainsKey(otherid))

                            {//now since we add the game object if it appears before this component, and add it prematurely before it appears, now we can assemble 1/2 of the game object in both places
                             //eventually it will be 100% complete at the end.
                             //it's not great coding, but it works.
                             // if we find the component after the game object is created, the slot will be created here and added to when we find the game object so can just add our component right after this.
                             /*DebugMSG*/Msg("id is not made, creating slot");
                                entities.Add(otherid, Engine.Current.WorldManager.FocusedWorld.AddSlot(""));
                                break;
                            }
                        }

                        if (line.StartsWith("  m_Mesh: "))
                        {
                            /*DebugMSG*/
                            Msg("found mesh, parsing");
                            var meshFileID = line.Split(':')[2].Split(',')[0].Trim();//this is on purpose, because a line for the m_Mesh looks like this: "  m_Mesh: {fileID: 4300000, guid: d6ba91ccc11280d4da45a2d1c17d88ba, type: 3}"
                            var modelFileGUID = line.Split(':')[3].Split(',')[0].Trim();
                            /*DebugMSG*/
                            Msg("mesh ref id is: \"" + modelFileGUID + "\"");
                            var meshtype = int.Parse(line.Split(':')[4].Split('}')[0].Trim()); //same here

                            if (meshtype == 3)
                            {
                                //this will load the mesh, which is actually a file, and load it into the slot this component should be on.
                                try{
                                    var allTasks = new List<ImportTaskClass>();
                                    allTasks.AddRange(tasksTemp);
                                    allTasks.AddRange(Tasks);
                                    ImportTaskClass importingExists;
                                    try//if this try fails, then a not found exception should have been thrown, allowing us to continue
                                    {
                                        importingExists = allTasks.First(i => i.fileImportTask.assetID.Equals(modelFileGUID));
                                        currentEntityImportTaskHolder = new ImportTaskClass(importingExists.fileImportTask,
                                            meshFileID,
                                            new PrefabData());
                                        allTasks.Clear();
                                    }
                                    catch(InvalidOperationException)
                                    {//if not found, make a new one.
                                        
                                        currentEntityImportTaskHolder = new ImportTaskClass(
                                        new FileImportHelperTask(__state.AssetIDDict[modelFileGUID], ((Slot)entities[otherid]), modelFileGUID),
                                            meshFileID,
                                            new PrefabData());
                                        allTasks.Clear();
                                    }

                                }
                                catch(Exception e)
                                {
                                    Msg("could not find mesh reference!!! Did you forget to import the model along with the prefab at the same time?");
                                    Msg("ERROR BELOW:");
                                    Msg(e.StackTrace);
                                }

                            }

                        }

                        break;
                    case "SkinnedMeshRenderer":
                        if (inBoneSection)
                        {
                            if (line.StartsWith("  - {fileID: "))
                            {
                                //this runs after the file is imported
                                //though looking at sourcecode via DNSpy (which is okay according to TOS of Resonite), the importer runs on an async task, so this is annoying...



                                //This code finds the bone list that this model has and adds it to a list of bone ID's
                                //then we add this to the data that's shoved into the tasks list that gets accessed when the models are done.
                                //this way, we know which id's go to which bones. Then it's as easy as putting them together.


                                
                                
                                tempBoneArrayIDs.Add(line.Split(':')[1].Split('}')[0].Trim());



                            }
                            else
                            {
                                currentEntityImportTaskHolder.BoneArrayIDs = tempBoneArrayIDs;
                                tasksTemp.Add(currentEntityImportTaskHolder);
                            }
                        }



                        if (line.StartsWith("  m_GameObject: "))
                        {
                            /*DebugMSG*/Msg("found game object parent ref id, checking");
                            otherid = line.Split(':')[2].Split('}')[0].Trim();
                            /*DebugMSG*/Msg("id is \"" + otherid + "\"");
                            if (!entityChildID_To_EntityParentID.ContainsKey(entityID))
                            {
                                entityChildID_To_EntityParentID.Add(entityID, otherid);
                            }
                            
                            if (!entities.ContainsKey(otherid)){//now since we add the game object if it appears before this component, and add it prematurely before it appears, now we can assemble 1/2 of the game object in both places
                             //eventually it will be 100% complete at the end.
                             //it's not great coding, but it works.
                             // if we find the component after the game object is created, the slot will be created here and added to when we find the game object so can just add our component right after this.
                             /*DebugMSG*/Msg("id is not made, creating slot");
                                entities.Add(otherid, Engine.Current.WorldManager.FocusedWorld.AddSlot(""));
                                break;
                            }
                        }
                        else if (line.StartsWith("  m_Mesh: "))
                        {
                            /*DebugMSG*/Msg("found mesh, parsing");
                            var meshFileID = line.Split(':')[2].Split(',')[0].Trim();//this is on purpose, because a line for the m_Mesh looks like this: "  m_Mesh: {fileID: 4300000, guid: d6ba91ccc11280d4da45a2d1c17d88ba, type: 3}"
                            var modelFileGUID = line.Split(':')[3].Split(',')[0].Trim();
                            /*DebugMSG*/
                            Msg("mesh ref id is: \""+ modelFileGUID + "\"");
                            var meshtype = int.Parse(line.Split(':')[4].Split('}')[0].Trim()); //same here

                            if (meshtype == 3)
                            {
                                //this will load the mesh, which is actually an fbx, and load it into the slot this component should be on.
                                //yes this may run the importer again
                                //this will load the mesh, which is actually a file, and load it into the slot this component should be on.
                                try
                                {
                                    var allTasks = new List<ImportTaskClass>();
                                    allTasks.AddRange(tasksTemp);
                                    allTasks.AddRange(Tasks);
                                    ImportTaskClass importingExists;
                                    try//if this try fails, then a not found exception should have been thrown, allowing us to continue
                                    {
                                        importingExists = allTasks.First(i => i.fileImportTask.assetID.Equals(modelFileGUID));
                                        currentEntityImportTaskHolder = new ImportTaskClass(importingExists.fileImportTask,
                                            meshFileID,
                                            new PrefabData());
                                        allTasks.Clear();
                                    }
                                    catch (InvalidOperationException)
                                    {//if not found, make a new one.

                                        currentEntityImportTaskHolder = new ImportTaskClass(
                                        new FileImportHelperTask(__state.AssetIDDict[modelFileGUID], ((Slot)entities[otherid]), modelFileGUID),
                                            meshFileID,
                                            new PrefabData());
                                        allTasks.Clear();
                                    }

                                }
                                catch (Exception e)
                                {
                                    Msg("could not find mesh reference!!! Did you forget to import the model along with the prefab at the same time?");
                                    Msg("ERROR BELOW:");
                                    Msg(e.StackTrace);
                                }

                            }

                        }
                        else if (line.StartsWith("  m_Bones:"))
                        {
                            inBoneSection = true;

                            break;
                        }
                        break;
                }
            }

            PrefabData prefabdata = new PrefabData(entities, entityChildID_To_EntityParentID, prefabslot);

            foreach (var task in tasksTemp)
            {
                var newtask = task;
                newtask.Prefabdata = prefabdata;
                Tasks.Add(newtask);
            }

            return prefabdata;
        }

        private static Task ImportModelUnity(Slot slot, string v)
        {
            ModelImportSettings result = ModelImportSettings.PBS(true,true,true,false,true,false);

            result.SetupIK = true;
            result.ImportBones = true;

            Task importthread = ModelImporter.ImportModelAsync(v, slot, result, null, null);

            return importthread;
        }

        private static IEnumerable<string> EndMethod(Slot root, IEnumerable<string> files, bool shouldBeRaw, SharedData __state)
        {
            Msg("Start post import patch unitypackage");
            //skip if raw files since we didn't do our setup and the user wants raw files.
            if (shouldBeRaw) return files;
            files = files.Union(__state.ListOfPrefabs).Union(__state.ListOfMetas);

            //now we have a full list of meta files and prefabs regarding this import file list from our prefix (where ever this is even if not a unity package folder) we now begin the hard part
            // *drums* making the files go onto the model! 

            foreach (string Prefab in __state.ListOfPrefabs)
            {
                Msg("Start prefab import");
                PrefabData result = LoadPrefabUnity(files, __state, root, __state.FileName_To_AssetIDDict[Prefab]); //did this so something can be done with it later.
                
                Msg("End prefab import");
            }

            Msg("end post import patch unitypackage");
            return files;
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