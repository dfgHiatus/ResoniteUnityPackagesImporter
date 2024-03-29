using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    public class SkinnedMeshRenderer : IUnityObject
    {
        public ulong id { get; set; }
        public bool instanciated { get; set; }
        public int m_Enabled = 1;
        public List<Dictionary<string, string>> m_Materials;
        public Dictionary<string, string> m_Mesh;
        public List<Dictionary<string, ulong>> m_Bones;
        public Dictionary <string, ulong> m_GameObject; //so we know what we belong to.
        public FrooxEngine.SkinnedMeshRenderer createdMeshRenderer;

        public AABB m_AABB;



        //this doesn't fully instanciate it, since we have to import our files all at once.
        //instead we register ourselfs into a list of meshes that wanna be imported, so we can import each model once.
        //this is done later in the unitypackage importer process, where we scan the dictionary we're adding our own id to so we can import our model and finalize.
        public async Task instanciateAsync(Dictionary<ulong, IUnityObject> existing_prefab_entries)
        {

            if (!instanciated)
            {
                instanciated = true;

                if (existing_prefab_entries.TryGetValue(m_GameObject["fileID"], out IUnityObject parentobj))
                {

                    GameObject obj = parentobj as GameObject;
                    await obj.instanciateAsync(existing_prefab_entries);

                    UnityPackageImporter.UniversalImporterPatch.MeshRendererID_To_FileGUID.Add(id, m_Mesh["guid"]);

                    await default(ToWorld);
                    createdMeshRenderer = obj.frooxEngineSlot.AttachComponent<FrooxEngine.SkinnedMeshRenderer>();
                    createdMeshRenderer.Enabled = m_Enabled == 1; //in case it's disabled in unity, this will make it disabled when imported.
                    createdMeshRenderer.ExplicitLocalBounds.Value = Elements.Core.BoundingBox.CenterSize(new Elements.Core.float3(m_AABB.m_Center["x"], m_AABB.m_Center["y"], m_AABB.m_Center["z"]), new Elements.Core.float3(m_AABB.m_Extent["x"], m_AABB.m_Extent["y"], m_AABB.m_Extent["z"]));
                    createdMeshRenderer.BoundsComputeMethod.Value = SkinnedBounds.Explicit; //use Unity's skinned bounds methods. 
                    await default(ToBackground);

                    foreach (Dictionary<string, ulong> item in m_Bones)
                    {
                        if (existing_prefab_entries.TryGetValue(item["fileID"], out IUnityObject bone_trans))
                        {
                            if (existing_prefab_entries.TryGetValue(((Transform)bone_trans).m_GameObject["fileID"], out IUnityObject bone_obj))
                            {
                                GameObject obj2 = bone_obj as GameObject;
                                await bone_obj.instanciateAsync(existing_prefab_entries);
                                await default(ToWorld);
                                createdMeshRenderer.Bones.Add().Target = obj2.frooxEngineSlot;//create our bone list for when we import.
                                await default(ToBackground);
                            }
                        }
                        else
                        {
                            UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render couldn't find it's bone with an id of \"" + item["fileID"].ToString() + "\" Your renderer will come out mis-shapen!");
                        }

                    }

                   
                }
                else
                {
                    UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render couldn't find it's attach-to Slot with an id of \"" + m_GameObject["fileID"].ToString() + "\" Your renderer was not created!");
                }
                

                
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
}
