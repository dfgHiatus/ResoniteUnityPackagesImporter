using FrooxEngine;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
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
        public Dictionary<string, ulong> m_GameObject;
        public int m_Enabled = 1;
        public Dictionary<int, FileImportHelperTaskMaterial> materials = new Dictionary<int, FileImportHelperTaskMaterial>();
        public string Name = string.Empty;
        public List<Dictionary<string, string>> m_Materials;
        public Dictionary<string, string> m_Mesh;
        public List<Dictionary<string, ulong>> m_Bones;
        public FrooxEngine.SkinnedMeshRenderer createdMeshRenderer;
        public GameObject parentobj;

        public AABB m_AABB;



        //this doesn't fully instanciate it, since we have to import our files all at once.
        //instead we register ourselfs into a list of meshes that wanna be imported, so we can import each model once.
        //this is done later in the unitypackage importer process, where we scan the dictionary we're adding our own id to so we can import our model and finalize.
        public async Task instanciateAsync(Dictionary<ulong, IUnityObject> existing_prefab_entries, PrefabImporter importer)
        {

            if (!instanciated)
            {
                
                instanciated = true;
                try
                {
                    importer.MeshRendererID_To_FileGUID.Add(id, m_Mesh["guid"]);
                    existing_prefab_entries.TryGetValue(m_GameObject["fileID"], out IUnityObject parentobj_inc);
                    await default(ToWorld);
                    await parentobj_inc.instanciateAsync(existing_prefab_entries, importer);
                    await default(ToBackground);
                    parentobj = parentobj_inc as GameObject;
                    Name = parentobj.m_Name;
                }
                catch
                {
                    UnityPackageImporter.Warn("The prefab is malformed!!! a skinned mesh render couldn't find it's parent an id of \"" + m_GameObject["fileID"].ToString() + "\" Your renderer will come out mis-shapen!");
                }


                await default(ToBackground);
                foreach (Dictionary<string, ulong> item in m_Bones)
                {
                    if (existing_prefab_entries.TryGetValue(item["fileID"], out IUnityObject bone_trans))
                    {
                        if (existing_prefab_entries.TryGetValue(((Transform)bone_trans).m_GameObject["fileID"], out IUnityObject bone_obj))
                        {
                            GameObject obj2 = bone_obj as GameObject;
                            await bone_obj.instanciateAsync(existing_prefab_entries, importer);
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
                    await default(ToWorld);
                    try
                    {
                        materials.Add(counter, new FileImportHelperTaskMaterial(material["guid"], importer));
                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Warn("Importing a material threw an error!");
                        UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    }
                    
                    
                    await default(ToBackground);
                    counter += 1;


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
