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

namespace UnityPackageImporter {

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
        private static ModConfiguration config;
        private static string cachePath = Path.Combine(Engine.Current.CachePath, "Cache", "DecompressedUnityPackages");

        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<bool> importAsRawFiles =
         new ModConfigurationKey<bool>("importAsRawFiles", "Import files as raw binaries.", () => false);
        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<bool> importText =
             new ModConfigurationKey<bool>("importText", "Import Text", () => true);
        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<bool> importTexture =
             new ModConfigurationKey<bool>("importTexture", "Import Textures", () => true);
        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<bool> importDocument =
             new ModConfigurationKey<bool>("importDocument", "Import Documents", () => true);
        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<bool> importMesh =
             new ModConfigurationKey<bool>("importMesh", "Import Meshes", () => true);
        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<bool> importPrefab =
             new ModConfigurationKey<bool>("importPrefab", "Import Prefabs", () => true);
        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<bool> importPointCloud =
             new ModConfigurationKey<bool>("importPointCloud", "Import Point Clouds", () => true);
        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<bool> importAudio =
             new ModConfigurationKey<bool>("importAudio", "Import Audio", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> importFont =
             new ModConfigurationKey<bool>("importFont", "Import Fonts", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> importVideo =
             new ModConfigurationKey<bool>("importVideo", "Import Videos", () => true);

        public static bool ImportPrefab()
        {
            return config.GetValue(importPrefab);
        }


        public override void OnEngineInit()
        {
            new Harmony("net.dfgHiatus.UnityPackageImporter").PatchAll();
            config = GetConfiguration();
            Directory.CreateDirectory(cachePath);
        }
    
        public static string[] DecomposeUnityPackages(string[] files)
        {
            var fileToHash = files.ToDictionary(file => file, GenerateMD5);
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



        /* 
        Maybe the importer could be made smarter to detect a Unity project with just the prefab's PC path as reference? Then use all of those files to find the dependencies I guess?
        Though, this is fine for now, since the package might have all the dependencies (sometimes)
        If the package doesn't, we just skip those files. Later, a unity project dependency finder should be implemented. So use a way to see the prefab is in a unity project and act from there.
        */
        [HarmonyPatch(typeof(UniversalImporter), "Import", typeof(AssetClass), typeof(IEnumerable<string>),
            typeof(World), typeof(float3), typeof(floatQ), typeof(bool))]
        public class UniversalImporterPatch
        {

            private struct shareddata
            {
                public Dictionary<string, string> FileName_To_AssetIDDict;
                public Dictionary<string, string> AssetIDDict;
                public List<string> ListOfMetas;
                public List<string> ListOfPrefabs;
            };


            static bool Prefix(ref IEnumerable<string> files)
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

                shareddata __state = new shareddata();


                List<string> scanthesefiles = new List<string>();


                //scan litterally everything selected with our code for importing prefabs
                scanthesefiles.AddRange(notUnityPackage);
                //add everything in the unity package for our file scan
                foreach (var dir in DecomposeUnityPackages(hasUnityPackage.ToArray()))
                    scanthesefiles.AddRange(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).ToArray());

                /*DebugMSG*/Msg("CALLING The pre import patch");
                scanthesefiles = startmethod(slot,
                    scanthesefiles,
                    config.GetValue(importAsRawFiles), out __state).ToList();

                scanthesefiles = EndMethod(slot, scanthesefiles, config.GetValue(importAsRawFiles), __state).ToList();


                //once we have removed the prefabs, now we let the original stuff go through so we have the files normally
                //idk if we really need this if the stuff above is going to eventually just import prefabs and textures already set up... - @989onan
                BatchFolderImporter.BatchImport(
                    slot,
                    allDirectoriesToBatchImport,
                    config.GetValue(importAsRawFiles));



                if (notUnityPackage.Count <= 0) return false;
                files = notUnityPackage.ToArray();
                return true;
            }



            private static IEnumerable<string> startmethod(Slot root, IEnumerable<string> files, bool forceUnknown, out shareddata __state)
            {
                bool shouldBeRaw = forceUnknown;
                Slot parentUnder = root;
                /*DebugMSG*/Msg("Start pre import patch");
                //remove the meta files from the rest of the code later on in the return statements, since we don't want to let the importer bring in fifty bajillion meta files...
                List<string> ListOfNotMetasAndPrefabs = new List<string>();
                foreach (var file in files)
                {
                    if (!(Path.GetExtension(file).ToLower() == UNITY_PREFAB_EXTENSION || Path.GetExtension(file).ToLower() == UNITY_META_EXTENSION))
                    {
                        ListOfNotMetasAndPrefabs.Add(file);
                    }
                }

                __state.ListOfPrefabs = new List<string>();
                __state.ListOfMetas = new List<string>();
                __state.AssetIDDict = new Dictionary<String, String>();
                __state.FileName_To_AssetIDDict = new Dictionary<String, String>();

                if (shouldBeRaw)
                {

                    return ListOfNotMetasAndPrefabs.ToArray();
                }

                //first we iterate over every file to find metas and prefabs

                //we make a dictionary that associates the GUID of unity files with their paths. The files given to us are in a cache, with the directories already structured properly and the names fixed.
                // all we do is read the meta file and steal the GUID from there to get our identifiers in the Prefabs

                foreach (var file in files)
                {
                    if (Path.GetExtension(file).ToLower() == UNITY_PREFAB_EXTENSION)
                    {
                        __state.ListOfPrefabs.Add(file);
                    }
                    if (Path.GetExtension(file).ToLower() == UNITY_META_EXTENSION)
                    {
                        string filename = file.Substring(0, file.Length - Path.GetExtension(file).Length); //since every meta is filename + extension + ".meta" we can cut off the extension and have the original file name and path.
                        string fileGUID = File.ReadLines(file).ToArray()[1].Split(':')[1].Trim();
                        __state.AssetIDDict.Add(fileGUID, filename); // the GUID is on the first line (not 0th) after a colon and space, so trim it to get id.
                        __state.FileName_To_AssetIDDict.Add(filename, fileGUID); //have a flipped one for laters. I know I'm not very efficient with this one.
                        __state.ListOfMetas.Add(file);
                    }
                }

                //now the files are found, we will use these in our PostFix. We want to yeet these from the normal FrooxEngine import process, but keep them for last
                //so we can read them and do some wizard magic with the files corrosponding with our prefabs at the end

                /*DebugMSG*/Msg("end preimport patch unitypackage");
                return ListOfNotMetasAndPrefabs.ToArray();
                
            }


            /*struct UnityTransform
            {
                floatQ rotation;
                float3 Position;
                float3 Scale;
                string entityID;
                string slotparentID;
                List<string> transformchildren;

            }*/


            //yay recursion!
            private static Slot LoadPrefabUnity(IEnumerable<String> files, shareddata __state, Slot parentUnder, string PrefabID)
            {
                String Prefab = __state.AssetIDDict.GetValueSafe(PrefabID);
                var prefabslot = Engine.Current.WorldManager.FocusedWorld.AddSlot(Path.GetFileNameWithoutExtension(Prefab));
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
                string typeofobj = "";
                string entityID = "";
                var otherid = "";
                var foundtransform = false;

                //particle system tags

                //we're gonna use an int with this, here's the key
                /*
                 * inStartColor = 0
                 * inStartSize = 1
                 * inGravityModifier = 2
                 * inEmissionModule = 3
                 */

                //int particletag = -1;

                //skinned mesh renderer tags
                bool inBoneSection = false;



                //orphan transforms temp storage
                Dictionary<string, string> OrphanTransforms_Child_To_Parent = new Dictionary<string, string>();


                Dictionary<string, IWorldElement> Entities = new Dictionary<string, IWorldElement>();
                Dictionary<string, string> EntityChildID_To_EntityParentID = new Dictionary<string, string>();

                foreach (string line in File.ReadLines(Prefab))
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
                                /*DebugMSG*/Msg("Start TransformComponent interpreting");
                                break;
                            case "GameObject":
                                /*DebugMSG*/Msg("Start GameObject interpreting");
                                if (!EntityChildID_To_EntityParentID.ContainsKey(entityID))
                                {
                                    Entities.Add(entityID, Engine.Current.WorldManager.FocusedWorld.AddSlot(""));
                                    foundtransform = false;
                                }
                                foundtransform = false;
                                break;
                            case "ParticleSystem":
                                /*DebugMSG*/Msg("Start ParticleSystem interpreting");
                                //particletag = -1;
                                break;
                            case "MeshFilter":
                                /*DebugMSG*/Msg("Start MeshFilter interpreting");
                                //nothing needed here yet

                                break;
                            case "MeshRenderer":
                                /*DebugMSG*/Msg("Start MeshRenderer interpreting");
                                //nothing needed here yet
                                break;
                            case "SkinnedMeshRenderer":
                                /*DebugMSG*/Msg("Start SkinnedMeshRenderer interpreting");
                                inBoneSection = false;
                                break;
                        }




                        continue;
                    }


                    //now we know what kind of entity/obj, time to parse it's data.
                    switch (typeofobj)
                    {
                        case ("GameObject"):


                            //this assumes transform is first in the component list which is a good assumption because it always forces itself at the top in unity.
                            //since we handle adding components within the component adding blocks along with the slot, if we add it here we can
                            //assume it will be there at the end of all of this.


                            //if only starts with worked with a switch
                            if (line.StartsWith("  - component") && foundtransform == false)
                            {
                                /*DebugMSG*/Msg("found transform component ref for game object, adding");
                                otherid = line.Split(':')[2].Split('}')[0].Trim();
                                if (!EntityChildID_To_EntityParentID.ContainsKey(otherid))
                                {
                                    EntityChildID_To_EntityParentID.Add(otherid, entityID);
                                }


                                
                                foundtransform = true;
                                break;
                            }
                            else if (line.StartsWith("  - component"))
                            {
                                /*DebugMSG*/Msg("found non transform component ref for game object, adding parent child relationship");
                                otherid = line.Split(':')[2].Split('}')[0].Trim();
                                if (!EntityChildID_To_EntityParentID.ContainsKey(otherid))
                                {
                                    EntityChildID_To_EntityParentID.Add(otherid, entityID);
                                }
                            }
                            else if (line.StartsWith("  m_Name: "))
                            {
                                /*DebugMSG*/Msg("found name for game object, setting");
                                ((Slot)Entities[entityID]).Name = line.Remove(0, "  m_Name: ".Length);
                            }
                            else if (line.StartsWith("  m_TagString: "))
                            {
                                /*DebugMSG*/Msg("found tag for game object, setting");
                                ((Slot)Entities[entityID]).Tag = line.Remove(0, "  m_TagString: ".Length); //idk if we need this, but it's cool.
                            }
                            else if (line.StartsWith("  m_IsActive: "))
                            {
                                /*DebugMSG*/Msg("found active for game object, setting");
                                ((Slot)Entities[entityID]).ActiveSelf = line.Remove(0, "  m_IsActive: ".Length).Trim().Equals("1");
                            }








                            break;
                        case ("Transform"):
                            //if only starts with worked with a switch
                            if (line.StartsWith("  m_GameObject: "))
                            {
                                /*DebugMSG*/Msg("found game object parent ref id, checking");
                                otherid = line.Split(':')[2].Split('}')[0].Trim();
                                /*DebugMSG*/Msg("id is \"" + otherid + "\"");
                                if (!EntityChildID_To_EntityParentID.ContainsKey(entityID))
                                {
                                    EntityChildID_To_EntityParentID.Add(entityID, otherid);
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

                                ((Slot)Entities[otherid]).LocalRotation = new floatQ(x, y, z, w);
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

                                ((Slot)Entities[otherid]).LocalPosition = new float3(x, y, z);


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
                                
                                ((Slot)Entities[otherid]).LocalScale = new float3(x, y, z);
                            }
                            else if (line.StartsWith("  m_Father:"))
                            {
                                
                                //finding this transform object's parent slot. if not, store to a temp array;

                                //here the child transform block could have been made before or after the transform, making finding the slot first impossible. We have to parent later if we can't find it.
                                
                                string parentTransformComponentID = line.Split(':')[2].Split('}')[0].Trim();

                                if (EntityChildID_To_EntityParentID.ContainsKey(parentTransformComponentID))
                                {
                                    ((Slot)Entities[otherid]).SetParent(((Slot)Entities[EntityChildID_To_EntityParentID[parentTransformComponentID]]), false);
                                }
                                else
                                {
                                    if (!OrphanTransforms_Child_To_Parent.ContainsKey(entityID))
                                    {
                                        OrphanTransforms_Child_To_Parent.Add(entityID, parentTransformComponentID);
                                    }
                                    
                                }

                                //this is garbage, but unity prefabs have forced my hand
                                //basically children transforms can appear after the transform tries to reference it
                                //in coding this is awful. So I have to iterate over freaking everything to find ones that were instantiated
                                //before their children. Pain. - @989onan
                                List<string> removelist = new List<string>();
                                foreach (var pair in OrphanTransforms_Child_To_Parent.AsParallel())
                                {
                                    if (pair.Value == entityID)
                                    {
                                        try
                                        {
                                            ((Slot)Entities[EntityChildID_To_EntityParentID[pair.Key]]).SetParent(((Slot)Entities[otherid]), false);
                                            removelist.Add(pair.Key);
                                        }
                                        catch (Exception e)
                                        {
                                            /*DebugMSG*/Msg(e.StackTrace);
                                        }
                                    }


                                }
                                foreach(string key in removelist)
                                {
                                    OrphanTransforms_Child_To_Parent.Remove(key);
                                }

                            }
                            break;


                        

                           
                        case ("ParticleSystem"):
                            if (line.StartsWith("  m_GameObject: "))
                            {
                                /*DebugMSG*/Msg("found game object parent ref id, checking");
                                otherid = line.Split(':')[2].Split('}')[0].Trim();
                                /*DebugMSG*/Msg("id is \"" + otherid+"\"");
                                if (!EntityChildID_To_EntityParentID.ContainsKey(entityID))
                                {
                                    EntityChildID_To_EntityParentID.Add(entityID, otherid);
                                }
                                if (!Entities.ContainsKey(otherid))

                                {//now since we add the game object if it appears before this component, and add it prematurely before it appears, now we can assemble 1/2 of the game object in both places
                                 //eventually it will be 100% complete at the end.
                                 //it's not great coding, but it works.
                                 // if we find the component after the game object is created, the slot will be created here and added to when we find the game object so can just add our component right after this.
                                 /*DebugMSG*/Msg("id is not made, creating slot");
                                    Entities.Add(otherid, Engine.Current.WorldManager.FocusedWorld.AddSlot(""));
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


                            ParticleSystem thisComponent = ((ParticleSystem)Entities[entityID]);


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
                        case ("MeshFilter"):


                            if (line.StartsWith("  m_GameObject: "))
                            {
                                /*DebugMSG*/Msg("found game object parent ref id, checking");
                                otherid = line.Split(':')[2].Split('}')[0].Trim();
                                /*DebugMSG*/Msg("id is \"" + otherid+"\"");
                                if (!EntityChildID_To_EntityParentID.ContainsKey(entityID))
                                {
                                    EntityChildID_To_EntityParentID.Add(entityID, otherid);
                                }
                                if (!Entities.ContainsKey(otherid))

                                {//now since we add the game object if it appears before this component, and add it prematurely before it appears, now we can assemble 1/2 of the game object in both places
                                 //eventually it will be 100% complete at the end.
                                 //it's not great coding, but it works.
                                 // if we find the component after the game object is created, the slot will be created here and added to when we find the game object so can just add our component right after this.
                                 /*DebugMSG*/Msg("id is not made, creating slot");
                                    Entities.Add(otherid, Engine.Current.WorldManager.FocusedWorld.AddSlot(""));
                                    break;
                                }
                            }

                            if (line.StartsWith("  m_Mesh: "))
                            {
                                /*DebugMSG*/Msg("found mesh ref id");
                                var meshid = line.Split(':')[3].Split(',')[0].Trim();//this is on purpose, because a line for the m_Mesh looks like this: "  m_Mesh: {fileID: 4300000, guid: d6ba91ccc11280d4da45a2d1c17d88ba, type: 3}"
                                /*DebugMSG*/Msg("mesh ref id is: \""+ meshid + "\"");
                                var meshtype = int.Parse(line.Split(':')[4].Split('}')[0].Trim()); //same here

                                if (meshtype == 3)
                                {
                                    //this will load the mesh, which is actually an fbx, and load it into the slot this component should be on.
                                    //yes this may run the importer again
                                    try
                                    {
                                        BatchFolderImporter.BatchImport(((Slot)Entities[otherid]), new List<String>(new String[] { __state.AssetIDDict[meshid] }), false /*<- here we want false because this code wouldn't run if it was false to begin with.*/);
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
                        case ("SkinnedMeshRenderer"):
                            if (inBoneSection)
                            {
                                if (line.StartsWith("  - {fileID: "))
                                {
                                    //this runs after the file is imported
                                    //though looking at sourcecode via DNSpy (which is okay according to TOS of Resonite), the importer runs on an async task, so this is annoying...



                                    // TODO: There needs to be a way to merge these bones with an fbx import of the object that this references.
                                    // that way we don't have to set our own IK and we can just drop in the objects parented under this object.
                                    // maybe make a duplicate of the fbx that gets imported that this skinned mesh renderer references -- 
                                    // and then drop our bones including children onto that one?
                                    // idk. Something can be done here. I sure don't know myself... - @989onan


                                    //some random code idk
                                    /*otherid = line.Split(':')[1].Split('}')[0].Trim();
                                    if (!Entities.ContainsKey(otherid))
                                    {
                                        Entities.Add(otherid, Engine.Current.WorldManager.FocusedWorld.AddSlot(""));
                                    }
                                    Slot bone = ((Slot)Entities[otherid]);

                                    List<Component> slotcomponents = new List<Component>();
                                    foreach(Component component in bone.Components)
                                    {
                                        slotcomponents.Add(component);
                                    }

                                    foreach(Component component in slotcomponents)
                                    {
                                        bone.MoveComponent(component);
                                    }*/



                                }
                            }



                            if (line.StartsWith("  m_GameObject: "))
                            {
                                /*DebugMSG*/Msg("found game object parent ref id, checking");
                                otherid = line.Split(':')[2].Split('}')[0].Trim();
                                /*DebugMSG*/Msg("id is \"" + otherid + "\"");
                                if (!EntityChildID_To_EntityParentID.ContainsKey(entityID))
                                {
                                    EntityChildID_To_EntityParentID.Add(entityID, otherid);
                                }
                                
                                if (!Entities.ContainsKey(otherid)){//now since we add the game object if it appears before this component, and add it prematurely before it appears, now we can assemble 1/2 of the game object in both places
                                 //eventually it will be 100% complete at the end.
                                 //it's not great coding, but it works.
                                 // if we find the component after the game object is created, the slot will be created here and added to when we find the game object so can just add our component right after this.
                                 /*DebugMSG*/Msg("id is not made, creating slot");
                                    Entities.Add(otherid, Engine.Current.WorldManager.FocusedWorld.AddSlot(""));
                                    break;
                                }
                            }
                            else if (line.StartsWith("  m_Mesh: "))
                            {
                                /*DebugMSG*/Msg("found mesh, parsing");
                                var meshid = line.Split(':')[3].Split(',')[0].Trim();//this is on purpose, because a line for the m_Mesh looks like this: "  m_Mesh: {fileID: 4300000, guid: d6ba91ccc11280d4da45a2d1c17d88ba, type: 3}"
                                /*DebugMSG*/Msg("mesh ref id is: \""+ meshid + "\"");
                                var meshtype = int.Parse(line.Split(':')[4].Split('}')[0].Trim()); //same here

                                if (meshtype == 3)
                                {
                                    //this will load the mesh, which is actually an fbx, and load it into the slot this component should be on.
                                    //yes this may run the importer again
                                    try
                                    {
                                        BatchFolderImporter.BatchImport(((Slot)Entities[otherid]), new List<String>(new String[] { __state.AssetIDDict[meshid] }), false /*<- here we want false because this code wouldn't run if it was false to begin with.*/);
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







                return prefabslot;
            }

            private static IEnumerable<String> EndMethod(Slot root, IEnumerable<String> files, bool shouldBeRaw, shareddata __state)
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
                    LoadPrefabUnity(files, __state, root, __state.FileName_To_AssetIDDict[Prefab]);
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
            return (config.GetValue(importText) && assetClass == AssetClass.Text) 
                || (config.GetValue(importTexture) && assetClass == AssetClass.Texture) 
                || (config.GetValue(importDocument) && assetClass == AssetClass.Document) 
                || (config.GetValue(importPointCloud) && assetClass == AssetClass.PointCloud) 
                || (config.GetValue(importAudio) && assetClass == AssetClass.Audio) 
                || (config.GetValue(importFont) && assetClass == AssetClass.Font) 
                || (config.GetValue(importVideo) && assetClass == AssetClass.Video) 
                || (config.GetValue(importMesh) && assetClass == AssetClass.Model && extension != ".xml") /* Handle an edge case where assimp will try to import .xml files as 3D models*/
                || (config.GetValue(importPrefab) && (extension == UNITY_PREFAB_EXTENSION || /*Add file if it is a prefab*/ (extension == UNITY_META_EXTENSION))) /* add the .meta files into the pile so we can read them to find models later.*/
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
            var hasher = MD5.Create();
            var stream = File.OpenRead(filepath);
            var hash = hasher.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
};