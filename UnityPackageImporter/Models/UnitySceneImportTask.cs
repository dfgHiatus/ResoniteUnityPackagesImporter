using FrooxEngine;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.FrooxEngineRepresentation;
using UnityPackageImporter.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace UnityPackageImporter.Models
{
    public class UnitySceneImportTask: IUnityStructureImporter
    {
        public Dictionary<ulong, IUnityObject> existingIUnityObjects { get; set; }
        public Slot CurrentStructureRootSlot { get; set; }

        public KeyValuePair<string, string> ID { get; set; }

        public Slot allimportsroot { get; set; }
        public UnityProjectImporter unityProjectImporter { get; set; }

        public UnitySceneImportTask(Slot allimportsroot, KeyValuePair<string, string> ID, UnityProjectImporter unityProjectImporter)
        {
            this.ID = ID;
            this.unityProjectImporter = unityProjectImporter;
            this.allimportsroot = allimportsroot;
        }

        

        public async Task StartImport()
        {
            StringBuilder debugScene = new StringBuilder();
            try
            {
                existingIUnityObjects = new Dictionary<ulong, IUnityObject>();
                await default(ToWorld);

                this.CurrentStructureRootSlot = Engine.Current.WorldManager.FocusedWorld.AddSlot(Path.GetFileName(ID.Value));
                UnityPackageImporter.Msg("Current slot for scene import is: ");
                UnityPackageImporter.Msg(CurrentStructureRootSlot.ToString());
                this.CurrentStructureRootSlot.SetParent(this.allimportsroot, false);
                await default(ToBackground);



                //we first have to remove "stripped" since those cause yaml parsing errors
                string[] initialstream = File.ReadAllLines(ID.Value);
                string[] newcontent = new string[initialstream.Length];
                for (int i = 0; i < initialstream.Length; i++)
                {
                    string line = initialstream[i];
                    newcontent[i] = line;
                    if (line.StartsWith("--- !u!"))
                    {
                        newcontent[i] = newcontent[i].Replace(" stripped", "");

                    }
                }
                File.WriteAllLines(ID.Value, newcontent);

                this.existingIUnityObjects = YamlToFrooxEngine.parseYaml(this.ID.Value);

                UnityPackageImporter.Msg("Loaded " + existingIUnityObjects.Count.ToString() + " Unity prefabs/transforms/special_components for scene!");

                //instanciate everything else.
                await default(ToWorld);
                foreach (var obj in existingIUnityObjects)
                {
                    UnityPackageImporter.Msg("loading object for scene \"" + ID.Value + "\" with an id of \"" + obj.Value.id.ToString() + "\"");
                    try
                    {
                        await obj.Value.instanciateAsync(this);
                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Warn("Scene IUnityObject failed to instanciate!");
                        UnityPackageImporter.Msg("Scene IUnityObject ID: \"" + obj.Value.id.ToString() + "\"");
                        UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    }
                    try
                    {
                        debugScene.Append(obj.Value.ToString());
                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Warn("Scene IUnityObject could not be turned into a string!");
                        UnityPackageImporter.Msg("Scene IUnityObject ID: \"" + obj.Value.id.ToString() + "\"");
                        UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    }
                    
                    
                    
                }
                await default(ToBackground);



                await default(ToBackground);

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
                                try{
                                    movethese.Add(existingIUnityObjects[trans.m_GameObjectID]);
                                }
                                catch
                                {
                                    UnityPackageImporter.Warn("transform with id \""+trans.id.ToString()+"\" does not have a parent game object! This is bad!" );
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

                UnityPackageImporter.Msg("re-enabling skinned mesh renderers in scene \""+ID.Value+"\"");

                await default(ToBackground);
                foreach (var obj in existingIUnityObjects)
                {
                    if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer))
                    {

                        FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer newobj = (obj.Value as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer);
                        await default(ToWorld);
                        if(newobj.createdMeshRenderer != null)
                        {
                            newobj.createdMeshRenderer.Enabled = newobj.m_Enabled == 1;
                        }
                        await default(ToBackground);
                    }
                }

                UnityPackageImporter.Msg("Setting up IK for Prefabs in scene \"" + ID.Value + "\"");
                await default(ToWorld);
                foreach (var obj in existingIUnityObjects)
                {
                    if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.PrefabInstance))
                    {
                        FrooxEngineRepresentation.GameObjectTypes.PrefabInstance prefab = obj.Value as FrooxEngineRepresentation.GameObjectTypes.PrefabInstance;
                        if(prefab.m_SourcePrefab != null)
                        {
                            if(prefab.m_SourcePrefab.guid != null)
                            {
                                if(prefab.ImportRoot != null)
                                {
                                    if (prefab.ImportRoot.frooxEngineSlot != null)
                                    {
                                        if (unityProjectImporter.SharedImportedFBXScenes.ContainsKey(prefab.m_SourcePrefab.guid))
                                        {
                                            await default(ToWorld);
                                            await UnityProjectImporter.SettupHumanoid(
                                            prefab.importask,
                                            prefab.ImportRoot.frooxEngineSlot);
                                            await default(ToBackground);
                                        }
                                        else
                                        {
                                            UnityPackageImporter.Msg("A prefab (source fbx id: \"" + prefab.id.ToString() + "\") in scene \"" + this.ID.Value + "\" that probably points to another prefab was attempted to be imported. TODO: FIX THIS");//TODO: FIX THIS!
                                        }
                                    }
                                }
                                
                            }
                           
                        }
                        

                    }
                }
                await default(ToBackground);
            }
            catch (Exception e)
            {
                UnityPackageImporter.Warn("Scene \"" + ID.Value + "\" hit critical import error! dumping!");
                UnityPackageImporter.Warn(e.Message + e.StackTrace);
                UnityPackageImporter.Msg(debugScene.ToString());
                FrooxEngineBootstrap.LogStream.Flush();
                throw e;
                
            }
            
            UnityPackageImporter.Msg("Yaml generation done");
            
            UnityPackageImporter.Msg("Scene finished!");
        }

    }
}
