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

namespace UnityPackageImporter.Models
{
    public class FileImportTaskScene
    {
        public ModelImportData data;
        public Dictionary<ulong, Slot> FILEID_To_Slot_Pairs = new Dictionary<ulong, Slot>();
        public string file;
        private UnityStructureImporter importer;
        public string assetID;
        public bool import_finished = false;

        Slot targetSlot;
        public Slot FinishedFileSlot = null;
        public bool postprocessfinished = false;
        public bool? isBiped;
        public bool running;

        public FileImportTaskScene(Slot targetSlot, string assetID, UnityStructureImporter importer, string file)
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
            Context.WaitFor(ImportNode(scene.RootNode, targetSlot, data));
            UnityPackageImporter.Msg("Finished task for file " + file);
            this.FinishedFileSlot = data.TryGetSlot(scene.RootNode); //get the slot in case it was dumped beside a bunch of others.
            FILEID_To_Slot_Pairs = RecusiveFileIDSlotFinder(scene.RootNode);
            this.running = false;
        }

        public static Dictionary<ulong, Slot> RecusiveFileIDSlotFinder(ModelImportData data, Node root)
        {
            Dictionary<ulong, Slot> FILEID_into_Slot_Pairs = new Dictionary<ulong, Slot>();

            Slot curnode = data.TryGetSlot(root);

            XxHash64 _xhash32 = new XxHash64();
            var bytes = Encoding.UTF8.GetBytes("Type:GameObject->//RootNode/root/FemaleClothesNoSocks0");
            var stream = new MemoryStream(bytes);
            _xhash32.Append(stream);
            ulong hashBytes = _xhash32.GetCurrentHashAsUInt64();
            Console.WriteLine((long)hashBytes);
            FILEID_into_Slot_Pairs.Add(curnode.Name);
            if (root.ChildCount > 0)
            {
                foreach(Node child in root.Children)
                {
                    FILEID_into_Slot_Pairs.AddRange(RecusiveFileIDSlotFinder(data, child));
                }
                
            }

            return FILEID_into_Slot_Pairs;
        }







    }
}
