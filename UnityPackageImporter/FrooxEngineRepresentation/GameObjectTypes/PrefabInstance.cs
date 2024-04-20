using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    public class PrefabInstance : IUnityObject
    {
        public bool instanciated { get; set; }
        public ulong id { get; set; }

        public int m_ObjectHideFlags = 1000;
        public int serializedVersion = 1000;

        public SourceObj m_CorrespondingSourceObject { get; set; }
        public SourceObj m_SourcePrefab { get; set; }

        public GameObject ImportRoot;

        public ModPrefab m_Modification;

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
                            if (importer.existingIUnityObjects.TryGetValue(m_Modification.m_TransformParent["fileID"], out IUnityObject foundobjectparent))
                            {
                                await foundobjectparent.instanciateAsync(importer);
                                targetParent = (foundobjectparent as GameObject).frooxEngineSlot;
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

                        ImportRoot = new GameObject();
                        UnityPackageImporter.Msg("is targetParent instanciated?: " + (targetParent != null));
                        await default(ToWorld);
                        ImportRoot.frooxEngineSlot = importer.unityProjectImporter.SharedImportedFBXScenes[m_SourcePrefab.guid].FinishedFileSlot.Duplicate();
                        ImportRoot.frooxEngineSlot.SetParent(targetParent);


                        await default(ToBackground);

                        this.PrefabHashes = importer.unityProjectImporter.SharedImportedFBXScenes[m_SourcePrefab.guid].FILEID_To_Slot_Pairs;
                        if (m_Modification != null)
                        {
                            foreach (ModsPrefab mod in m_Modification.m_Modifications)
                            {
                                UnityPackageImporter.Msg("is the current modification instanciated?: " + (mod != null));
                                if (this.PrefabHashes.TryGetValue(mod.target, out IUnityObject targetobj))
                                {
                                    Type targettype = targetobj.GetType();
                                    try
                                    {
                                        //This is so bad, since it does a lot of reflection, but there's no better way of doing this since the hash of the object we're targeting doesn't actually represent the class of the type we made from it. - @989onan
                                        if (targettype.GetProperty(mod.propertyPath).GetValue(targetobj).GetType() == typeof(int))
                                        {
                                            int.TryParse(mod.value, out int value);
                                            targettype.GetProperty(mod.propertyPath).SetValue(Convert.ChangeType(targetobj, targettype), value);
                                        }
                                        else if (targettype.GetProperty(mod.propertyPath).GetValue(targetobj).GetType() == typeof(float))
                                        {
                                            float.TryParse(mod.value, out float value);
                                            targettype.GetProperty(mod.propertyPath).SetValue(Convert.ChangeType(targetobj, targettype), value);
                                        }
                                        else if (targettype.GetProperty(mod.propertyPath).GetValue(targetobj).GetType() == typeof(bool))
                                        {
                                            bool.TryParse(mod.value, out bool value);
                                            targettype.GetProperty(mod.propertyPath).SetValue(Convert.ChangeType(targetobj, targettype), value);
                                        }
                                        else if (targettype.GetProperty(mod.propertyPath).GetValue(targetobj).GetType() == typeof(string))
                                        {
                                            targettype.GetProperty(mod.propertyPath).SetValue(Convert.ChangeType(targetobj, targettype), mod.value);
                                        }
                                        else
                                        {
                                            UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the current structure is malformed!!! The modification with the hash \"" + mod.target.fileID + "\" has a value of \"" + mod.value + "\", which doesn't cast to any: " +
                                                "int, float, string" +
                                                ".");

                                        }

                                    }
                                    catch (Exception e)
                                    {
                                        UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the structure scene is malformed!!! The modification with the hash \"" + mod.propertyPath + "\" does not exist! ");
                                        UnityPackageImporter.Warn(e.Message + e.StackTrace);
                                    }


                                }
                                else
                                {
                                    UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the structure scene is malformed!!! The modification with the hash \"" + mod.target.fileID + "\" does not match any in the list of hashes on the prefab \"" + id.ToString() + "\"");
                                }

                            }

                        }

                    }
                    else
                    {
                        UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the structure scene is malformed!!! The file source of the prefab doesn't exist in the imported package or file list set.");
                    }
                }
                
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
