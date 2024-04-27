using FrooxEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.FrooxEngineRepresentation;


namespace UnityPackageImporter.Models
{
    internal class UnityPrefabImportTask : IUnityStructureImporter
    {

        public List<Slot> oldSlots = new List<Slot>();
        public Dictionary<ulong, IUnityObject> existingIUnityObjects { get; set; }
        public Slot CurrentStructureRootSlot { get; set; }
        public Slot allimportsroot { get; set; }
        public KeyValuePair<string, string> ID { get; set; }


        public UnityProjectImporter unityProjectImporter { get; set; }

        public UnityPrefabImportTask(Slot root, KeyValuePair<string, string> ID, UnityProjectImporter unityProjectImporter)
        {
            this.ID = ID;
            this.unityProjectImporter = unityProjectImporter;
            this.allimportsroot = root;
        }
        public async Task StartImport()
        {
            StringBuilder debugPrefab = new StringBuilder();
            try {
                existingIUnityObjects = new Dictionary<ulong, IUnityObject>();
                await default(ToWorld);
                this.CurrentStructureRootSlot = Engine.Current.WorldManager.FocusedWorld.AddSlot(Path.GetFileName(ID.Value));
                this.CurrentStructureRootSlot.SetParent(this.allimportsroot, false);
                this.CurrentStructureRootSlot.PositionInFrontOfUser(null, null, 0.7f, null, false, false, true);
                await default(ToBackground);




                this.existingIUnityObjects  = YamlToFrooxEngine.parseYaml(this.ID.Value);

                //some debugging for the user to show them it worked or failed.

                UnityPackageImporter.Msg("Loaded " + existingIUnityObjects.Count.ToString() + " Unity objects/components/meshes for prefab!");


                //instanciate our objects to generate our prefab entirely, using the ids we assigned ealier to identify our prefab elements in our list.
                foreach (var obj in existingIUnityObjects)
                {
                    UnityPackageImporter.Msg("loading object for prefab \"" + ID.Value + "\" with an id of \"" + obj.Value.id.ToString() + "\"");
                    try
                    {
                        await obj.Value.instanciateAsync(this);
                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Warn("Prefab IUnityObject failed to instanciate!");
                        UnityPackageImporter.Msg("Prefab IUnityObject ID: \"" + obj.Value.id.ToString() + "\"");
                        UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    }
                    try
                    {
                        debugPrefab.Append(obj.Value.ToString());
                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Warn("Prefab IUnityObject could not be turned into a string!");
                        UnityPackageImporter.Msg("Prefab IUnityObject ID: \"" + obj.Value.id.ToString() + "\"");
                        UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    }
                    


                }

                List<IUnityObject> movethese = new List<IUnityObject>();

                foreach (var obj in existingIUnityObjects)
                {
                    if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.Transform))
                    {
                        FrooxEngineRepresentation.GameObjectTypes.Transform trans = obj.Value as FrooxEngineRepresentation.GameObjectTypes.Transform;
                        if (trans != null)
                        {
                            if (trans.m_FatherID == 0 && trans.parentHashedGameObj == null)
                            {
                                try
                                {
                                    movethese.Add(existingIUnityObjects[trans.m_GameObjectID]);
                                }
                                catch
                                {
                                    UnityPackageImporter.Warn("transform with id \"" + trans.id.ToString() + "\" does not have a parent game object! This is bad!");
                                }
                            }
                        }
                    }
                }

                UnityPackageImporter.Msg("Moving orphaned objects");
                //getting rid of objects that should go under the prefab slot.
                foreach (var obj in movethese)
                {
                    FrooxEngineRepresentation.GameObjectTypes.GameObject gameobj = obj as FrooxEngineRepresentation.GameObjectTypes.GameObject;
                    await default(ToWorld);
                    gameobj.frooxEngineSlot.SetParent(this.CurrentStructureRootSlot, false);
                    await default(ToBackground);
                }



                UnityPackageImporter.Msg("Yaml generation done");
                UnityPackageImporter.Msg("Setting up IK Inline");

                
                //create humanoid stuff for prefabs that are inline.
                await default(ToWorld);
                foreach (var obj in existingIUnityObjects)
                {
                    if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.PrefabInstance))
                    {
                        FrooxEngineRepresentation.GameObjectTypes.PrefabInstance prefab = obj.Value as FrooxEngineRepresentation.GameObjectTypes.PrefabInstance;

                        await UnityProjectImporter.SettupHumanoid(
                            prefab.importask,
                            prefab.ImportRoot.frooxEngineSlot,
                            true);
                    }
                }

                UnityPackageImporter.Msg("Setting up IK Root");
                foreach (var obj in existingIUnityObjects)
                {
                    if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer))
                    {

                        FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer newobj = (obj.Value as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer);
                        await default(ToWorld);
                        newobj.createdMeshRenderer.Enabled = newobj.m_Enabled == 1;
                        await default(ToBackground);
                    }
                }

                foreach (var obj in existingIUnityObjects)
                {
                    if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer))
                    {
                        
                        FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer newobj = (obj.Value as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer);
                        if (newobj.createdMeshRenderer.Slot.Parent.Name == "RootNode")
                        {
                            if (this.unityProjectImporter.SharedImportedFBXScenes.TryGetValue(newobj.m_Mesh.guid, out FileImportTaskScene importedfbx)) {
                                await default(ToWorld);
                                await UnityProjectImporter.SettupHumanoid(importedfbx, this.CurrentStructureRootSlot, true);
                                await default(ToBackground);
                                break; //all skinned mesh renderers should go to the current prefab if they're under the root. I think that is the root above in the if statement with "RootNode" - @989onan
                            }
                            else
                            {
                                UnityPackageImporter.Msg("A prefab (source fbx id: \"" + newobj.m_Mesh.guid + "\") in prefab \"" + this.ID.Value + "\" that probably points to another prefab was attempted to be imported. TODO: FIX THIS");//TODO: FIX THIS!
                            }

                        }

                    }
                }
            }
            catch (Exception e)
            {
                UnityPackageImporter.Warn("Prefab hit critical import error! dumping!");
                UnityPackageImporter.Warn(e.Message + e.StackTrace);
                UnityPackageImporter.Msg(debugPrefab.ToString());
                FrooxEngineBootstrap.LogStream.Flush();
                throw e;
            }


            await default(ToBackground);
            UnityPackageImporter.Msg("Yaml generation done");

            
            UnityPackageImporter.Msg("Prefab finished!");
        }
    }
}
