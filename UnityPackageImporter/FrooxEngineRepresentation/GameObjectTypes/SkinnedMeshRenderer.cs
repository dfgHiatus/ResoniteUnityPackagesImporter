using Assimp;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;
using static HarmonyLib.Code;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    public class SkinnedMeshRenderer : IUnityObject
    {
        public ulong id { get; set; }
        public bool instanciated { get; set; }
        public Dictionary<string, ulong> m_GameObject;
        public int m_Enabled = 1;
        public Dictionary<int, FileImportHelperTaskMaterial> materials = new Dictionary<int, FileImportHelperTaskMaterial>();
        public List<Dictionary<string, string>> m_Materials;
        public SourceObj m_Mesh;
        public List<Dictionary<string, ulong>> m_Bones;
        public FrooxEngine.SkinnedMeshRenderer createdMeshRenderer;
        public GameObject parentobj;
        public SourceObj m_CorrespondingSourceObject { get; set; }
        public Dictionary<string, ulong> m_PrefabInstance { get; set; }

        public AABB m_AABB;


        public async Task instanciateAsync(IUnityStructureImporter importer)
        {

            if (!instanciated)
            {

                instanciated = true;

                try
                {
                    if (importer.unityProjectImporter.SharedImportedFBXScenes.TryGetValue(m_Mesh.guid, out FileImportTaskScene importedfbx2))
                    {
                        
                        if(m_CorrespondingSourceObject.guid == null)
                        {
                            importer.existingIUnityObjects.TryGetValue(m_GameObject["fileID"], out IUnityObject parentobj_inc);
                            parentobj = parentobj_inc as GameObject;

                            foreach (var pair in importedfbx2.FILEID_To_Slot_Pairs)
                            {
                                if (pair.Value.GetType() == typeof(SkinnedMeshRenderer))
                                {


                                    FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer newskin = (pair.Value as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer);
                                    if (newskin.createdMeshRenderer.Slot.Name.Equals(parentobj.frooxEngineSlot.Name))
                                    {

                                        

                                        await default(ToWorld);
                                        await parentobj.instanciateAsync(importer);
                                        await default(ToWorld);

                                        Slot newslot = newskin.createdMeshRenderer.Slot.Duplicate();
                                        newslot.SetParent(parentobj.frooxEngineSlot.Parent, false);
                                        newslot.TRS = parentobj.frooxEngineSlot.TRS;
                                        foreach (Slot child in parentobj.frooxEngineSlot.Children)
                                        {
                                            child.SetParent(parentobj.frooxEngineSlot, false);
                                        }
                                        await default(ToWorld);
                                        parentobj.frooxEngineSlot.Destroy();
                                        parentobj.frooxEngineSlot = newslot;
                                        this.createdMeshRenderer = newslot.GetComponent<FrooxEngine.SkinnedMeshRenderer>();
                                        await default(ToBackground);

                                    }
                                }
                            }
                        }
                        else
                        {
                            importer.existingIUnityObjects.TryGetValue(m_PrefabInstance["fileID"], out IUnityObject parentobj_inc);
                            await default(ToWorld);
                            await parentobj_inc.instanciateAsync(importer);
                            await default(ToWorld);
                            PrefabInstance prefab = parentobj_inc as PrefabInstance;

                            if (prefab.PrefabHashes.TryGetValue(this.m_CorrespondingSourceObject, out IUnityObject gameobj)) {
                                FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer newskin = (gameobj as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer);
                                parentobj.frooxEngineSlot = newskin.createdMeshRenderer.Slot;
                                this.createdMeshRenderer = newskin.createdMeshRenderer;
                            }
                            else
                            {
                                UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render with a guid of \"" + m_Mesh.guid + "\" could not find/create it's parent game obj!");
                            }
                            
                        }
                        
                        
                        

                        if(parentobj == null)
                        {
                            UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render with a guid of \"" + m_Mesh.guid + "\" could not find/create it's parent game obj with name \""+ parentobj.frooxEngineSlot.Name + "\"!");
                        }
                    }
                    else
                    {
                        UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render with a guid of \""+ m_Mesh.guid + "\" could not find the pre-imported FBX!");
                    }
                        
                    
                }
                catch (Exception ex)
                {
                    try
                    {
                        UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render couldn't find it's parent an id of \"" + m_GameObject["fileID"].ToString() + "\" Your renderer will come out mis-shapen!");
                    }
                    catch
                    {
                        try
                        {
                            UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render couldn't find it's parent a source obj of \"" + m_CorrespondingSourceObject.ToString() + "\" Your renderer will come out mis-shapen!");
                        }
                        catch
                        {
                            UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render does not have a valid parent obj type! Your renderer will come out mis-shapen!");
                        }
                    }
                    UnityPackageImporter.Warn(ex.Message, ex.StackTrace);
                }



                await default(ToBackground);
                foreach (Dictionary<string, ulong> item in m_Bones)
                {
                    if (importer.existingIUnityObjects.TryGetValue(item["fileID"], out IUnityObject bone_trans))
                    {
                        if (importer.existingIUnityObjects.TryGetValue(((Transform)bone_trans).m_GameObject["fileID"], out IUnityObject bone_obj))
                        {
                            GameObject obj2 = bone_obj as GameObject;
                            await default(ToWorld);
                            await bone_obj.instanciateAsync(importer);
                            await default(ToBackground);
                        }
                    }
                    else
                    {
                        UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render couldn't find it's bone with an id of \"" + item["fileID"].ToString() + "\" Your renderer will come out mis-shapen!");
                    }

                }

                int counter = 0;
                foreach(Dictionary<string, string> material in m_Materials)
                {
                    
                    try
                    {
                        if (importer.unityProjectImporter.AssetIDDict.ContainsKey(material["guid"]))
                        {
                            await default(ToWorld);
                            string file = importer.unityProjectImporter.AssetIDDict[material["guid"]];
                            var matimporttask = new FileImportHelperTaskMaterial(material["guid"], file, importer.unityProjectImporter);
                            materials.Add(counter, matimporttask);
                            await default(ToBackground);
                        }
                        
                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Warn("Importing a material threw an error!");
                        UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    }
                    
                    
                    
                    counter += 1;


                }

                if (importer.unityProjectImporter.SharedImportedFBXScenes.TryGetValue(m_Mesh.guid, out FileImportTaskScene importedfbx))
                {
                    FrooxEngine.SkinnedMeshRenderer FoundMesh = this.createdMeshRenderer;
                    //FoundMesh.Mesh.ReferenceID = (skinnedMeshRenderer as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer).createdMeshRenderer.Mesh.ReferenceID;
                    await default(ToWorld);
                    while (!FoundMesh.Mesh.IsAssetAvailable)
                    {
                        await default(NextUpdate);
                        //UnityPackageImporter.Msg("Waiting for mesh assets for: \"" + mesh.Slot.Name + "\"");
                    }
                    await default(ToWorld);
                    //below comment is done at the humanoid stage, so that shite doesn't get in the way of players - @989onan
                    //FoundMesh.Enabled = this.m_Enabled == 1; //in case it's disabled in unity, this will make it disabled when imported;
                    FoundMesh.Enabled = false;


                    /*//TODO: These are scaled wrong, causing the mesh to scale weirdly. come back later to this, maybe?. - @989onan
                    FoundMesh.ExplicitLocalBounds.Value =
                    Elements.Core.BoundingBox.CenterSize(
                        new Elements.Core.float3(
                            this.m_AABB.m_Center["x"],
                            this.m_AABB.m_Center["y"],
                            this.m_AABB.m_Center["z"]),
                        new Elements.Core.float3(this.m_AABB.m_Extent["x"],
                        this.m_AABB.m_Extent["y"],
                        this.m_AABB.m_Extent["z"]
                        ));*/
                    FoundMesh.BoundsComputeMethod.Value = SkinnedBounds.SlowRealtimeAccurate; //use generated colliders
                    await default(ToBackground);

                    Dictionary<string, Slot> bonemappings = new Dictionary<string, Slot>();
                    UnityPackageImporter.Msg("gathering bones for model.");
                    foreach (Dictionary<string, ulong> bonemap in this.m_Bones)
                    {
                        if (importer.existingIUnityObjects.TryGetValue(bonemap["fileID"], out IUnityObject unityObject))
                        {
                            FrooxEngineRepresentation.GameObjectTypes.Transform obj = unityObject as FrooxEngineRepresentation.GameObjectTypes.Transform;
                            await default(ToWorld);
                            await obj.instanciateAsync(importer);
                            await default(ToBackground);
                            if (importer.existingIUnityObjects.TryGetValue(obj.m_GameObjectID, out IUnityObject unityObjectGame))
                            {
                                GameObject gameobj = unityObjectGame as GameObject;
                                if (gameobj.m_Name.EndsWith(" 1"))
                                {
                                    string removed = gameobj.m_Name.Substring(0, gameobj.m_Name.LastIndexOf(" 1"));
                                    if (FoundMesh.Mesh.Asset.Data.bones.Find(bone => bone.Name == removed) != null)
                                    {
                                        bonemappings.Add(removed, gameobj.frooxEngineSlot); //take care of meshes that have the same names as bone (EX: "Head" mesh and "Head" bone)
                                        continue;
                                    }
                                }
                                bonemappings.Add(gameobj.frooxEngineSlot.Name, gameobj.frooxEngineSlot);

                            }
                            else
                            {
                                UnityPackageImporter.Msg("Couldn't find bone (error 2) for skinned mesh renderer on your prefab! This is bad!");
                            }
                            
                        }
                        else
                        {
                            UnityPackageImporter.Msg("Couldn't find bone for skinned mesh renderer on your prefab! This is bad!");
                        }


                    }



                    await default(ToWorld);
                    UnityPackageImporter.Msg("setting up bones for model.");
                    FoundMesh.SetupBones(bonemappings);
                    await default(ToBackground);

                    if (this.m_Materials != null)
                    {
                        if(this.m_Materials.Count > 0)
                        {
                                
                            UnityPackageImporter.Msg("clearing bad material objects for: \"" + FoundMesh.Slot.Name + "\"");
                            await default(ToWorld);
                            FoundMesh.Materials.Clear();
                            await default(ToBackground);

                            UnityPackageImporter.Msg("getting good material objects for: \"" + FoundMesh.Slot.Name + "\"");
                            for (int index = 0; index < this.m_Materials.Count(); index++)
                            {

                                if (this.materials.TryGetValue(index, out FileImportHelperTaskMaterial materialtask))
                                {
                                    try
                                    {
                                        await default(ToWorld);
                                        FoundMesh.Materials.Add().Target = await materialtask.runImportFileMaterialsAsync();
                                        await default(ToBackground);
                                    }
                                    catch (Exception e)
                                    {
                                        UnityPackageImporter.Warn("Could not attach material \"" + index.ToString() + "\" on mesh \"" + FoundMesh.Slot.Name + "\" from skinned mesh renderer data. It's probably not in the project or in the files you dragged over.");
                                        UnityPackageImporter.Warn("stacktrace for material \"" + index.ToString() + "\" on mesh \"" + FoundMesh.Slot.Name + "\"");
                                        UnityPackageImporter.Warn(e.Message);
                                        await default(ToWorld);
                                        FoundMesh.Materials.Add();
                                        await default(ToBackground);
                                    }
                                }
                                else
                                {
                                    UnityPackageImporter.Warn("Could not find material task for material \"" + index.ToString() + "\" on mesh \"" + FoundMesh.Slot.Name + "\" from skinned mesh renderer data. It's probably not in the project or in the files you dragged over.");
                                    await default(ToWorld);
                                    FoundMesh.Materials.Add();
                                    await default(ToBackground);
                                }
                            }
                        }
                    }

                       

                        

                    UnityPackageImporter.Msg("Setting up blend shapes");
                    await default(ToWorld);
                    FoundMesh.SetupBlendShapes();
                    await default(ToBackground);

                    UnityPackageImporter.Msg("Skinned Mesh Renderer \"" + FoundMesh.Slot.Name + "\" imported!");
                }
                else
                {
                    UnityPackageImporter.Msg("Skinned Mesh Renderer \"" + parentobj.m_Name + "\" could not find it's source mesh on the target file \""+ importedfbx.file+ "\"!");
                }

                

            }


        }


        
        

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("id: " + id.ToString());
            result.AppendLine("instanciated: " + instanciated.ToString());
            if (parentobj != null)
            {
                result.AppendLine("parentobj: " + parentobj.ToString());
            }
            else
            {
                result.AppendLine("parentobj: null");
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
            if (createdMeshRenderer != null)
            {
                result.AppendLine("createdMeshRenderer" + createdMeshRenderer.ToString());
            }
            else
            {
                result.AppendLine("createdMeshRenderer: null");
            }
            
            return base.ToString();
        }

        
    }
    //to store aabb data to bring into froox engine for skinned mesh renderers
    public class AABB
    {
        public Dictionary<string, float> m_Center;
        public Dictionary<string, float> m_Extent;
        public AABB()
        {

        }
    }
}
