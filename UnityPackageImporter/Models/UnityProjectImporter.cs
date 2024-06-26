﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.FinalIK;
using UnityPackageImporter.Models;


namespace UnityPackageImporter;

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
    public World world;
    public ReadOnlyDictionary<string, string> ListOfPrefabs;

    public UnityProjectImporter(IEnumerable<string> files, Dictionary<string, string> AssetIDDict, Dictionary<string, string> ListOfPrefabs, Dictionary<string, string> ListOfMetas, Dictionary<string, string> ListOfUnityScenes, Slot root, Slot assetsRoot, World world)
    {
        this.files = files as List<string>;
        this.importTaskAssetRoot = assetsRoot;
        this.root = root;

        // These are read only, since they're for reference only. this allows us to be thread safe since we should only be reading not writing.
        this.ListOfPrefabs = new ReadOnlyDictionary<string, string>(ListOfPrefabs);
        this.ListOfMetas = new ReadOnlyDictionary<string, string>(ListOfMetas);
        this.AssetIDDict = new ReadOnlyDictionary<string, string>(AssetIDDict);
        this.ListOfUnityScenes = new ReadOnlyDictionary<string, string>(ListOfUnityScenes);
        this.world = world;
    }

    public async Task StartImports()
    {
        await default(ToBackground);
        UnityPackageImporter.Msg("Start Project Importing for unitypackage");

        // I feel so smart making the wait all import fbx tasks code. - @989onan
        await default(ToWorld);
        IEnumerable<FileImportTaskScene> fbx_tasks = FillFBXFiles();
        
        await Task.WhenAll(fbx_tasks.Select(task => task.RunnerWrapper()).ToArray());
        await default(ToBackground);
        // Now we have a full list of meta files and prefabs regarding this import file list from our prefix (where ever this is even if not a unity package folder) we now begin the hard part *drums* making the files go onto the model! 
        List<IUnityStructureImporter> unityImportTasks = new List<IUnityStructureImporter>();
        int total = this.ListOfPrefabs.Count + this.ListOfUnityScenes.Count;
        int rowSize = MathX.Max(1, MathX.CeilToInt(MathX.Sqrt((float)total)));
        
        float3 GlobalPosition = new float3(0,0,0);
        floatQ GlobalRotation = new floatQ(0, 0, 0, 1);

        await default(ToWorld);
        this.world.LocalUser.GetPointInFrontOfUser(out GlobalPosition, out GlobalRotation, null, null, 0.7f,true);
        await default(ToBackground);
        int counter = 0;

        foreach (KeyValuePair<string,string> Prefab in this.ListOfPrefabs)
        {
            UnityPackageImporter.Msg("create prefab import task obj for prefab \"" + Prefab.Value + "\"");
            await default(ToWorld);
            unityImportTasks.Add(new UnityPrefabImportTask((GlobalRotation * UniversalImporter.GridOffset(ref counter, rowSize)) + GlobalPosition, root, Prefab, this));
            await default(ToBackground);
        }

        foreach(KeyValuePair<string, string> Scene in this.ListOfUnityScenes)
        {
            UnityPackageImporter.Msg("create scene import task obj for scene \""+ Scene.Value+ "\"");
            await default(ToWorld);
            unityImportTasks.Add(new UnitySceneImportTask((GlobalRotation * UniversalImporter.GridOffset(ref counter, rowSize)) + GlobalPosition, root, Scene, this));
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

    private IEnumerable<FileImportTaskScene> FillFBXFiles()
    {
        int total = 0;
        
        foreach (KeyValuePair<string,string> pair in AssetIDDict)
        {
            string[] filename = pair.Value.Split('.');
            if(new string[]{"fbx"}.Contains(filename[filename.Length-1].ToLower())){

                if (!SharedImportedFBXScenes.ContainsKey(pair.key))
                {
                    total++;          
                }
            }     
        }

        int rowSize = MathX.Max(1, MathX.CeilToInt(MathX.Sqrt((float)total)));
        int counter = 0;
        this.world.LocalUser.GetPointInFrontOfUser(out float3 GlobalPosition, out floatQ GlobalRotation, null, null, 0.7f, true);
        foreach (KeyValuePair<string, string> pair in AssetIDDict)
        {
            string[] filename = pair.Value.Split('.');
            if (new string[] { "fbx" }.Contains(filename[filename.Length - 1].ToLower()))
            {
                if (!SharedImportedFBXScenes.ContainsKey(pair.key))
                {
                    UnityPackageImporter.Debug("now importing \"" + pair.Value + "\" for later use by prefabs and scenes!");
                    FileImportTaskScene importtask = new FileImportTaskScene(this.root, pair.key, this, this.AssetIDDict[pair.key], (GlobalRotation * UniversalImporter.GridOffset(ref counter, rowSize)) + GlobalPosition);
                    this.SharedImportedFBXScenes.Add(pair.key, importtask);
                    yield return importtask;

                }
            }
        }
        
        yield break;   
    }

    // This is static for a reason to be shared, don't use any fields from this importer that aren't static, and make sure to use locking to be thread safe
    public static async Task SettupHumanoid(FileImportTaskScene task, Slot FBXRoot, bool needsScaleComp)
    {
        UnityPackageImporter.Msg("checking if this FBX is a humanoid");
        Slot taskSlot = FBXRoot;

        BipedRig biped = taskSlot.AttachComponent<BipedRig>();
        await task.metafile.ScanFile(task, FBXRoot);
        await task.metafile.GenerateComponents(biped);
        
        // EXPLAINATION OF THIS CODE:
        // We are using the froox engine data made from assimp to force our model onto what we generated instead of
        // What froox engine made. This is better than asking froox engine to fully import the model for us
        // We get more control this way.
        if (biped.IsBiped) {

            await default(ToWorld);

            Slot movecenter = taskSlot.Parent.AddSlot(taskSlot.Name + " - Move Me With This!");
            movecenter.TRS = taskSlot.TRS;
            taskSlot.SetParent(movecenter);

            Rig rig = taskSlot.GetComponent<Rig>();
            if (rig == null)
            {
                UnityPackageImporter.Msg("rig missing, attaching.");
                UnityPackageImporter.Msg("slot to string is: " + taskSlot.ToString());
                rig = taskSlot.AttachComponent<Rig>();
            }

            UnityPackageImporter.Msg("Finding if we set up VRIK and are biped.");
            if (biped.IsBiped)
            {
                UnityPackageImporter.Msg("We have not set up vrik!");

                // Put our stuff under a slot called rootnode so froox engine can set this model up as an avatar
                await default(ToWorld);
                Slot rootnode = FBXRoot; // Intentional - @989onan
               
                UnityPackageImporter.Msg("Scaling up/down armature to file's global scale.");
                //Make the bone distances bigger since this model's file may have been exported 100X smaller
                await default(ToWorld);
                foreach (Slot slot in rootnode.GetAllChildren(false).ToArray())
                {
                    if (null == slot.GetComponent<FrooxEngine.SkinnedMeshRenderer>())
                    {
                        UnityPackageImporter.Msg("adding bone " + slot.Name +" with scale \""+ task.metafile.GlobalScale + "\"");
                        slot.LocalPosition *= needsScaleComp?task.metafile.GlobalScale:1;
                        rig.Bones.AddUnique(slot);
                    }

                    UnityPackageImporter.Msg("creating bone colliders for bone " + slot.Name);
                    BodyNode node = BodyNode.NONE;
                    try
                    {
                        node = biped.Bones.FirstOrDefault(i => i.Value.Target.Name.Equals(slot.Name)).key;
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
                float num = FBXRoot.ComputeBoundingBox(true, FBXRoot, null, null).Size.y/1.8f;

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
                //this is here to initialize our Rig's biped forward at the end of the import for VRIK later on.
                biped.GuessForwardFlipped();
                try
                {
                    biped.DetectHandRigs();
                }
                catch (Exception e)
                {
                    UnityPackageImporter.Msg("Error when detecting hand rigs for model \"" + task.file + "\"");
                    UnityPackageImporter.Msg(e.Message,e.StackTrace);
                }
                vrik.Initiate();//create our vrik component using our custom biped rig as our humanoid. Since it's on the same slot.

                //set our ik draggables up so people can play with the model and know it worked. - @989onan
                //ps, stolen from FrooxEngine decompiled code.
                Slot slot3 = biped[BodyNode.Head];
                Slot slot4 = biped[BodyNode.Hips];
                Slot slot5 = biped[BodyNode.LeftHand];
                Slot slot6 = biped[BodyNode.RightHand];
                Slot slot7 = biped[BodyNode.LeftFoot];
                Slot slot8 = biped[BodyNode.RightFoot];
                Slot slot9 = biped.TryGetBone(BodyNode.LeftToes);
                Slot slot10 = biped.TryGetBone(BodyNode.RightToes);
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