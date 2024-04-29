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
using UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes;

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

        private Slot targetSlot;
        public Slot FinishedFileSlot = null;
        public bool postprocessfinished = false;
        public bool running;

        public FileImportTaskScene(Slot targetSlot, string assetID, UnityProjectImporter importer, string file)
        {
            this.targetSlot = targetSlot.AddSlot(Path.GetFileNameWithoutExtension(file)+ " - Temp.fbx");

            this.file = file;
            this.importer = importer;
            this.importTaskAssetSlot = importer.importTaskAssetRoot.AddSlot("Assets - "+ Path.GetFileNameWithoutExtension(file) + ".fbx");


            this.assetID = assetID;
        }

        private FileImportTaskScene(Slot targetSlot, UnityProjectImporter importer)
        {
            this.targetSlot = targetSlot;
            this.importer = importer;

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

            await this.metafile.ScanFile(this, data.TryGetSlot(scene.RootNode));

            foreach (Slot slot in data.TryGetSlot(scene.RootNode).GetAllChildren(false).ToArray())
            {
                if (null == slot.GetComponent<FrooxEngine.SkinnedMeshRenderer>())
                {
                    await default(ToWorld);
                    UnityPackageImporter.Msg("scaling bone " + slot.Name + " with scale \"" + this.metafile.GlobalScale + "\"");

                    //prevent from taking over the world by the aliens-- I meant the model - @989onan
                    
                    await default(ToBackground);
                }
            }

            await default(ToWorld);
            data.TryGetSlot(scene.RootNode).Tag = "PREFABROOTTAG1234";
            await default(ToBackground);

            this.FinishedFileSlot = this.targetSlot;

            foreach (Slot childofroot in data.TryGetSlot(scene.RootNode).Children)
            {
                this.FILEID_To_Slot_Pairs.AddRange(RecusiveFileIDSlotFinder(childofroot, data.TryGetSlot(scene.RootNode), this.assetID));//find each child slot of the whole thing
            }

            //this for statement thing is crazy. But it gets the job done - @989onan
            foreach (FrooxEngine.SkinnedMeshRenderer mesh in this.FinishedFileSlot.GetAllChildren().FindAll(slot => slot.GetComponent<FrooxEngine.SkinnedMeshRenderer>() != null).Select(slot => slot.GetComponent<FrooxEngine.SkinnedMeshRenderer>()))
            {

                UnityPackageImporter.Msg("scanning files for ids for mesh renderer "+mesh.Slot.Name);
                FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer skinnedrenderer = new FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer();
                SourceObj identifier = this.findRealSource(mesh.Slot.Name, "SkinnedMeshRenderer", "//RootNode/root/" + mesh.Slot.Name);
                identifier.guid = this.assetID;
                skinnedrenderer.m_CorrespondingSourceObject = identifier;
                skinnedrenderer.createdMeshRenderer = mesh;
                skinnedrenderer.m_Mesh = this.findRealSource(mesh.Slot.Name, "Mesh", mesh.Slot.Name); //this allows us to identify this object by mesh, which is useful for prefabs.
                skinnedrenderer.m_Mesh.guid = this.assetID;
                UnityPackageImporter.Msg("id for mesh \""+mesh.Slot.Name+"\" is \""+ skinnedrenderer.m_Mesh.ToString() + "\"files for ids for mesh renderer " + mesh.Slot.Name);
                UnityPackageImporter.Msg("id for skinned mesh renderer \"" + mesh.Slot.Name + "\" is \"" + identifier.ToString()+"\"");
                this.FILEID_To_Slot_Pairs.Add(identifier, skinnedrenderer);
            }






            UnityPackageImporter.Msg("Finished task for file " + file);
            this.running = false;
        }


        //this is trashy I know - @989onan
        public async Task<FileImportTaskScene> MakeCopyAndPopulatePrefabData()
        {
            FileImportTaskScene copy = new FileImportTaskScene(targetSlot, importer);
            copy.file = this.file;
            await default(ToWorld);
            UnityPackageImporter.Msg("target slot instanciated?: "+(copy.targetSlot != null));
            copy.targetSlot = copy.targetSlot.Duplicate(null, false, null);
            copy.FinishedFileSlot = copy.targetSlot;
            copy.metafile = new MetaDataFile();
            copy.FILEID_To_Slot_Pairs.Clear();
            copy.assetID = this.assetID;

            //cleanup
            Slot Sceneroot = copy.targetSlot.GetChildrenWithTag("PREFABROOTTAG1234").First();
            Sceneroot.Tag = null;
            await default(ToBackground);


            UnityPackageImporter.Msg("scanning meta file for data");
            UnityPackageImporter.Msg("file instanciated?: " + (copy.file != null));
            UnityPackageImporter.Msg("targetSlot instanciated?: " + (copy.targetSlot != null));
            UnityPackageImporter.Msg("FinishedFileSlot instanciated?: " + (copy.FinishedFileSlot != null));
            await copy.metafile.ScanFile(copy, copy.targetSlot);
            UnityPackageImporter.Msg("recurisively searching for slots in file: " + file);
            FrooxEngineBootstrap.LogStream.Flush();

            foreach (Slot childofroot in Sceneroot.Children)
            {
                copy.FILEID_To_Slot_Pairs.AddRange(RecusiveFileIDSlotFinder(childofroot, Sceneroot, copy.assetID));//find each child slot of the whole thing
            }

            //this for statement thing is crazy. But it gets the job done - @989onan
            foreach (FrooxEngine.SkinnedMeshRenderer mesh in copy.FinishedFileSlot.GetAllChildren().FindAll(slot => slot.GetComponent<FrooxEngine.SkinnedMeshRenderer>() != null).Select(slot => slot.GetComponent<FrooxEngine.SkinnedMeshRenderer>()))
            {

                UnityPackageImporter.Msg("giving a skinned mesh renderer materials for file: " + file);
                FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer skinnedrenderer = new FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer();
                SourceObj identifier = copy.findRealSource(mesh.Slot.Name, "SkinnedMeshRenderer", "//RootNode/root/" + mesh.Slot.Name);
                skinnedrenderer.m_CorrespondingSourceObject = identifier;
                identifier.guid = copy.assetID;
                UnityPackageImporter.Msg("path for mesh \""+ mesh.Slot.Name + "\"" + identifier.ToString());
               
                skinnedrenderer.m_Mesh = copy.findRealSource(mesh.Slot.Name, "Mesh", mesh.Slot.Name); //this allows us to identify this object by mesh, which is useful for prefabs.

                //UnityPackageImporter.Msg($"{Calculated_fileid} was made from SkinnedMeshRenderer with a slot \"{mesh.Slot.Name}\" with path \"{calculatedpath}\"");
                
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
                    bonemappings.Add(bone.Name, copy.FinishedFileSlot.FindChildInHierarchy(bone.Name));
                }
                skinnedrenderer.createdMeshRenderer.SetupBones(bonemappings);
                skinnedrenderer.createdMeshRenderer.SetupBlendShapes();



                //we will replace these missing ones later with m_modifications
                List<string> materialnames = new List<string>();



                for (int i = 0; i < skinnedrenderer.createdMeshRenderer.Materials.Count; i++)
                {

                    materialnames.Add(skinnedrenderer.createdMeshRenderer.Materials[i].FindNearestParent<Slot>().Name.Replace("Material: ", "").Trim());
                }

                for (int i = 0; i < skinnedrenderer.createdMeshRenderer.Mesh.Asset.Data.SubmeshCount; i++)
                {
                    FileImportHelperTaskMaterial materialtask;
                    try
                    {
                        if (copy.metafile.externalObjects != null)
                        {
                            if (copy.metafile.externalObjects.TryGetValue(materialnames[i], out SourceObj material))
                            {
                                await default(ToWorld);
                                materialtask = new FileImportHelperTaskMaterial(material.guid, copy.importer.AssetIDDict[material.guid], copy.importer);
                                await default(ToBackground);
                            }
                            else
                            {
                                await default(ToWorld);
                                materialtask = new FileImportHelperTaskMaterial(copy.importer); //missing material for now. M_Modifications may modify later.
                                await default(ToBackground);
                            }
                        }
                        else
                        {
                            await default(ToWorld);
                            materialtask = new FileImportHelperTaskMaterial(copy.importer);
                            await default(ToBackground);
                        }
                        await default(ToWorld);
                        skinnedrenderer.materials.Add(materialtask);
                        skinnedrenderer.m_Materials.Add(new SourceObj(0,string.Empty,0)); //this is to signify 
                        await default(ToBackground);
                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Msg("The material " + materialnames[i] + " importing encountered an error!");
                        skinnedrenderer.m_Materials.Add(new SourceObj(0, string.Empty, 0));
                        throw e;
                    }
                    await default(ToWorld);




                }

                if (!copy.FILEID_To_Slot_Pairs.ContainsKey(identifier))
                {
                    copy.FILEID_To_Slot_Pairs.Add(identifier, skinnedrenderer);
                }


                UnityPackageImporter.Msg("clearing bad material objects for initial import for: \"" + skinnedrenderer.createdMeshRenderer.Slot.Name + "\"");
                await default(ToWorld);
                skinnedrenderer.createdMeshRenderer.Materials.Clear();
                await default(ToBackground);
                int counter = 0;
                UnityPackageImporter.Msg("getting good material objects for initial import for: \"" + skinnedrenderer.createdMeshRenderer.Slot.Name + "\" it has \"" + skinnedrenderer.materials.Count.ToString() + " \" materials in the generated list to instanciate");
                foreach (FileImportHelperTaskMaterial materialtask in skinnedrenderer.materials)
                {
                    try
                    {
                        await default(ToWorld);
                        UnityPackageImporter.Msg("assigning material for initial import slot for: \"" + skinnedrenderer.createdMeshRenderer.Slot.Name + "\"");
                        skinnedrenderer.createdMeshRenderer.Materials.Add().Target = await materialtask.runImportFileMaterialsAsync();
                        await default(ToBackground);
                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Warn("Could not attach material  \"" + counter.ToString() + "\" for initial import on mesh \"" + skinnedrenderer.createdMeshRenderer.Slot.Name + "\" from skinned mesh renderer data. It's probably not in the project or in the files you dragged over.");
                        UnityPackageImporter.Warn("stacktrace for material \"" + counter.ToString() + "\" for initial import on mesh \"" + skinnedrenderer.createdMeshRenderer.Slot.Name + "\"");
                        UnityPackageImporter.Warn(e.Message);
                        await default(ToWorld);
                        skinnedrenderer.createdMeshRenderer.Materials.Add(await new FileImportHelperTaskMaterial(copy.importer).runImportFileMaterialsAsync());
                        await default(ToBackground);
                    }
                    counter++;
                }

            }


            return copy;
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

        public Dictionary<SourceObj, IUnityObject> RecusiveFileIDSlotFinder(Slot curnode, Slot Scene, string assetID)
        {
            //UnityPackageImporter.Msg($"checking slot \"{root.Name}\"");
            FrooxEngineBootstrap.LogStream.Flush();

            Dictionary<SourceObj, IUnityObject> FILEID_into_Slot_Pairs = new Dictionary<SourceObj, IUnityObject>(new SourceObjCompare());


            

            //UnityPackageImporter.Msg($"{Calculated_fileid} was made from slot \"{curnode.Name}\" with path \"{gameobjpath}\"");
            FrooxEngineBootstrap.LogStream.Flush();
            FrooxEngineRepresentation.GameObjectTypes.GameObject calclatedobj = new FrooxEngineRepresentation.GameObjectTypes.GameObject();
            calclatedobj.frooxEngineSlot = curnode;
            calclatedobj.m_CorrespondingSourceObject = findRealSource(curnode.Name, "GameObject", "//RootNode/root" + FindSlotPath(curnode, Scene));
            calclatedobj.m_Name = curnode.Name;
            if (!FILEID_into_Slot_Pairs.ContainsKey(calclatedobj.m_CorrespondingSourceObject))
            {
                FILEID_into_Slot_Pairs.Add(calclatedobj.m_CorrespondingSourceObject, calclatedobj);
            }
            




            
            FrooxEngineRepresentation.GameObjectTypes.Transform calclatedTransformobj = new FrooxEngineRepresentation.GameObjectTypes.Transform();
            calclatedTransformobj.parentHashedGameObj = calclatedobj;
            
            calclatedTransformobj.m_CorrespondingSourceObject = findRealSource(curnode.Name, "Transform", "//RootNode/root" + FindSlotPath(curnode, Scene));

            //this is so m_modifications work - @989onan
            calclatedTransformobj.m_LocalPosition = new TransformFloat3(calclatedobj.frooxEngineSlot.LocalPosition.x, calclatedobj.frooxEngineSlot.LocalPosition.y, calclatedobj.frooxEngineSlot.LocalPosition.z);
            calclatedTransformobj.m_LocalScale = new TransformFloat3(calclatedobj.frooxEngineSlot.LocalScale.x, calclatedobj.frooxEngineSlot.LocalScale.y, calclatedobj.frooxEngineSlot.LocalScale.z);
            calclatedTransformobj.m_LocalRotation = new TransformFloat4(calclatedobj.frooxEngineSlot.LocalRotation.x, calclatedobj.frooxEngineSlot.LocalRotation.y, calclatedobj.frooxEngineSlot.LocalRotation.z, calclatedobj.frooxEngineSlot.LocalRotation.w);

            //UnityPackageImporter.Msg($"{Calculated_fileidtransform} was made from transform \"{curnode.Name}\" with path \"{transformpath}\"");
            if (!FILEID_into_Slot_Pairs.ContainsKey(calclatedTransformobj.m_CorrespondingSourceObject))
            {
                FILEID_into_Slot_Pairs.Add(calclatedTransformobj.m_CorrespondingSourceObject, calclatedTransformobj);
            }
            if (curnode.ChildrenCount > 0)
            {
                foreach(Slot child in curnode.Children)
                {
                    FILEID_into_Slot_Pairs.AddRange(RecusiveFileIDSlotFinder(child, Scene, assetID));
                }
                
            }

            return FILEID_into_Slot_Pairs;
        }

        public SourceObj findRealSource(string thisname, string type, string rawpath)
        {
            SourceObj sourceObj = new SourceObj();
            string endingtype = "";
            if (!type.Equals("GameObject") && !type.Equals("Mesh"))
            {
                endingtype = "/"+type;
            }
            string path = "Type:" + type + "->" + rawpath + endingtype + "0";
            long calculated_gameobj_id = (long)XXHash.Hash64(Encoding.UTF8.GetBytes(path));
            sourceObj.fileID = calculated_gameobj_id;
            sourceObj.guid = this.assetID;
            sourceObj.type = 0;


            foreach (KeyValuePair<long, string> item in this.metafile.fileIDToRecycleName)
            {
                string numident = UnityObjectMapping.type_name_To_ID[type].ToString();

                string nummatch = item.Key.ToString();
                if (thisname == item.Value && nummatch.Remove(nummatch.Length-5).Equals(numident)) //the format is: "{numident}+00002" or  "{numident}+00012" and so on. So removing 5 chars gives us our identifier.
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
