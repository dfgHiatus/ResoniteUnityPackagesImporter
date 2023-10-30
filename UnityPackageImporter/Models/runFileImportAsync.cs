using Assimp;
using Assimp.Configs;
using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FrooxEngine.ModelExporter;
using static FrooxEngine.ModelImporter;

namespace UnityPackageImporter.Models
{
    public class FileImportHelperTask
    {
        public ModelImportData data;
        public string file;
        public string assetID;

        public Task task;

        Slot targetSlot;
        public bool postprocessfinished = false;

        public FileImportHelperTask(string file, Slot targetSlot, string assetID)
        {
            this.targetSlot = targetSlot; 
            this.file = file;
            this.assetID = assetID;

            this.task = runImportFileMeshesAsync();
        }

        public Task runImportFileMeshesAsync()
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            targetSlot.StartCoroutine(ImportFileMeshesWrapper(taskCompletionSource));
            return taskCompletionSource.Task;
        }

        private IEnumerator<Context> ImportFileMeshesWrapper(TaskCompletionSource<bool> completion = null)
        {
            yield return Context.WaitFor(ImportFileMeshes());
            completion.SetResult(result: true);
        }



        private IEnumerator<Context> ImportFileMeshes()
        {
            yield return Context.ToBackground();
            AssimpContext assimpContext = new AssimpContext();
            assimpContext.Scale = 0.01f; //TODO: Grab file's scale from metadata
            assimpContext.SetConfig(new NormalSmoothingAngleConfig(66f));
            assimpContext.SetConfig(new TangentSmoothingAngleConfig(10f));
            PostProcessSteps postProcessSteps = PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.ImproveCacheLocality | PostProcessSteps.PopulateArmatureData | PostProcessSteps.GenerateUVCoords | PostProcessSteps.FindInstances | PostProcessSteps.FlipWindingOrder;
            Scene scene = null;
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


            PreprocessScene(scene);
            this.data = new ModelImportData(file, scene, this.targetSlot, null, ModelImportSettings.PBS(true, true, false, false, false, false), null);
            yield return Context.WaitFor(ModelImporter.ImportNode(scene.RootNode, targetSlot, this.data));



            yield break;
        } 


    }
}
