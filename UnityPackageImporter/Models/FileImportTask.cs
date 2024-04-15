﻿using Assimp;
using Assimp.Configs;
using Elements.Core;
using FrooxEngine;
using UnityFrooxEngineRunner;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static FrooxEngine.ModelImporter;
using System.Diagnostics;

namespace UnityPackageImporter.Models
{
    public class FileImportTask
    {
        public ModelImportData data;
        public string file;
        private UnityStructureImporter importer;
        public string assetID;
        public bool import_finished = false;

        Slot targetSlot;
        public bool postprocessfinished = false;
        public bool? isBiped;
        public bool running;

        public FileImportTask(Slot targetSlot, string assetID, UnityStructureImporter importer, string file)
        {
            this.targetSlot = targetSlot;
            this.file = file;
            this.importer = importer;
            
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
            PostProcessSteps postProcessSteps = PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.ImproveCacheLocality | PostProcessSteps.PopulateArmatureData | PostProcessSteps.GenerateUVCoords | PostProcessSteps.FindInstances | PostProcessSteps.FlipWindingOrder;
            Scene scene = null;

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
            this.data = new ModelImportData(file, scene, this.targetSlot, importer.importTaskAssetRoot, ModelImportSettings.PBS(true, true, false, false, false, false), null);
            UnityPackageImporter.Msg("importing node into froox engine, file: " + file);
            Task.WaitAll(recursiveNodeParserAsync(scene.RootNode, targetSlot, data));
            UnityPackageImporter.Msg("Finished task for file " + file);

            this.running = false;
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
                yield break;
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
