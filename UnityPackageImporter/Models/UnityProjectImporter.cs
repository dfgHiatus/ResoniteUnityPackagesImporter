using Elements.Core;
using FrooxEngine;
using FrooxEngine.FinalIK;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.FrooxEngineRepresentation;
using UnityPackageImporter.Models;


namespace UnityPackageImporter
{
    public class UnityProjectImporter
    {

        public ReadOnlyDictionary<string, string> ListOfMetas;
        public readonly Slot importTaskAssetRoot;
        public ReadOnlyDictionary<ulong, string> MeshRendererID_To_FileGUID;
        public List<FileImportHelperTaskMaterial> TasksMaterials = new List<FileImportHelperTaskMaterial>();
        



        public Dictionary<string, FileImportTaskScene> SharedImportedFBXScenes = new Dictionary<string, FileImportTaskScene>();

        public ReadOnlyDictionary<string, string> AssetIDDict;

        public ReadOnlyDictionary<string, string> ListOfUnityScenes;

        public List<string> files;
        public Slot root;
        
        public ReadOnlyDictionary<string, string> ListOfPrefabs;

        public Slot CurrentSceneSlot { get; set; }

        public UnityProjectImporter(IEnumerable<string> files, Dictionary<string, string> AssetIDDict, Dictionary<string, string> ListOfPrefabs, Dictionary<string, string> ListOfMetas, Dictionary<string, string> ListOfUnityScenes, Slot root, Slot assetsRoot)
        {
            this.files = files as List<string>;
            this.importTaskAssetRoot = assetsRoot;
            this.root = root;

            //these are read only, since they're for reference only. this allows us to be thread safe since we should only be reading not writing.
            this.ListOfPrefabs = new ReadOnlyDictionary<string, string>(ListOfPrefabs);
            this.ListOfMetas = new ReadOnlyDictionary<string, string>(ListOfMetas);
            this.AssetIDDict = new ReadOnlyDictionary<string, string>(AssetIDDict);
            this.ListOfUnityScenes = new ReadOnlyDictionary<string, string>(ListOfUnityScenes);
        }

        public async Task startImports()
        {
            await default(ToBackground);
            UnityPackageImporter.Msg("Start Prefab Importing unitypackage");


            //I feel so smart making the wait all import fbx tasks code. - @989onan
            
            Task.WaitAll(fillFBXFiles().ToArray());
            
            //now we have a full list of meta files and prefabs regarding this import file list from our prefix (where ever this is even if not a unity package folder) we now begin the hard part
            // *drums* making the files go onto the model! 

            List<IUnityStructureImporter> unityImportTasks = new List<IUnityStructureImporter>();

            foreach (KeyValuePair<string,string> Prefab in this.ListOfPrefabs)
            {

                UnityPackageImporter.Msg("Start prefab import");
                unityImportTasks.Add(new UnityPrefabImportTask(root, Prefab));

                UnityPackageImporter.Msg("End prefab import");
            }
            UnityPackageImporter.Msg("end Prefab Importing patch unitypackage, starting on scenes");
            foreach(var Scene in this.ListOfUnityScenes)
            {
                UnityPackageImporter.Msg("Start scene import");

                unityImportTasks.Add(new UnitySceneImportTask(root, Scene));

                UnityPackageImporter.Msg("End scene import");
            }

            await Task.WhenAll(unityImportTasks.Select(task => task.StartImport()));


            foreach(FileImportTaskScene obj in this.SharedImportedFBXScenes.Values)
            {
                obj.FinishedFileSlot.Destroy();
            }


            SharedImportedFBXScenes.Clear();
            UnityPackageImporter.Msg("All finished!");
        }


        private IEnumerable<Task> fillFBXFiles()
        {

            foreach(KeyValuePair<string,string> pair in AssetIDDict)
            {
                string[] filename = pair.Value.Split('.');
                if(new string[]{"fbx"}.Contains(filename[filename.Length-2].ToLower())){

                    if (!SharedImportedFBXScenes.ContainsKey(pair.key))
                    {
                        UnityPackageImporter.Debug("now importing \"" + pair.Value + "\" for later use by prefabs and scenes!");
                        FileImportTaskScene importtask = new FileImportTaskScene(this.root, pair.key, this, this.AssetIDDict[pair.key]);
                        yield return importtask.runnerWrapper();
                        this.SharedImportedFBXScenes.Add(pair.key, importtask);
                    }
                }

                
            }
            yield break;
            
        }


        //this is static for a reason to be shared, don't use any fields from this importer that aren't static, and make sure to use locking to be thread safe
        public static async Task SettupHumanoid(string fbxfilesource, Slot FBXRoot)
        {



            //EXPLAINATION OF THIS CODE:
            //we are using the froox engine data made from assimp to force our model onto what we generated instead of
            //what froox engine made. This is better than asking froox engine to fully import the model for us
            //we get more control this way.


            await default(ToWorld);
            Slot taskSlot = FBXRoot;
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
            UnityPackageImporter.Msg("checking biped data once for the first time, and only once for file: \"" + fbxfilesource + "\"");
            UnityPackageImporter.Msg("Reading this Model File's MetaData");
            //this creates our biped rig under the slot we specify. in this case, it is the root.
            //it scans the file's metadata to make it work.
            //this MetaDataFile object class gives us access to the biped rig component directly if desired
            //but we're using it to also get our global scale.
            await default(ToBackground);
            await metadata.ScanFile(fbxfilesource + UnityPackageImporter.UNITY_META_EXTENSION, taskSlot);
            await default(ToWorld);
            UnityPackageImporter.Msg("returning to main thread and checking if we found a biped rig.");
            bool isBiped = metadata.modelBoneHumanoidAssignments.IsBiped;
            await default(ToBackground);

            await default(ToWorld);
            UnityPackageImporter.Msg("Finding if we set up VRIK and are biped.");
            //this has value shouldn't throw an error because it was assigned above.
            if (!foundvrik && (isBiped))
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
            else
            {
                UnityPackageImporter.Msg("this prefab is not biped. ");
            }
            await default(ToBackground);
        }

    }
}