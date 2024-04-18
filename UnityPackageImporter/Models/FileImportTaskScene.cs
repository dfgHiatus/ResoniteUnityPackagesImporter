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
using System.Linq;

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

        Slot targetSlot;
        public Slot FinishedFileSlot = null;
        public bool postprocessfinished = false;
        public bool? isBiped;
        public bool running;

        public FileImportTaskScene(Slot targetSlot, string assetID, UnityProjectImporter importer, string file)
        {
            this.targetSlot = targetSlot;
            this.file = file;
            this.importer = importer;
            this.importTaskAssetSlot = importer.importTaskAssetRoot.AddSlot("Assets - "+Path.GetFileName(file));


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
            assimpContext.Scale = 0.01f; //TODO: Grab file's scale from metadata
            assimpContext.SetConfig(new NormalSmoothingAngleConfig(66f));
            assimpContext.SetConfig(new TangentSmoothingAngleConfig(10f));
            PostProcessSteps postProcessSteps = PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.ImproveCacheLocality | PostProcessSteps.PopulateArmatureData | PostProcessSteps.GenerateUVCoords | PostProcessSteps.FindInstances | PostProcessSteps.FlipWindingOrder | PostProcessSteps.LimitBoneWeights;
            Assimp.Scene scene = null;

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
            this.FinishedFileSlot = data.TryGetSlot(scene.RootNode); //get the slot by the import data to make sure we have what we imported.
            UnityPackageImporter.Msg("recurisively searching for slots in file: " + file);
            FrooxEngineBootstrap.LogStream.Flush();


            //find each child slot of the whole thing
            foreach(Node childofroot in scene.RootNode.Children)
            {
                FILEID_To_Slot_Pairs.AddRange(RecusiveFileIDSlotFinder(this.data, childofroot, data.TryGetSlot(childofroot), this.assetID));
            }
            

            
            foreach(FrooxEngine.SkinnedMeshRenderer mesh in this.data.skinnedRenderers)
            {
                string calculatedpath = "Type:SkinnedMeshRenderer->//RootNode/root/" + mesh.Slot.Name + "/SkinnedMeshRenderer0";//we are assuming all meshes are under the RootNode assimp makes.
                long Calculated_fileid = (long)XXHash.Hash64(Encoding.UTF8.GetBytes(calculatedpath));
                UnityPackageImporter.Msg($"{Calculated_fileid} was made from SkinnedMeshRenderer with a slot \"{mesh.Slot.Name}\" with path \"{calculatedpath}\"");
                FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer skinnedrenderer = new FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer();
                skinnedrenderer.createdMeshRenderer = mesh;

                await default(ToWorld);
                while (!mesh.Mesh.IsAssetAvailable)
                {
                    await default(NextUpdate);
                    UnityPackageImporter.Msg("Waiting for mesh assets for: \"" + mesh.Slot.Name + "\"");
                }
                await default(ToWorld);
                UnityPackageImporter.Msg("finished waiting for mesh assets for: \"" + mesh.Slot.Name + "\"");

                SourceObj identifier = new SourceObj();
                identifier.fileID = Calculated_fileid;
                identifier.guid = this.assetID;
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

        public static Dictionary<SourceObj, IUnityObject> RecusiveFileIDSlotFinder(ModelImportData data2, Node root, Slot SceneRoot, string assetID)
        {
            UnityPackageImporter.Msg($"checking slot \"{root.Name}\"");
            FrooxEngineBootstrap.LogStream.Flush();

            Dictionary<SourceObj, IUnityObject> FILEID_into_Slot_Pairs = new Dictionary<SourceObj, IUnityObject>(new SourceObjCompare());

            Slot curnode = data2.TryGetSlot(root);

            string calculatedpath = FindSlotPath(curnode, SceneRoot);

            string gameobjpath = "Type:GameObject->//RootNode/root" + calculatedpath + "0";

            long Calculated_fileid = (long)XXHash.Hash64(Encoding.UTF8.GetBytes(gameobjpath));

            UnityPackageImporter.Msg($"{Calculated_fileid} was made from slot \"{curnode.Name}\" with path \"{gameobjpath}\"");
            FrooxEngineBootstrap.LogStream.Flush();
            FrooxEngineRepresentation.GameObjectTypes.GameObject calclatedobj = new FrooxEngineRepresentation.GameObjectTypes.GameObject();
            calclatedobj.instanciated = true;
            calclatedobj.frooxEngineSlot = curnode;

            SourceObj identifier1 = new SourceObj();
            identifier1.fileID = Calculated_fileid;
            identifier1.guid = assetID;

            FILEID_into_Slot_Pairs.Add(identifier1, calclatedobj);
            string transformpath = "Type:Transform->//RootNode/root" + calculatedpath + "/Transform0";

            long Calculated_fileidtransform = (long)XXHash.Hash64(Encoding.UTF8.GetBytes(transformpath));
            FrooxEngineRepresentation.GameObjectTypes.Transform calclatedTransformobj = new FrooxEngineRepresentation.GameObjectTypes.Transform();
            calclatedTransformobj.parentHashedGameObj = calclatedobj;

            UnityPackageImporter.Msg($"{Calculated_fileidtransform} was made from transform \"{curnode.Name}\" with path \"{transformpath}\"");

            SourceObj identifier2 = new SourceObj();
            identifier2.fileID = Calculated_fileidtransform;
            identifier2.guid = assetID;
            FILEID_into_Slot_Pairs.Add(identifier2, calclatedTransformobj);
            if (root.ChildCount > 0)
            {
                foreach(Node child in root.Children)
                {
                    FILEID_into_Slot_Pairs.AddRange(RecusiveFileIDSlotFinder(data2, child, SceneRoot, assetID));
                }
                
            }

            return FILEID_into_Slot_Pairs;
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
            string path = "/"+child.Parent.Name + curpath;
            if(child.Parent == StopAt){
                return path;
            }
            return FindSlotPathRecursive(child.Parent, StopAt, path);
        }




    }
}
