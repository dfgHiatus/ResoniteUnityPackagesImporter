using Assimp;
using Assimp.Configs;
using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static FrooxEngine.ModelImporter;

namespace UnityPackageImporter.Models
{
    public class FileImportHelperTaskMesh
    {
        public ModelImportData data;
        public string file;
        public string assetID;
        public bool? isBiped = null;
        public SharedData __state;
        public Task task;

        Slot targetSlot;
        public bool postprocessfinished = false;

        public FileImportHelperTaskMesh(string file, Slot targetSlot, string assetID, SharedData __state)
        {
            this.targetSlot = targetSlot;
            this.file = file;
            this.assetID = assetID;
            this.isBiped = null;
            this.__state = __state;
            this.task = targetSlot.StartGlobalTask(async () => await runImportFileMeshesAsync());
        }

        private async Task runImportFileMeshesAsync()
        {
            await default(ToBackground);
            await ImportFileMeshes();
        }


        private async Task ImportFileMeshes()
        {
            UnityPackageImporter.Msg("Start code block for file import for file " + file);
            await default(ToBackground);
            AssimpContext assimpContext = new AssimpContext();
            assimpContext.Scale = 0.01f; //TODO: Grab file's scale from metadata
            assimpContext.SetConfig(new NormalSmoothingAngleConfig(66f));
            assimpContext.SetConfig(new TangentSmoothingAngleConfig(10f));
            PostProcessSteps postProcessSteps = PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.ImproveCacheLocality | PostProcessSteps.PopulateArmatureData | PostProcessSteps.GenerateUVCoords | PostProcessSteps.FindInstances | PostProcessSteps.FlipWindingOrder;
            Scene scene = null;
            UnityPackageImporter.Msg("Start assimp file import for file " + file);
            try
            {
                scene = assimpContext.ImportFile(this.file, postProcessSteps);
            }
            catch (Exception arg)
            {
                UniLog.Warning(string.Format("Exception when importing {0}:\n\n{1}", this.file, arg), false);
            }
            finally
            {
                assimpContext.Dispose();
            }

            UnityPackageImporter.Msg("Preprocessing scene for file " + file);
            PreprocessScene(scene);
            UnityPackageImporter.Msg("making model import data for file: " + file);
            this.data = new ModelImportData(file, scene, this.targetSlot, __state.importTaskAssetRoot, ModelImportSettings.PBS(true, true, false, false, false, false), null);
            UnityPackageImporter.Msg("importing node into froox engine, file: " + file);
            Task.WaitAll(recursiveNodeParserAsync(scene.RootNode, targetSlot, data));
            UnityPackageImporter.Msg("Finishing task for file " + file);

        }



        public static Task recursiveNodeParserAsync(Node node, Slot targetSlot, ModelImportData data)
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            targetSlot.StartCoroutine(recursiveNodeParserWrapper(node, targetSlot, data, taskCompletionSource));
            return taskCompletionSource.Task;
        }

        public static IEnumerator<Context> recursiveNodeParserWrapper(Node node, Slot targetSlot, ModelImportData data, TaskCompletionSource<bool> completion = null)
        {
            yield return Context.WaitFor(recursiveNodeParser(node, targetSlot, data));
            completion.SetResult(result: true);
        }
        public static IEnumerator<Context> recursiveNodeParser(Node node, Slot targetSlot, ModelImportData data)
        {

           
            
            if (node.MeshCount > 0)
            {
                //by calling import node in this weird way only when we find a mesh, it will set up the mesh for us but ignore
                //all of the bones and stuff, making the meshes easier to handle.
                UnityPackageImporter.Msg("Importing Mesh \"" + node.Name + "\"");
                yield return Context.WaitFor(ImportNode(node, targetSlot, data));

                UnityPackageImporter.Msg("Finish Importing Mesh \"" + node.Name + "\"");
            }
            
            if(!node.HasChildren)
            {
                yield break;
            }

            yield return Context.ToBackground();
            foreach (Node child in node.Children)
            {
                yield return Context.WaitFor(recursiveNodeParser(child, targetSlot, data));
            }



            
        }







    }
}
