using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes;

public class SkinnedMeshRenderer : IUnityObject
{
    public ulong id { get; set; }
    public bool instanciated { get; set; }
    public Dictionary<string, ulong> m_GameObject;
    public int m_Enabled = 1;
    public List<FileImportHelperTaskMaterial> materials = new List<FileImportHelperTaskMaterial>();
    public List<SourceObj> m_Materials = new List<SourceObj>();
    public SourceObj m_Mesh;
    public List<Dictionary<string, ulong>> m_Bones;
    public FrooxEngine.SkinnedMeshRenderer createdMeshRenderer;
    public SourceObj m_CorrespondingSourceObject { get; set; }
    public Dictionary<string, ulong> m_PrefabInstance { get; set; }

    public AABB m_AABB;


    public async Task InstanciateAsync(IUnityStructureImporter importer)
    {
        if (instanciated) return;
        bool NotFromInLinePrefab = true;
        instanciated = true;

        try
        {

            if (m_CorrespondingSourceObject != null)
            {
                if (m_CorrespondingSourceObject.guid != null)
                {
                    UnityPackageImporter.Msg("Importing skinned mesh renderer via corrosponding object. this id is: \"" + this.id + "\"");
                    NotFromInLinePrefab = false;
                    if (importer.existingIUnityObjects.TryGetValue(m_PrefabInstance["fileID"], out IUnityObject parentobj_inc))
                    {
                        PrefabInstance prefab;
                        try
                        {
                            await default(ToWorld);
                            await parentobj_inc.InstanciateAsync(importer);
                            await default(ToWorld);
                            prefab = parentobj_inc as PrefabInstance;

                            if (prefab.PrefabHashes.TryGetValue(this.m_CorrespondingSourceObject, out IUnityObject gameobj))
                            {
                                try
                                {
                                    FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer newskin = (gameobj as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer);
                                    await default(ToWorld);
                                    await gameobj.InstanciateAsync(importer);
                                    await default(ToWorld);
                                    this.createdMeshRenderer = newskin.createdMeshRenderer;
                                    this.materials = newskin.materials;
                                    this.m_Materials = newskin.m_Materials;
                                    UnityPackageImporter.Warn("assigned skinned mesh renderer \"" + newskin.createdMeshRenderer.Slot.Name + "\" some properties from one already created by a prefab.");
                                }
                                catch (Exception ex)
                                {
                                    UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render with a guid of \"" + m_CorrespondingSourceObject.guid + "\" could not assign itself to a prefab hash!");
                                    throw ex;
                                }
                            }
                            else
                            {
                                UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render with a guid of \"" + m_CorrespondingSourceObject.guid + "\" could not find/create it's parent game obj!");
                            }
                        }
                        catch (Exception ex)
                        {
                            UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render with a guid of \"" + m_CorrespondingSourceObject.guid + "\" could not create it's parent game obj!");
                            throw ex;
                        }
                    }
                    else
                    {
                        UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render with a guid of \"" + m_CorrespondingSourceObject.guid + "\" could not find it's parent prefab obj!");
                    }

                }

            }
            GameObject parentobj = null;
            if (NotFromInLinePrefab)
            {
                UnityPackageImporter.Msg("Importing skinned mesh renderer via mesh guid. this id is: \"" + this.id + "\"");
                if (importer.unityProjectImporter.SharedImportedFBXScenes.TryGetValue(m_Mesh.guid, out FileImportTaskScene importedfbx2))
                {


                    importer.existingIUnityObjects.TryGetValue(m_GameObject["fileID"], out IUnityObject parentobj_inc);
                    parentobj = parentobj_inc as GameObject;

                    UnityPackageImporter.Msg("Importing skinned mesh renderer via mesh guid. this id is: \"" + this.id + "\", parentobj = " + parentobj.ToString());
                    foreach (var pair in importedfbx2.FILEID_To_Slot_Pairs)
                    {
                        if (pair.Value.GetType() == typeof(SkinnedMeshRenderer))
                        {
                            UnityPackageImporter.Msg("Importing skinned mesh renderer via mesh guid 2. this id is: \"" + this.id + "\", parentobj = " + parentobj.ToString());

                            FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer newskin = (pair.Value as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer);
                            if (newskin.m_Mesh.fileID == this.m_Mesh.fileID)
                            {

                                UnityPackageImporter.Msg("Assigning parent object for skinned mesh renderer: \"" + this.id + "\"");

                                await default(ToWorld);
                                await parentobj.InstanciateAsync(importer);
                                await default(ToWorld);

                                Slot newslot = newskin.createdMeshRenderer.Slot.Duplicate(parentobj.frooxEngineSlot.Parent, false, new DuplicationSettings());
                                newslot.TRS = parentobj.frooxEngineSlot.TRS;
                                foreach (Slot child in parentobj.frooxEngineSlot.Children)
                                {
                                    child.SetParent(newslot, false);
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
                    UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render with a guid of \"" + m_Mesh.guid + "\" could not find the pre-imported FBX!");
                }

                if (parentobj == null)
                {
                    UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render with a guid of \"" + m_Mesh.guid + "\" could not find/create it's parent game obj with name \"" + parentobj.frooxEngineSlot.Name + "\"!");
                }
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
            //UnityPackageImporter.Warn(ex.Message, ex.StackTrace);
            throw ex;
        }

        FrooxEngine.SkinnedMeshRenderer FoundMesh = this.createdMeshRenderer;


        int counter = 0;
        if (NotFromInLinePrefab) //this basically says that this skinned renderer is not coming from an inline prefab from a scene import
        {

            //FoundMesh.Mesh.ReferenceID = (skinnedMeshRenderer as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer).createdMeshRenderer.Mesh.ReferenceID;
            await default(ToWorld);
            importer.progressIndicator?.UpdateProgress(0f, "", "now loading a skinned mesh renderer named \"" + FoundMesh.Slot.Name + "\" ");
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
                    await obj.InstanciateAsync(importer);
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

            UnityPackageImporter.Msg("Setting up blend shapes");
            await default(ToWorld);
            FoundMesh.SetupBlendShapes();
            await default(ToBackground);




            await default(ToBackground);
            foreach (Dictionary<string, ulong> item in m_Bones)
            {
                if (importer.existingIUnityObjects.TryGetValue(item["fileID"], out IUnityObject bone_trans))
                {
                    if (importer.existingIUnityObjects.TryGetValue(((Transform)bone_trans).m_GameObject["fileID"], out IUnityObject bone_obj))
                    {
                        GameObject obj2 = bone_obj as GameObject;
                        await default(ToWorld);
                        await bone_obj.InstanciateAsync(importer);
                        await default(ToBackground);
                    }
                }
                else
                {
                    UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render  with the name \"" + FoundMesh.Slot.Name.ToString() + "\" couldn't find it's bone with an id of \"" + item["fileID"].ToString() + "\" Your renderer will come out mis-shapen!");
                }

            }








        }

        for (int i = 0; i < m_Materials.Count; i++)
        {

            SourceObj material = m_Materials[i];
            try
            {

                if (material.guid != string.Empty)
                {
                    if (importer.unityProjectImporter.AssetIDDict.ContainsKey(material.guid))
                    {
                        await default(ToWorld);
                        string filemat = importer.unityProjectImporter.AssetIDDict[material.guid];
                        try
                        {
                            materials.RemoveAt(i);
                            materials.Insert(i, new FileImportHelperTaskMaterial(material.guid, filemat, importer.unityProjectImporter));
                            UnityPackageImporter.Msg("Imported a material at index \"" + i + "\" on skinnedmeshrenderer named \"" + FoundMesh.Slot.Name + "\" by replacing one of the indices");
                        }
                        catch
                        {
                            materials.Add(new FileImportHelperTaskMaterial(material.guid, filemat, importer.unityProjectImporter));
                            UnityPackageImporter.Msg("Imported a material at index \"" + i + "\" on skinnedmeshrenderer named \"" + FoundMesh.Slot.Name + "\" by tacking onto the end.");
                        }

                        await default(ToBackground);
                    }
                    else
                    {
                        await default(ToWorld);
                        UnityPackageImporter.Msg("Importing a material at index \"" + i + "\" on skinnedmeshrenderer named \"" + FoundMesh.Slot.Name + "\" has an overridden material but we can't find the material that has the override's GUID. (is it in the package?)");
                        try
                        {
                            materials.RemoveAt(i);
                            materials.Insert(i, new FileImportHelperTaskMaterial(importer.unityProjectImporter));
                        }
                        catch
                        {
                            materials.Add(new FileImportHelperTaskMaterial(importer.unityProjectImporter));
                        }
                        await default(ToBackground);
                    }
                }
                else
                {
                    UnityPackageImporter.Msg("Importing a material at index \"" + i + "\" on skinnedmeshrenderer named \"" + FoundMesh.Slot.Name + "\" did not override. Using material generated at import.");
                }


            }
            catch (Exception e)
            {
                UnityPackageImporter.Warn("Importing a material at index \"" + i + "\" on skinnedmeshrenderer named \"" + FoundMesh.Slot.Name + "\" threw an error!");
                UnityPackageImporter.Warn(e.Message + e.StackTrace);
            }


        }

        UnityPackageImporter.Msg("clearing bad material objects for: \"" + FoundMesh.Slot.Name + "\"");
        await default(ToWorld);
        FoundMesh.Materials.Clear();
        await default(ToBackground);
        counter = 0;
        UnityPackageImporter.Msg("getting good material objects for: \"" + FoundMesh.Slot.Name + "\" it has \"" + this.materials.Count.ToString() + " \" materials in the generated list to instanciate and m_Materials being instanciated is: \"" + (m_Materials.Count > 0) + "\" ");
        foreach (FileImportHelperTaskMaterial materialtask in this.materials)
        {
            try
            {
                importer.progressIndicator?.UpdateProgress(0f, "", "now loading material " + counter.ToString() + " on a skinned mesh renderer named \"" + FoundMesh.Slot.Name + "\" ");
                await default(ToWorld);
                UnityPackageImporter.Msg("assigning material slot for: \"" + FoundMesh.Slot.Name + "\"");
                FoundMesh.Materials.Add().Target = await materialtask.runImportFileMaterialsAsync();
                await default(ToBackground);
            }
            catch (Exception e)
            {
                UnityPackageImporter.Warn("Could not attach material \"" + counter.ToString() + "\" on mesh \"" + FoundMesh.Slot.Name + "\" from skinned mesh renderer data. It's probably not in the project or in the files you dragged over.");
                UnityPackageImporter.Warn("stacktrace for material \"" + counter.ToString() + "\" on mesh \"" + FoundMesh.Slot.Name + "\"");
                UnityPackageImporter.Warn(e.Message);
                await default(ToWorld);
                FoundMesh.Materials.Add(await new FileImportHelperTaskMaterial(importer.unityProjectImporter).runImportFileMaterialsAsync());
                await default(ToBackground);
            }
            counter++;
        }

        UnityPackageImporter.Msg("Skinned Mesh Renderer \"" + FoundMesh.Slot.Name + "\" imported, and imported \"" + counter.ToString() + "\" materials with it.");
    }

    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        result.AppendLine("id: " + id.ToString());
        result.AppendLine("instanciated: " + instanciated.ToString());

        if (m_CorrespondingSourceObject != null)
            result.AppendLine("m_CorrespondingSourceObject: " + m_CorrespondingSourceObject.ToString());
        else
            result.AppendLine("m_CorrespondingSourceObject: null");

        if (m_PrefabInstance != null)
            result.AppendLine("m_PrefabInstance: " + m_PrefabInstance.ToString());
        else
            result.AppendLine("m_PrefabInstance: null");


        if (this.m_GameObject != null)
        {
            if (this.m_GameObject.ContainsKey("fileID"))
            {
                result.AppendLine("m_GameObjectID: " + this.m_GameObject["fileID"].ToString());
            }
        }
        else
        {
            result.AppendLine("m_GameObjectID: null");
        }

        if (m_Mesh != null)
            result.AppendLine("m_Mesh: " + m_Mesh.ToString());
        else
            result.AppendLine("m_Mesh: null");

        if (createdMeshRenderer != null)
            result.AppendLine("createdMeshRenderer" + createdMeshRenderer.ToString());
        else
            result.AppendLine("createdMeshRenderer: null");
        
        return result.ToString();
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
