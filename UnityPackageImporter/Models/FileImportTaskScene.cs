using Assimp;
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
using System.Text;
using static FrooxEngine.ModelImporter;
using System.Diagnostics;
using UnityEngine.SceneManagement;
using MonoMod.Utils;
using HashDepot;
using UnityPackageImporter.FrooxEngineRepresentation;
using UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes;
using UnityEngine.VR;
using uOSC;
using UnityEngine;
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
            this.data = new ModelImportData(file, scene, this.targetSlot, importer.importTaskAssetRoot, ModelImportSettings.PBS(true, true, false, false, false, false), null);
            UnityPackageImporter.Msg("importing node into froox engine, file: " + file);
            Context.WaitFor(ImportNode(scene.RootNode, targetSlot, data));
            UnityPackageImporter.Msg("Finished task for file " + file);
            this.FinishedFileSlot = data.TryGetSlot(scene.RootNode); //get the slot in case it was dumped beside a bunch of others.
            FILEID_To_Slot_Pairs.AddRange(RecusiveFileIDSlotFinder(this.data, scene.RootNode, this.FinishedFileSlot, this.assetID));
           
            foreach(FrooxEngine.SkinnedMeshRenderer mesh in this.data.skinnedRenderers)
            {
                string calculatedpath = FindSlotPath(mesh.Slot, this.FinishedFileSlot);
                long Calculated_fileid = (long)XXHash.Hash64(Encoding.UTF8.GetBytes(("/" + calculatedpath + "/SkinnedMeshRenderer0")));
                Console.WriteLine($"{Calculated_fileid} was made from SkinnedMeshRenderer with a slot \"{mesh.Slot.Name}\" with path \"{calculatedpath}\"");
                FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer skinnedrenderer = new FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer();
                skinnedrenderer.createdMeshRenderer = mesh;

                await default(ToWorld);
                while (!mesh.Mesh.IsAssetAvailable)
                {
                    await default(NextUpdate);
                    await Task.Delay(1000);
                    UnityPackageImporter.Msg("Waiting for mesh assets for: \"" + mesh.Slot.Name + "\"");
                }
                await default(ToWorld);
                UnityPackageImporter.Msg("finished waiting for mesh assets for: \"" + mesh.Slot.Name + "\"");

                SourceObj identifier = new SourceObj();
                identifier.fileID = Calculated_fileid;
                identifier.guid = this.assetID;
                FILEID_To_Slot_Pairs.Add(identifier, skinnedrenderer);
            }



            


            

            


            this.running = false;
        }

        //asset id is optional. it's for my convenience.
        public static Dictionary<SourceObj, IUnityObject> RecusiveFileIDSlotFinder(ModelImportData data, Node root, Slot SceneRoot, string assetID)
        {
            Dictionary<SourceObj, IUnityObject> FILEID_into_Slot_Pairs = new Dictionary<SourceObj, IUnityObject>(new SourceObjCompare());

            Slot curnode = data.TryGetSlot(root);

            string calculatedpath = FindSlotPath(curnode, SceneRoot);

            string gameobjpath = "/" + calculatedpath + "0";

            long Calculated_fileid = (long)XXHash.Hash64(Encoding.UTF8.GetBytes(gameobjpath));
           
            Console.WriteLine($"{Calculated_fileid} was made from slot \"{curnode.Name}\" with path \"{gameobjpath}\"");
            FrooxEngineRepresentation.GameObjectTypes.GameObject calclatedobj = new FrooxEngineRepresentation.GameObjectTypes.GameObject();
            calclatedobj.instanciated = true;
            calclatedobj.frooxEngineSlot = curnode;

            SourceObj identifier1 = new SourceObj();
            identifier1.fileID = Calculated_fileid;
            identifier1.guid = assetID;

            FILEID_into_Slot_Pairs.Add(identifier1, calclatedobj);
            string transformpath = "/" + calculatedpath + "/Transform0";

            long Calculated_fileidtransform = (long)XXHash.Hash64(Encoding.UTF8.GetBytes(transformpath));
            FrooxEngineRepresentation.GameObjectTypes.Transform calclatedTransformobj = new FrooxEngineRepresentation.GameObjectTypes.Transform();
            calclatedTransformobj.parentHashedGameObj = calclatedobj;

            Console.WriteLine($"{Calculated_fileidtransform} was made from slot \"{curnode.Name}\" with path \"{transformpath}\"");

            SourceObj identifier2 = new SourceObj();
            identifier2.fileID = Calculated_fileidtransform;
            identifier2.guid = assetID;
            FILEID_into_Slot_Pairs.Add(identifier2, calclatedTransformobj);
            if (root.ChildCount > 0)
            {
                foreach(Node child in root.Children)
                {
                    FILEID_into_Slot_Pairs.AddRange(RecusiveFileIDSlotFinder(data, child, SceneRoot, assetID));
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
                    return FindSlotPathRecursive(child, StopAt, child.Name);

                }
                else
                {
                    return string.Empty;
                }
            }

        }

        private static string FindSlotPathRecursive(Slot child, Slot StopAt, string curpath)
        {
            string path = "/"+child.Parent+"/" + curpath;
            if(child.Parent == StopAt){
                return path;
            }
            return FindSlotPathRecursive(child, StopAt, path);
        }




    }
}
