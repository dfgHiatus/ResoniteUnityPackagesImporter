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

namespace UnityPackageImporter
{
    public class UnityStructureImporter
    {

        public Dictionary<string, string> ListOfMetas;
        public Slot importTaskAssetRoot;
        public Dictionary<ulong, string> MeshRendererID_To_FileGUID = new Dictionary<ulong, string>();
        public List<FileImportHelperTaskMaterial> TasksMaterials = new List<FileImportHelperTaskMaterial>();
        public List<Slot> oldSlots = new List<Slot>();

        public List<string> importedFBXScenes = new List<string>();

        public Dictionary<string, string> AssetIDDict;

        public Dictionary<string, string> ListOfUnityScenes;

        public List<string> files;
        public Slot root;
        public Dictionary<ulong, IUnityObject> unityprefabimports = new Dictionary<ulong, IUnityObject>();
        public Dictionary<ulong, IUnityObject> unitysceneimports = new Dictionary<ulong, IUnityObject>();
        public Dictionary<string, string> ListOfPrefabs;


        public UnityStructureImporter(IEnumerable<string> files, Dictionary<string, string> AssetIDDict, Dictionary<string, string> ListOfPrefabs, Dictionary<string, string> ListOfMetas, Dictionary<string, string> ListOfUnityScenes)
        {
            this.files = files as List<string>;
            
            
            this.ListOfPrefabs = ListOfPrefabs;
            this.ListOfMetas = ListOfMetas;
            this.AssetIDDict = AssetIDDict;
            this.ListOfUnityScenes = ListOfUnityScenes;
        }

        public async Task startImports(Slot root, Slot assetsRoot)
        {
            this.importTaskAssetRoot = assetsRoot;
            this.root = root;
            await default(ToBackground);
            UnityPackageImporter.Msg("Start Prefab Importing unitypackage");
            //skip if raw files since we didn't do our setup and the user wants raw files.
            
            

            //now we have a full list of meta files and prefabs regarding this import file list from our prefix (where ever this is even if not a unity package folder) we now begin the hard part
            // *drums* making the files go onto the model! 

            foreach (var Prefab in this.ListOfPrefabs)
            {

                UnityPackageImporter.Msg("Start prefab import");
                await LoadPrefabUnity(Prefab); //did this so something can be done with it later.

                UnityPackageImporter.Msg("End prefab import");
            }
            UnityPackageImporter.Msg("end Prefab Importing patch unitypackage, starting on scenes");
            foreach(var Scene in this.ListOfUnityScenes)
            {
                UnityPackageImporter.Msg("Start scene import");
                await LoadSceneUnity(Scene); //did this so something can be done with it later.

                UnityPackageImporter.Msg("End scene import");
            }
        }

        private async Task LoadSceneUnity(KeyValuePair<string, string> SceneID)
        {
            await default(ToWorld);
            var SceneSlot = Engine.Current.WorldManager.FocusedWorld.AddSlot(Path.GetFileName(SceneID.Value));
            SceneSlot.SetParent(this.root, false);
            await default(ToBackground);
            //begin the parsing of our prefabs.
            //begin the parsing of our prefabs.

            //parse loop
            //now using the power of yaml we can make this a bit more reliable and hopefully smaller.
            //reading unity prefabs as yaml allows us to much more easily obtain the data we need.
            //Unity yamls are different, but with a little trickery we can still read them with a library.



            using var sr = File.OpenText(SceneID.Value);

            var deserializer = new DeserializerBuilder().WithNodeTypeResolver(new UnityNodeTypeResolver()).IgnoreUnmatchedProperties().Build();

            var parser = new Parser(sr);

            StringBuilder debugScene = new StringBuilder();
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
                    unitysceneimports.Add(doc.id, doc);
                }
                catch (Exception e)
                {
                    UnityPackageImporter.Msg("Couldn't evaluate node type. stacktrace below");
                    UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    try
                    {
                        IUnityObject doc = new FrooxEngineRepresentation.GameObjectTypes.NullType();
                        doc.id = UnityNodeTypeResolver.anchor;
                        unitysceneimports.Add(doc.id, doc);
                    }
                    catch (ArgumentException e2)
                    {/*idc.*/
                        UnityPackageImporter.Msg("Duplicate key probably. just ignore this.");
                        UnityPackageImporter.Warn(e2.Message + e2.StackTrace);
                    }

                }



            }

            //some debugging for the user to show them it worked or failed.

            UnityPackageImporter.Msg("Loaded " + unitysceneimports.Count.ToString() + " Unity objects/components/prefabs/lights ETC!");

            //find FBX's in our scene that need importing, so we can import them and then attach our prefab objects to it.
            await default(ToWorld);
            foreach (var obj in unitysceneimports)
            {
                if (obj.Value.m_CorrespondingSourceObject.guid != "" && this.AssetIDDict.ContainsKey(obj.Value.m_CorrespondingSourceObject.guid) && !importedFBXScenes.Contains(obj.Value.m_CorrespondingSourceObject.guid))
                {
                    this.importedFBXScenes.Add(obj.Value.m_CorrespondingSourceObject.guid);
                    UnityPackageImporter.Debug("now importing \"" + this.AssetIDDict[obj.Value.m_CorrespondingSourceObject.guid] + "\" for the scene +\""+ SceneID.Value + "\"!");
                    await new FileImportTaskScene(SceneSlot, obj.Value.m_CorrespondingSourceObject.guid, this, this.AssetIDDict[obj.Value.m_CorrespondingSourceObject.guid]).runnerWrapper();
                }
            }

            //instanciate our objects to generate our prefab entirely, using the ids we assigned ealier to identify our prefab elements in our list.

            await default(ToWorld);
            foreach (var obj in unitysceneimports)
            {
                await obj.Value.instanciateAsync(unitysceneimports, this);
                debugScene.Append(obj.Value.ToString());
            }
            await default(ToBackground);

            List<IUnityObject> destroythese = new List<IUnityObject>();

            foreach (var obj in unitysceneimports)
            {
                if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.Transform))
                {
                    FrooxEngineRepresentation.GameObjectTypes.Transform trans = obj.Value as FrooxEngineRepresentation.GameObjectTypes.Transform;
                    if (trans != null)
                    {
                        if (trans.m_FatherID == 0)
                        {
                            destroythese.Add(trans);
                            destroythese.Add(unitysceneimports[trans.m_GameObjectID]);
                        }
                    }
                }
            }

            //getting rid of objects that should go under the prefab slot.
            foreach (var obj in destroythese)
            {
                obj.instanciated = false;
                if (obj.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.GameObject))
                {
                    FrooxEngineRepresentation.GameObjectTypes.GameObject gameobj = obj as FrooxEngineRepresentation.GameObjectTypes.GameObject;
                    await default(ToWorld);
                    foreach (Slot prefabImmediateChild in gameobj.frooxEngineSlot.Children.ToArray())
                    {
                        prefabImmediateChild.SetParent(SceneSlot, false);
                    }
                    gameobj.frooxEngineSlot.Destroy();
                    await default(ToBackground);
                }
                unitysceneimports.Remove(obj.id);
            }

            await default(ToBackground);
            UnityPackageImporter.Debug("now debugging every object after instanciation!");
            UnityPackageImporter.Debug(debugScene.ToString());
            UnityPackageImporter.Msg("Yaml generation done");
            UnityPackageImporter.Msg("Scene finished!");
        }

        private async Task LoadPrefabUnity(KeyValuePair<string, string> PrefabID)
        {




            await default(ToWorld);
            var prefabslot = Engine.Current.WorldManager.FocusedWorld.AddSlot(Path.GetFileName(PrefabID.Value));
            prefabslot.SetParent(this.root, false);
            await default(ToBackground);
            //begin the parsing of our prefabs.
            //begin the parsing of our prefabs.

            //parse loop
            //now using the power of yaml we can make this a bit more reliable and hopefully smaller.
            //reading unity prefabs as yaml allows us to much more easily obtain the data we need.
            //Unity yamls are different, but with a little trickery we can still read them with a library.



            using var sr = File.OpenText(PrefabID.Value);

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
                    unityprefabimports.Add(doc.id, doc);
                }
                catch (Exception e)
                {
                    UnityPackageImporter.Msg("Couldn't evaluate node type. stacktrace below");
                    UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    try
                    {
                        IUnityObject doc = new FrooxEngineRepresentation.GameObjectTypes.NullType();
                        doc.id = UnityNodeTypeResolver.anchor;
                        unityprefabimports.Add(doc.id, doc);
                    }
                    catch (ArgumentException e2)
                    {/*idc.*/
                        UnityPackageImporter.Msg("Duplicate key probably. just ignore this.");
                        UnityPackageImporter.Warn(e2.Message + e2.StackTrace);
                    }

                }



            }

            //some debugging for the user to show them it worked or failed.

            UnityPackageImporter.Msg("Loaded " + unityprefabimports.Count.ToString() + " Unity objects/components/meshes!");



            //instanciate our objects to generate our prefab entirely, using the ids we assigned ealier to identify our prefab elements in our list.
            await default(ToWorld);
            foreach (var obj in unityprefabimports)
            {
                await obj.Value.instanciateAsync(unityprefabimports, this);
                debugPrefab.Append(obj.Value.ToString());
            }
            await default(ToBackground);

            List<IUnityObject> destroythese = new List<IUnityObject>();

            foreach (var obj in unityprefabimports)
            {
                if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.Transform))
                {
                    FrooxEngineRepresentation.GameObjectTypes.Transform trans = obj.Value as FrooxEngineRepresentation.GameObjectTypes.Transform;
                    if (trans != null)
                    {
                        if (trans.m_FatherID == 0)
                        {
                            destroythese.Add(trans);
                            destroythese.Add(unityprefabimports[trans.m_GameObjectID]);
                        }
                    }
                }
            }

            //getting rid of objects that should go under the prefab slot.
            foreach (var obj in destroythese)
            {
                obj.instanciated = false;
                if (obj.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.GameObject))
                {
                    FrooxEngineRepresentation.GameObjectTypes.GameObject gameobj = obj as FrooxEngineRepresentation.GameObjectTypes.GameObject;
                    await default(ToWorld);
                    foreach (Slot prefabImmediateChild in gameobj.frooxEngineSlot.Children.ToArray())
                    {
                        prefabImmediateChild.SetParent(prefabslot, false);
                    }
                    gameobj.frooxEngineSlot.Destroy();
                    await default(ToBackground);
                }
                unityprefabimports.Remove(obj.id);
            }



            UnityPackageImporter.Debug("now debugging every object after instanciation!");
            UnityPackageImporter.Debug(debugPrefab.ToString());



            UnityPackageImporter.Msg("Yaml generation done");
            UnityPackageImporter.Msg("Importing models");


            await AttachAssetsToPrefab(prefabslot);
            await default(ToBackground);
            UnityPackageImporter.Msg("Prefab finished!");
        }

        private async Task AttachAssetsToPrefab(Slot root)
        {
            

            await default(ToWorld);
            List<ImportMeshTask> Tasks = new List<ImportMeshTask>();

            List<string> StartedImports_GUIDs = new List<string>();

            UnityPackageImporter.Msg("Generating tasks for imports for this prefab: " + root.ToString());
            foreach (var requestedTask in MeshRendererID_To_FileGUID)
            {
                FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer requestingMeshRenderer = this.unityprefabimports[requestedTask.Key] as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer;
                ulong meshid = ulong.Parse(requestingMeshRenderer.m_Mesh["fileID"]);


                ulong meshRendererID = requestingMeshRenderer.id;
                if (!StartedImports_GUIDs.Contains(requestedTask.Value))
                {
                    for (int i = 0; i < 100; i++)
                    {
                        
                    }
                    if (this.AssetIDDict.ContainsKey(requestedTask.Value))
                    {
                        StartedImports_GUIDs.Add(requestedTask.Value);
                        
                        UnityPackageImporter.Msg("Generating tasks for import: " + requestedTask.Value);
                        await FrooxEngineBootstrap.LogStream.FlushAsync();
                        Tasks.Add(new ImportMeshTask(requestingMeshRenderer, new FileImportTask(root, requestedTask.Value, this, this.AssetIDDict[requestedTask.Value]), root, this));

                    }
                    else
                    {
                        UnityPackageImporter.Msg("TASK DOES NOT HAVE FILE: " + requestedTask.Value);
                        await FrooxEngineBootstrap.LogStream.FlushAsync();
                    }
                    
                    
                }
                else
                {
                    //create an import mesh task sharing the same file import task so we can share fbx imports.
                    Tasks.Add(new ImportMeshTask(requestingMeshRenderer, Tasks.Find(i => i.fileImportTask.assetID == requestedTask.Value).fileImportTask, root, this));
                }
            }

            UnityPackageImporter.Msg("waiting on file import tasks");
            await FrooxEngineBootstrap.LogStream.FlushAsync();
            await Task.WhenAll(Tasks.Select(i => i.fileImportTask.runnerWrapper()));

            UnityPackageImporter.Msg("Waiting on asset mesh and material attaching import tasks now...");
            await FrooxEngineBootstrap.LogStream.FlushAsync();
            await Task.WhenAll(Tasks.Select(i => i.ImportMeshTaskRunner()));

            UnityPackageImporter.Msg("Starting on final parts");
            await FrooxEngineBootstrap.LogStream.FlushAsync();
            await default(ToWorld);
            oldSlots = new List<Slot>();
            foreach (ImportMeshTask task in Tasks)
            {
                await default(ToWorld);
                UnityPackageImporter.Msg("Start task finalization");
                UnityPackageImporter.Msg("Task is valid?" + (task != null).ToString());

                UnityPackageImporter.Msg("Grabbing Task Slot for task " + task.requestingMeshRenderer.createdMeshRenderer.Slot.Name);
                Slot taskSlot = task.PrefabRoot;
                UnityPackageImporter.Msg("is task slot valid? " + (null != taskSlot).ToString());
                UnityPackageImporter.Msg("is task prefab root slot valid? " + (null != taskSlot).ToString());

                //EXPLAINATION OF THIS CODE:
                //we are using the froox engine data made from assimp to force our model onto what we generated instead of
                //what froox engine made. This is better than asking froox engine to fully import the model for us
                //we get more control this way.



                Rig rig = taskSlot.GetComponent<Rig>();
                if (rig == null)
                {
                    UnityPackageImporter.Msg("rig missing, attaching.");
                    UnityPackageImporter.Msg("slot to string is: " + taskSlot.ToString());
                    rig = taskSlot.AttachComponent<Rig>();
                }
                await default(ToBackground);

                UnityPackageImporter.Msg("finding this task's Prefab Data");

                await default(ToWorld);
                //var meshname = taskSlot.Name;
                var foundvrik = null != taskSlot.GetComponent(typeof(VRIK));
                MetaDataFile metadata = new MetaDataFile();
                if (!task.fileImportTask.isBiped.HasValue)
                {
                    UnityPackageImporter.Msg("checking biped data once for the first time, and only once for file: \"" + task.fileImportTask.file + "\"");
                    UnityPackageImporter.Msg("Reading this Model File's MetaData");
                    //this creates our biped rig under the slot we specify. in this case, it is the root.
                    //it scans the file's metadata to make it work.
                    //this MetaDataFile object class gives us access to the biped rig component directly if desired
                    //but we're using it to also get our global scale.
                    await default(ToBackground);
                    await metadata.ScanFile(task.fileImportTask.file + UnityPackageImporter.UNITY_META_EXTENSION, taskSlot);
                    await default(ToWorld);
                    UnityPackageImporter.Msg("returning to main thread and checking if we found a biped rig.");
                    task.fileImportTask.isBiped = metadata.modelBoneHumanoidAssignments.IsBiped;
                }
                await default(ToBackground);

                await default(ToWorld);
                UnityPackageImporter.Msg("Finding if we set up VRIK and are biped.");
                //this has value shouldn't throw an error because it was assigned above.
                if (!foundvrik && (task.fileImportTask.isBiped.Value))
                {
                    UnityPackageImporter.Msg("We have not set up vrik!");


                    //put our stuff under a slot called rootnode so froox engine can set this model up as an avatar
                    await default(ToWorld);
                    Slot rootnode = taskSlot.AddSlot("RootNode");
                    
                    foreach (Slot prefabImmediateChild in taskSlot.Children.ToArray())
                    {
                        prefabImmediateChild.SetParent(rootnode, false);
                    }
                    rootnode.LocalScale *= metadata.GlobalScale;


                    UnityPackageImporter.Msg("Scaling up/down armature to file's global scale.");
                    //make the bone distances bigger since this model's file may have been exported 100X smaller
                    await default(ToWorld);
                    foreach (Slot slot in rootnode.GetAllChildren(false).ToArray())
                    {
                        if (null == slot.GetComponent<FrooxEngine.SkinnedMeshRenderer>())
                        {

                            UnityPackageImporter.Msg("scaling bone " + slot.Name);
                            slot.LocalPosition /= metadata.GlobalScale;
                        }


                        UnityPackageImporter.Msg("creating bone colliders for bone " + slot.Name);
                        BodyNode node = BodyNode.NONE;
                        try
                        {
                            node = metadata.modelBoneHumanoidAssignments.Bones.FirstOrDefault(i => i.Value.Target.Name.Equals(slot.Name)).key;
                        }
                        catch (Exception) { } //this is to catch key not found so we shouldn't handle this.

                        if (BodyNode.NONE != node)
                        {
                            UnityPackageImporter.Msg("finding length of bone " + slot.Name);
                            float3 v = float3.Zero;
                            UnityPackageImporter.Msg("finding bone length " + slot.Name);
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



                    UnityPackageImporter.Msg("attaching VRIK");
                    await default(ToWorld);
                    VRIK vrik = taskSlot.AttachComponent<VRIK>();
                    await default(ToBackground);
                    UnityPackageImporter.Msg("Initializing VRIK");
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
            UnityPackageImporter.Msg("Finished Handling Lagged Import Tasks");
        }
    }
}