using Assimp;
using Elements.Assets;
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
using static FrooxEngine.MeshUploadHint;


namespace UnityPackageImporter
{
    public class UnityProjectImporter
    {

        public ReadOnlyDictionary<string, string> ListOfMetas;
        public readonly Slot importTaskAssetRoot;
        public List<FileImportHelperTaskMaterial> TasksMaterials = new List<FileImportHelperTaskMaterial>();
        



        public Dictionary<string, FileImportTaskScene> SharedImportedFBXScenes = new Dictionary<string, FileImportTaskScene>();

        public ReadOnlyDictionary<string, string> AssetIDDict;

        public ReadOnlyDictionary<string, string> ListOfUnityScenes;

        public List<string> files;
        public Slot root;
        
        public ReadOnlyDictionary<string, string> ListOfPrefabs;


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
            UnityPackageImporter.Msg("Start Project Importing for unitypackage");


            //I feel so smart making the wait all import fbx tasks code. - @989onan
            await default(ToWorld);
            await Task.WhenAll(fillFBXFiles().Select(task => task.runnerWrapper()).ToArray());
            await default(ToBackground);
            //now we have a full list of meta files and prefabs regarding this import file list from our prefix (where ever this is even if not a unity package folder) we now begin the hard part
            // *drums* making the files go onto the model! 

            List<IUnityStructureImporter> unityImportTasks = new List<IUnityStructureImporter>();

            foreach (KeyValuePair<string,string> Prefab in this.ListOfPrefabs)
            {

                UnityPackageImporter.Msg("create prefab import task obj for prefab \"" + Prefab.Value + "\"");
                await default(ToWorld);
                unityImportTasks.Add(new UnityPrefabImportTask(root, Prefab, this));
                await default(ToBackground);
            }
            foreach(var Scene in this.ListOfUnityScenes)
            {
                UnityPackageImporter.Msg("create scene import task obj for scene \""+ Scene.Value+ "\"");
                await default(ToWorld);
                unityImportTasks.Add(new UnitySceneImportTask(root, Scene, this));
                await default(ToBackground);
            }
            await default(ToWorld);
            await Task.WhenAll(unityImportTasks.Select(task => task.StartImport()));
            UnityPackageImporter.Msg("Finished project importing! Cleaning up...");
            await default(ToBackground);

            foreach (FileImportTaskScene obj in this.SharedImportedFBXScenes.Values)
            {
                await default(ToWorld);
                obj.FinishedFileSlot.Destroy();
                await default(ToBackground);
            }


            SharedImportedFBXScenes.Clear();
            UnityPackageImporter.Msg("All finished!");
        }


        private IEnumerable<FileImportTaskScene> fillFBXFiles()
        {

            foreach(KeyValuePair<string,string> pair in AssetIDDict)
            {
                string[] filename = pair.Value.Split('.');
                if(new string[]{"fbx"}.Contains(filename[filename.Length-1].ToLower())){

                    if (!SharedImportedFBXScenes.ContainsKey(pair.key))
                    {
                        UnityPackageImporter.Debug("now importing \"" + pair.Value + "\" for later use by prefabs and scenes!");
                        FileImportTaskScene importtask = new FileImportTaskScene(this.root, pair.key, this, this.AssetIDDict[pair.key]);
                        this.SharedImportedFBXScenes.Add(pair.key, importtask);
                        yield return importtask;
                    }
                }

                
            }
            yield break;
            
        }


        //this is static for a reason to be shared, don't use any fields from this importer that aren't static, and make sure to use locking to be thread safe
        public static async Task SettupHumanoid(FileImportTaskScene task, Slot FBXRoot)
        {
            UnityPackageImporter.Msg("checking if this FBX is a humanoid");
            Slot taskSlot = FBXRoot;

            await task.metafile.ScanFile(task, taskSlot); //we have to scan again here, since the slots may have changed names.
            await task.metafile.GenerateComponents(taskSlot);

            bool isBiped = task.metafile.modelBoneHumanoidAssignments.IsBiped;

            //EXPLAINATION OF THIS CODE:
            //we are using the froox engine data made from assimp to force our model onto what we generated instead of
            //what froox engine made. This is better than asking froox engine to fully import the model for us
            //we get more control this way.
            if (isBiped) {

                await default(ToWorld);
                
                Rig rig = taskSlot.GetComponent<Rig>();
                if (rig == null)
                {
                    UnityPackageImporter.Msg("rig missing, attaching.");
                    UnityPackageImporter.Msg("slot to string is: " + taskSlot.ToString());
                    rig = taskSlot.AttachComponent<Rig>();
                }
                await default(ToBackground); 

                




                await default(ToWorld);
                UnityPackageImporter.Msg("Finding if we set up VRIK and are biped.");
                if (isBiped)
                {
                    UnityPackageImporter.Msg("We have not set up vrik!");


                    //put our stuff under a slot called rootnode so froox engine can set this model up as an avatar
                    await default(ToWorld);
                    Slot rootnode = FBXRoot; //intentional - @989onan
                    


                    UnityPackageImporter.Msg("Scaling up/down armature to file's global scale.");
                    //make the bone distances bigger since this model's file may have been exported 100X smaller
                    await default(ToWorld);
                    foreach (Slot slot in rootnode.GetAllChildren(false).ToArray())
                    {
                        if (null == slot.GetComponent<FrooxEngine.SkinnedMeshRenderer>())
                        {

                            UnityPackageImporter.Msg("scaling bone " + slot.Name);
                            slot.LocalPosition *= task.metafile.GlobalScale*100;
                        }


                        UnityPackageImporter.Msg("creating bone colliders for bone " + slot.Name);
                        BodyNode node = BodyNode.NONE;
                        try
                        {
                            node = task.metafile.modelBoneHumanoidAssignments.Bones.FirstOrDefault(i => i.Value.Target.Name.Equals(slot.Name)).key;
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

                    Elements.Core.BoundingBox boundingBox = Elements.Core.BoundingBox.Empty();
                    
                    await default(ToWorld);
                    float num = FBXRoot.ComputeBoundingBox(false, FBXRoot, null, null).Size.y/1.8f;

                    rootnode.LocalScale /= new float3(num, num, num);
                    await default(ToBackground);


                    UnityPackageImporter.Msg("attaching VRIK");
                    await default(ToWorld);
                    
                    await default(ToBackground);
                    UnityPackageImporter.Msg("Initializing VRIK");
                    await default(ToWorld);
                    Grabbable componentInParents = rootnode.GetComponentInParents<Grabbable>(null, true, false);
                    if (componentInParents != null)
                    {
                        componentInParents.Destroy();
                    }
                    VRIK vrik = rootnode.AttachComponent<VRIK>(true, null);
                    vrik.Solver.SimulationSpace.Target = rootnode.Parent;
                    vrik.Solver.OffsetSpace.Target = rootnode.Parent;
                    vrik.Initiate();//create our vrik component using our custom biped rig as our humanoid. Since it's on the same slot.

                    //set our ik draggables up so people can play with the model and know it worked. - @989onan
                    //ps, stolen from FrooxEngine decompiled code.
                    Slot slot3 = task.metafile.modelBoneHumanoidAssignments[BodyNode.Head];
                    Slot slot4 = task.metafile.modelBoneHumanoidAssignments[BodyNode.Hips];
                    Slot slot5 = task.metafile.modelBoneHumanoidAssignments[BodyNode.LeftHand];
                    Slot slot6 = task.metafile.modelBoneHumanoidAssignments[BodyNode.RightHand];
                    Slot slot7 = task.metafile.modelBoneHumanoidAssignments[BodyNode.LeftFoot];
                    Slot slot8 = task.metafile.modelBoneHumanoidAssignments[BodyNode.RightFoot];
                    Slot slot9 = task.metafile.modelBoneHumanoidAssignments.TryGetBone(BodyNode.LeftToes);
                    Slot slot10 = task.metafile.modelBoneHumanoidAssignments.TryGetBone(BodyNode.RightToes);
                    ModelImporter.SetupDraggable(slot3, vrik.Solver, vrik.Solver.spine.IKPositionHead, vrik.Solver.spine.IKRotationHead, vrik.Solver.spine.PositionWeight);
                    ModelImporter.SetupDraggable(slot4, vrik.Solver, vrik.Solver.spine.IKPositionPelvis, vrik.Solver.spine.IKRotationPelvis, vrik.Solver.spine.PelvisPositionWeight);
                    ModelImporter.SetupDraggable(slot5, vrik.Solver, vrik.Solver.leftArm.IKPosition, vrik.Solver.leftArm.IKRotation, vrik.Solver.leftArm.PositionWeight);
                    ModelImporter.SetupDraggable(slot6, vrik.Solver, vrik.Solver.rightArm.IKPosition, vrik.Solver.rightArm.IKRotation, vrik.Solver.rightArm.RotationWeight);
                    ModelImporter.SetupDraggable(slot9 ?? slot7, vrik.Solver, vrik.Solver.leftLeg.IKPosition, vrik.Solver.leftLeg.IKRotation, vrik.Solver.leftLeg.PositionWeight);
                    ModelImporter.SetupDraggable(slot10 ?? slot8, vrik.Solver, vrik.Solver.rightLeg.IKPosition, vrik.Solver.rightLeg.IKRotation, vrik.Solver.rightLeg.PositionWeight);
                    taskSlot.AttachComponent<DestroyRoot>();
                    DynamicVariableSpace avatar = taskSlot.AttachComponent<DynamicVariableSpace>();
                    avatar.SpaceName.Value = "Avatar"; //hehe random bias, go! - @989onan
                    taskSlot.AttachComponent<ObjectRoot>();
                    await default(ToBackground);
                }
                else
                {
                    UnityPackageImporter.Msg("this prefab is not biped. ");
                }
            }
            else
            {
                UnityPackageImporter.Msg("this prefab is not biped. ");
            }
            await default(ToBackground);
        }

    }
}