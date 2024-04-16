using FrooxEngine;
using Leap.Unity;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityPackageImporter.Models;
using static Oculus.Avatar.CAPI;

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
        public SourceObj m_CorrespondingSourceObject { get; set; }
        public Dictionary<string, ulong> m_PrefabInstance { get; set; }

        public AABB m_AABB;



        //this doesn't fully instanciate it, since we have to import our files all at once.
        //instead we register ourselfs into a list of meshes that wanna be imported, so we can import each model once.
        //this is done later in the unitypackage importer process, where we scan the dictionary we're adding our own id to so we can import our model and finalize.
        public async Task instanciateAsync(Dictionary<ulong, IUnityObject> existing_prefab_entries, UnityStructureImporter importer)
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
                    
                    try
                    {
                        if (importer.AssetIDDict.ContainsKey(material["guid"]))
                        {
                            await default(ToWorld);
                            string file = importer.AssetIDDict[material["guid"]];
                            var matimporttask = new FileImportHelperTaskMaterial(material["guid"], file, importer);
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

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("id: " + id.ToString());
            result.AppendLine("instanciated: " + instanciated.ToString());
            result.AppendLine("parentobj: " + parentobj.ToString());
            result.AppendLine("createdMeshRenderer" + createdMeshRenderer.ToString());
            result.AppendLine("m_CorrespondingSourceObject" + m_CorrespondingSourceObject.ToString());
            result.AppendLine("m_PrefabInstance: " + m_PrefabInstance.ToArrayString());
            return base.ToString();
        }
    }
}
