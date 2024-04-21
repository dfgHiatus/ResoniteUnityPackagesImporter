using Assimp;
using Assimp.Configs;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using static FrooxEngine.ModelImporter;
using MonoMod.Utils;
using HashDepot;
using UnityPackageImporter.FrooxEngineRepresentation;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using YamlDotNet.Core;
using SkyFrost.Base;
using static UMP.Wrappers.WrapperStandalone;
using Elements.Assets;

namespace UnityPackageImporter.Models
{
    public class FileImportTaskScene
    {
        public ModelImportData data;
        public Dictionary<SourceObj, IUnityObject> FILEID_To_Slot_Pairs = new Dictionary<SourceObj, IUnityObject>(new SourceObjCompare());
        public string file;
        private UnityProjectImporter importer;
        public string assetID;
        public Slot importTaskAssetSlot;
        public bool import_finished = false;
        public MetaDataFile metafile;

        Slot targetSlot;
        public Slot FinishedFileSlot = null;
        public bool postprocessfinished = false;
        public bool? isBiped;
        public bool running;

        public FileImportTaskScene(Slot targetSlot, string assetID, UnityProjectImporter importer, string file)
        {
            this.targetSlot = targetSlot.AddSlot(Path.GetFileNameWithoutExtension(file)+UnityPackageImporter.UNITY_PREFAB_EXTENSION);

            this.file = file;
            this.importer = importer;
            this.importTaskAssetSlot = importer.importTaskAssetRoot.AddSlot("Assets - "+ Path.GetFileNameWithoutExtension(file) + UnityPackageImporter.UNITY_PREFAB_EXTENSION);


            this.assetID = assetID;
        }

        //https://stackoverflow.com/a/31492250
        //https://stackoverflow.com/a/10789196

        public Task runnerWrapper()
        {
            if (!this.running)
            {
                this.running = true;
                return ImportFileMeshes();
            }
            return new Task(() => UnityPackageImporter.Msg("Tried to run task again, task already running. This is not an error."));
        }

        private async Task ImportFileMeshes()
        {

            UnityPackageImporter.Msg("Start code block for file import for file " + file);
            await default(ToWorld);
            AssimpContext assimpContext = new AssimpContext();
            assimpContext.Scale = 1f; 
            assimpContext.SetConfig(new NormalSmoothingAngleConfig(66f));
            assimpContext.SetConfig(new TangentSmoothingAngleConfig(10f));
            PostProcessSteps postProcessSteps = PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.ImproveCacheLocality | PostProcessSteps.PopulateArmatureData | PostProcessSteps.GenerateUVCoords | PostProcessSteps.FindInstances | PostProcessSteps.FlipWindingOrder | PostProcessSteps.LimitBoneWeights;
            Assimp.Scene scene = null;
            await default(ToBackground);

            this.metafile = new MetaDataFile();
            

            UnityPackageImporter.Msg("Start assimp file import for file \"" + file+ "\" If your log stops here, then Assimp crashed like a drunk man and took the game with it.\"");
            FrooxEngineBootstrap.LogStream.Flush();

            await default(ToBackground);
            try
            {
                scene = assimpContext.ImportFile(this.file, postProcessSteps);
            }
            catch (Exception arg)
            {
                UnityPackageImporter.Error(string.Format("Exception when importing {0}:\n\n{1}", this.file, arg), false);
                FrooxEngineBootstrap.LogStream.Flush();
            }
            finally
            {
                assimpContext.Dispose();
            }
            
            UnityPackageImporter.Msg("Preprocessing scene for file " + file);
            FrooxEngineBootstrap.LogStream.Flush();
            PreprocessScene(scene);
            UnityPackageImporter.Msg("making model import data for file: " + file);
            
            this.data = new ModelImportData(file, scene, this.targetSlot, this.importTaskAssetSlot, ModelImportSettings.PBS(true, true, false, false, false, false), null);
            UnityPackageImporter.Msg("importing node into froox engine, file: " + file);
            FrooxEngineBootstrap.LogStream.Flush();

            await Task.WhenAll(ImportNodeAsync(scene.RootNode, targetSlot, data));

            UnityPackageImporter.Msg("retrieving scene root for file: " + file);
            FrooxEngineBootstrap.LogStream.Flush();
            this.FinishedFileSlot = this.targetSlot; //this is on purpose - @989onan
            UnityPackageImporter.Msg("scanning meta file for data");
            await this.metafile.ScanFile(this, this.FinishedFileSlot); //scanning rather than creating components is important here. also this needs to happen immedeatley after the assimp import is finished. - @989onan
            UnityPackageImporter.Msg("recurisively searching for slots in file: " + file);
            FrooxEngineBootstrap.LogStream.Flush();


            //find each child slot of the whole thing
            foreach(Node childofroot in scene.RootNode.Children)
            {
                FILEID_To_Slot_Pairs.AddRange(RecusiveFileIDSlotFinder(this.data, childofroot, data.TryGetSlot(scene.RootNode), this.assetID));
            }

            

            foreach (FrooxEngine.SkinnedMeshRenderer mesh in this.data.skinnedRenderers)
            {


                string calculatedpath = FindSlotPath(mesh.Slot, data.TryGetSlot(scene.RootNode));
                SourceObj identifier = findRealSource(mesh.Slot.Name, "SkinnedMeshRenderer", calculatedpath);

                //UnityPackageImporter.Msg($"{Calculated_fileid} was made from SkinnedMeshRenderer with a slot \"{mesh.Slot.Name}\" with path \"{calculatedpath}\"");
                FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer skinnedrenderer = new FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer();
                skinnedrenderer.createdMeshRenderer = mesh;

                await default(ToWorld);
                while (!mesh.Mesh.IsAssetAvailable)
                {
                    await default(NextUpdate);
                    //UnityPackageImporter.Msg("Waiting for mesh assets for: \"" + mesh.Slot.Name + "\"");
                }
                await default(ToWorld);
                //UnityPackageImporter.Msg("finished waiting for mesh assets for: \"" + mesh.Slot.Name + "\"");

                Dictionary<string, Slot> bonemappings = new Dictionary<string, Slot>();
                foreach (var bone in skinnedrenderer.createdMeshRenderer.Mesh.Asset.Data.Bones)
                {
                    bonemappings.Add(bone.Name, this.FinishedFileSlot.FindChildInHierarchy(bone.Name));
                }
                skinnedrenderer.createdMeshRenderer.SetupBones(bonemappings);
                skinnedrenderer.createdMeshRenderer.SetupBlendShapes();


                skinnedrenderer.createdMeshRenderer.Materials.Clear();
                //we will replace these missing ones later.
                for (int j = 0; j < skinnedrenderer.createdMeshRenderer.Mesh.Asset.Data.SubmeshCount; j++)
                {
                    UnlitMaterial missingmat = this.importTaskAssetSlot.FindChildOrAdd("Missing Material").GetComponentOrAttach<UnlitMaterial>();
                    missingmat.TintColor.Value = new Elements.Core.colorX(1, 0, 1, 1, Elements.Core.ColorProfile.Linear);
                    skinnedrenderer.createdMeshRenderer.Materials.Add().Target = missingmat;
                }


                FILEID_To_Slot_Pairs.Add(identifier, skinnedrenderer);
            }






            UnityPackageImporter.Msg("Finished task for file " + file);
            this.running = false;
        }


        private static Task ImportNodeAsync(Node node, Slot targetSlot, ModelImportData data)
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            targetSlot.StartCoroutine(ImportNodeWrapper(node, targetSlot, data, taskCompletionSource));
            return taskCompletionSource.Task;
        }

        private static IEnumerator<Context> ImportNodeWrapper(Node node, Slot targetSlot, ModelImportData data, TaskCompletionSource<bool> completion = null)
        {
            yield return Context.WaitFor(ImportNode(node, targetSlot, data));
            completion.SetResult(result: true);
        }

        public Dictionary<SourceObj, IUnityObject> RecusiveFileIDSlotFinder(ModelImportData data2, Node root, Slot SceneRoot, string assetID)
        {
            //UnityPackageImporter.Msg($"checking slot \"{root.Name}\"");
            FrooxEngineBootstrap.LogStream.Flush();

            Dictionary<SourceObj, IUnityObject> FILEID_into_Slot_Pairs = new Dictionary<SourceObj, IUnityObject>(new SourceObjCompare());

            Slot curnode = data2.TryGetSlot(root);


            

            //UnityPackageImporter.Msg($"{Calculated_fileid} was made from slot \"{curnode.Name}\" with path \"{gameobjpath}\"");
            FrooxEngineBootstrap.LogStream.Flush();
            FrooxEngineRepresentation.GameObjectTypes.GameObject calclatedobj = new FrooxEngineRepresentation.GameObjectTypes.GameObject();
            calclatedobj.instanciated = true;
            calclatedobj.frooxEngineSlot = curnode;
            calclatedobj.m_CorrespondingSourceObject = findRealSource(curnode.Name, "GameObject", FindSlotPath(curnode, SceneRoot));

            FILEID_into_Slot_Pairs.Add(calclatedobj.m_CorrespondingSourceObject, calclatedobj);

            
            FrooxEngineRepresentation.GameObjectTypes.Transform calclatedTransformobj = new FrooxEngineRepresentation.GameObjectTypes.Transform();
            calclatedTransformobj.parentHashedGameObj = calclatedobj;
            calclatedTransformobj.instanciated = true;
            calclatedTransformobj.m_CorrespondingSourceObject = findRealSource(curnode.Name, "Transform", FindSlotPath(curnode, SceneRoot));

            //UnityPackageImporter.Msg($"{Calculated_fileidtransform} was made from transform \"{curnode.Name}\" with path \"{transformpath}\"");

            FILEID_into_Slot_Pairs.Add(calclatedTransformobj.m_CorrespondingSourceObject, calclatedTransformobj);
            if (root.ChildCount > 0)
            {
                foreach(Node child in root.Children)
                {
                    FILEID_into_Slot_Pairs.AddRange(RecusiveFileIDSlotFinder(data2, child, SceneRoot, assetID));
                }
                
            }

            return FILEID_into_Slot_Pairs;
        }

        public SourceObj findRealSource(string thisname, string type, string rawpath)
        {
            SourceObj sourceObj = new SourceObj();
            string endingtype = "";
            if (!type.Equals("GameObject"))
            {
                endingtype = "/"+type;
            }
            string path = "Type:" + type + "->//RootNode/root" + rawpath + endingtype + "0";
            long calculated_gameobj_id = (long)XXHash.Hash64(Encoding.UTF8.GetBytes(path));
            sourceObj.fileID = calculated_gameobj_id;
            sourceObj.guid = this.assetID;
            sourceObj.type = 0;


            foreach (KeyValuePair<long, string> item in this.metafile.fileIDToRecycleName)
            {
                if (thisname == item.Value)
                {
                    sourceObj.fileID = item.Key;//in case it's already defined in the metafile, because unity is weird - @989onan
                }
            }
            return sourceObj;
        }


        public static string FindSlotPath(Slot child, Slot StopAt)
        {
            if(child == null || StopAt == null)
            {
                return string.Empty;
            }
            else
            {
                if(child.IsChildOf(StopAt))
                {
                    return FindSlotPathRecursive(child, StopAt, "/"+child.Name);

                }
                else
                {
                    return "/" + child.Name;
                }
            }

        }

        private static string FindSlotPathRecursive(Slot child, Slot StopAt, string curpath)
        {
            if (child.Parent == StopAt)
            {
                return curpath;
            }
            string path = "/"+child.Parent.Name + curpath;
            
            return FindSlotPathRecursive(child.Parent, StopAt, path);
        }




    }
}
