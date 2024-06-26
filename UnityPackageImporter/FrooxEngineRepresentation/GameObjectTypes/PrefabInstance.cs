using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes;

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

    public async Task InstanciateAsync(IUnityStructureImporter importer)
    {
        if (instanciated) return;

        await default(ToBackground);
        
        UnityPackageImporter.Msg("is m_SourcePrefab instanciated?: " + (m_SourcePrefab != null));
        UnityPackageImporter.Msg("is m_Modification instanciated?: " + (m_Modification != null));
        UnityPackageImporter.Msg("is m_ObjectHideFlags instanciated?: " + m_ObjectHideFlags.ToString());
        UnityPackageImporter.Msg("is serializedVersion instanciated?: " + serializedVersion.ToString());

        if (m_SourcePrefab == null)
        {
            instanciated = true;
            return;
        }

        if (importer.unityProjectImporter.AssetIDDict.ContainsKey(m_SourcePrefab.guid))
        {
            UnityPackageImporter.Msg("starting instanciation of inline prefab \"" + id.ToString() + "\"");
            // Find FBX's in our scene that need importing, so we can import them and then attach our prefab objects to it.
            // We are sharing the FBX's, so if it's a project with X prefabs of the same model but with different changes can all use the same base model
            // Next we associate each shared FBX to prefabs that use them. then we duplicate the shared FBX's to each scene including this one, and then those duplicated FBXs become the prefabs themselves
            // Instanciate our objects to generate our prefab entirely, using the ids we assigned ealier to identify our prefab elements in our list.
            Slot targetParent;
            if (m_Modification == null)
            {
                targetParent = importer.CurrentStructureRootSlot;
            }
            else if (importer.existingIUnityObjects.TryGetValue(m_Modification.m_TransformParent["fileID"], out IUnityObject foundtransformparent))
            {
                await default(ToWorld);
                await foundtransformparent.InstanciateAsync(importer);
                await default(ToBackground);
                if (importer.existingIUnityObjects.TryGetValue((foundtransformparent as Transform).m_GameObjectID, out IUnityObject parentobjnew))
                {
                    await default(ToWorld);
                    await parentobjnew.InstanciateAsync(importer);
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

            UnityPackageImporter.Msg("is targetParent instanciated?: " + (targetParent != null));

            if (importask != null) return;

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
                importrootsource.fileID = 919132149155446097; // Hash of root object path
                importrootsource.type = 3;

                this.PrefabHashes = importask.FILEID_To_Slot_Pairs;
                this.PrefabHashes.Add(importrootsource, ImportRoot);
                List<IUnityObject> modifiedObjects = new List<IUnityObject>();
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
                                if (!modifiedObjects.Contains(targetobj))
                                {
                                    modifiedObjects.Add(targetobj);
                                }
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

                instanciated = true;
                // Instanciate our objects with our modifications, so in case they're not referenced in the scene, the imported FBX we duplicated to make this inline prefab has the M_Modifications still applied.
                //this is important to do, especially for skinned mesh renderers! - @989onan
                UnityPackageImporter.Msg("reinitializing objects that were modified.");
                foreach (IUnityObject obj in modifiedObjects)
                {
                    // This is so that it can find it's source, which is ourselves to prevent errors - @989onan
                    obj.m_PrefabInstance = new Dictionary<string, ulong> { { "fileID", this.id } }; 
                    try
                    {
                        UnityPackageImporter.Msg("reinitializing object that was modified. the object has an id of: \"" + obj.m_CorrespondingSourceObject.fileID + "\"");
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            UnityPackageImporter.Msg("reinitializing object that was modified. the object has an id of: \"" + obj.m_CorrespondingSourceObject.fileID + "\" has failed to instanciate. This is probably fine.");
                        }
                        catch (Exception ex)
                        {
                            UnityPackageImporter.Msg("reinitializing object that was modified. the object has an id of: \"NULL\" has failed to instanciate. This is probably fine.");
                            UnityPackageImporter.Msg(ex.Message, ex.StackTrace);
                        }
                        UnityPackageImporter.Msg(e.Message, e.StackTrace);
                    }
                    try
                    {
                        await default(ToWorld);
                        await obj.InstanciateAsync(importer);
                        await default(ToBackground);
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            UnityPackageImporter.Msg("reinitializing object that was modified. the object has an id of: \"" + obj.m_CorrespondingSourceObject.fileID + "\" has failed to instanciate. This is probably fine.");
                        }
                        catch (Exception ex)
                        {
                            UnityPackageImporter.Msg("reinitializing object that was modified. the object has an id of: \"NULL\" has failed to instanciate. This is probably fine.");
                            UnityPackageImporter.Msg(ex.Message, ex.StackTrace);
                        }
                        UnityPackageImporter.Msg(e.Message, e.StackTrace);
                    }

                }
            }
            else
            {
                UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the structure scene is malformed!!! cannot find file with GUID of \"" + m_SourcePrefab.guid.ToString() + "\" for this prefab! (does it exist in the unity project?)");
            }
        }
        else
        {
            UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the structure scene is malformed!!! The file source of the prefab doesn't exist in the imported package or file list set.");
        }
        

        instanciated = true;
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
