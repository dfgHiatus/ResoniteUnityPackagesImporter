using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;
using static FrooxEngine.MeshEmitter;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    public class PrefabInstance : IUnityObject
    {
        public bool instanciated { get; set; }
        public ulong id { get; set; }

        public int m_ObjectHideFlags { get; set; }
        public int serializedVersion { get; set; }

        public SourceObj m_CorrespondingSourceObject { get; set; }
        public SourceObj m_SourcePrefab { get; set; }


        public FileImportTaskScene importask = null;

        public GameObject ImportRoot { get; set; }

        public ModPrefab m_Modification { get; set; }

        public Dictionary<string, ulong> m_PrefabInstance { get; set; }

        public Dictionary<SourceObj, IUnityObject> PrefabHashes = new Dictionary<SourceObj, IUnityObject>(new SourceObjCompare());

        public async Task instanciateAsync(IUnityStructureImporter importer)
        {
            await default(ToBackground);
            if (!instanciated)
            {
                UnityPackageImporter.Msg("is m_SourcePrefab instanciated?: " + (m_SourcePrefab != null));
                UnityPackageImporter.Msg("is m_Modification instanciated?: " + (m_Modification != null));
                UnityPackageImporter.Msg("is m_ObjectHideFlags instanciated?: " + m_ObjectHideFlags.ToString());
                UnityPackageImporter.Msg("is serializedVersion instanciated?: " + serializedVersion.ToString());

                if (m_SourcePrefab != null)
                {
                    if (importer.unityProjectImporter.AssetIDDict.ContainsKey(m_SourcePrefab.guid))
                    {
                        UnityPackageImporter.Msg("starting instanciation of inline prefab \"" + id.ToString() + "\"");
                        //find FBX's in our scene that need importing, so we can import them and then attach our prefab objects to it.
                        //We are sharing the FBX's, so if it's a project with X prefabs of the same model but with different changes can all use the same base model
                        //next we associate each shared FBX to prefabs that use them. then we duplicate the shared FBX's to each scene including this one, and then those duplicated FBXs become the prefabs themselves
                        //instanciate our objects to generate our prefab entirely, using the ids we assigned ealier to identify our prefab elements in our list.
                        Slot targetParent;
                        if (m_Modification != null)
                        {
                            if (importer.existingIUnityObjects.TryGetValue(m_Modification.m_TransformParent["fileID"], out IUnityObject foundtransformparent))
                            {
                                await default(ToWorld);
                                await foundtransformparent.instanciateAsync(importer);
                                await default(ToBackground);
                                if(importer.existingIUnityObjects.TryGetValue((foundtransformparent as Transform).m_GameObjectID, out IUnityObject parentobjnew)){
                                    await default(ToWorld);
                                    await parentobjnew.instanciateAsync(importer);
                                    await default(ToBackground);
                                    targetParent = (parentobjnew as GameObject).frooxEngineSlot;
                                }
                                else
                                {
                                    UnityPackageImporter.Warn("targetParent had a value for prefab \"" + id.ToString() + "\" but could not find the id \"" + (foundtransformparent as Transform).m_GameObjectID + "\" in the list of game objects that exist in scene!");
                                    targetParent = importer.CurrentStructureRootSlot;
                                }
                            }
                            else
                            {
                                targetParent = importer.CurrentStructureRootSlot;
                            }
                        }
                        else
                        {
                            targetParent = importer.CurrentStructureRootSlot;
                        }

                        
                        UnityPackageImporter.Msg("is targetParent instanciated?: " + (targetParent != null));
                        
                        if(importask == null)
                        {
                            ImportRoot = new GameObject();
                            await default(ToWorld);
                            if (importer.unityProjectImporter.SharedImportedFBXScenes.ContainsKey(m_SourcePrefab.guid))
                            {
                                importask = await importer.unityProjectImporter.SharedImportedFBXScenes[m_SourcePrefab.guid].MakeCopyAndPopulatePrefabData();
                                ImportRoot.frooxEngineSlot = importask.FinishedFileSlot;
                                ImportRoot.frooxEngineSlot.SetParent(targetParent);

                                await default(ToBackground);

                                SourceObj importrootsource = new SourceObj();
                                importrootsource.guid = this.m_SourcePrefab.guid;
                                importrootsource.fileID = 919132149155446097; //hash of root object path.
                                importrootsource.type = 3;

                                this.PrefabHashes = importask.FILEID_To_Slot_Pairs;
                                this.PrefabHashes.Add(importrootsource, ImportRoot);
                                if (m_Modification != null)
                                {
                                    foreach (ModsPrefab mod in m_Modification.m_Modifications)
                                    {
                                        IUnityObject targetobj2 = null;
                                        IUnityObject targetobj3 = null;
                                        if (this.PrefabHashes.TryGetValue(mod.target, out IUnityObject targetobj) || importer.existingIUnityObjects.TryGetValue((ulong)(mod.target.fileID + (2 ^ 64)), out targetobj2) || importer.existingIUnityObjects.TryGetValue((ulong)(mod.target.fileID), out targetobj3))
                                        {
                                            targetobj = targetobj ?? targetobj2 ?? targetobj3; //get first available match.
                                            if (MModificationsParser.ParseModifcation(targetobj, mod))
                                            {
                                                UnityPackageImporter.Msg("The prefab with a file id of \"" + id.ToString() + "\" in the structure scene has parsed a modification with the hash \"" + mod.target.ToString() + "\" successfully.");
                                            }
                                            else
                                            {
                                                UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the structure scene is malformed!!! The modification with the hash \"" + mod.target.ToString() + "\" could not be parsed!");
                                            }

                                        }
                                        else
                                        {
                                            UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the structure scene is malformed!!! The modification with the hash \"" + mod.target.ToString() + "\" does not match any in the list of hashes on the prefab \"" + id.ToString() + "\"");
                                        }

                                    }

                                }
                            }
                            else
                            {
                                UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the structure scene is malformed!!! cannot find file with GUID of \"" + m_SourcePrefab.guid.ToString() + "\" for this prefab! (does it exist in the unity project?)");
                            }
                        }
                         


                        

                    }
                    else
                    {
                        UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the structure scene is malformed!!! The file source of the prefab doesn't exist in the imported package or file list set.");
                    }
                }
                instanciated = true;

            }
        }


        //a detailed to string for debugging.
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            
            if (m_SourcePrefab != null)
            {
                result.AppendLine("m_SourcePrefab: " + m_SourcePrefab.ToString());

            }
            else
            {
                result.AppendLine("m_SourcePrefab: null");
            }
            if (m_CorrespondingSourceObject != null)
            {
                result.AppendLine("m_CorrespondingSourceObject: " + m_CorrespondingSourceObject.ToString());

            }
            else
            {
                result.AppendLine("m_CorrespondingSourceObject: null");
            }
            if (m_PrefabInstance != null)
            {
                result.AppendLine("m_PrefabInstance: " + m_PrefabInstance.ToString());
            }
            else
            {
                result.AppendLine("m_PrefabInstance: null");
            }
            if (m_Modification != null)
            {
                result.AppendLine("m_Modification: " + m_Modification.ToString());

            }


            return result.ToString();
        }

        




    }
}
