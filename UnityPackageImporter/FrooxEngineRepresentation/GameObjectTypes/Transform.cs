using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{

    [Serializable]
    public class Transform: IUnityObject
    {
        public Dictionary<string, ulong> m_GameObject;
        public Dictionary<string, float> m_LocalRotation;
        public Dictionary<string, float> m_LocalPosition;
        public Dictionary<string, float> m_LocalScale;
        public Dictionary<string, ulong> m_Father;
        public ulong id { get; set; }
        public bool instanciated {  get; set; }

        public ulong m_FatherID;
        public ulong m_GameObjectID;


        public Transform()
        {
        }

        //this is the magic that allows us to construct an entire game object prefab with just yaml parsing.
        public async Task instanciateAsync(Dictionary<ulong, IUnityObject> existing_prefab_entries, PrefabImporter importer)
        {
            if (!instanciated)
            {
                instanciated = true;
                m_FatherID = m_Father["fileID"];
                m_GameObjectID = m_GameObject["fileID"];
                GameObject parentobj;
                if (existing_prefab_entries.TryGetValue(m_GameObjectID, out IUnityObject foundobject) && foundobject.GetType() == typeof(GameObject))
                {
                    parentobj = foundobject as GameObject;
                    await parentobj.instanciateAsync(existing_prefab_entries, importer);

                    //heh the dictionary stuff in yamls are weird
                    await default(ToWorld);
                    parentobj.frooxEngineSlot.LocalPosition = new float3(m_LocalPosition["x"], m_LocalPosition["y"], m_LocalPosition["z"]);
                    parentobj.frooxEngineSlot.LocalRotation = new floatQ(m_LocalRotation["x"], m_LocalRotation["y"], m_LocalRotation["z"], m_LocalRotation["w"]);
                    parentobj.frooxEngineSlot.LocalScale = new float3(m_LocalScale["x"], m_LocalScale["y"], m_LocalScale["z"]);
                    await default(ToBackground);
                    if (existing_prefab_entries.TryGetValue(m_FatherID, out IUnityObject foundobjectparent) && foundobjectparent.GetType() == typeof(Transform))
                    {
                        Transform parentTransform = foundobjectparent as Transform;

                        await parentTransform.instanciateAsync(existing_prefab_entries, importer);
                        if (existing_prefab_entries.TryGetValue(parentTransform.m_GameObjectID, out IUnityObject parentobj_parent) && parentobj_parent.GetType() == typeof(GameObject))
                        {
                            await default(ToWorld);
                            parentobj.frooxEngineSlot.SetParent((parentobj_parent as GameObject).frooxEngineSlot);
                            await default(ToBackground);
                        }
                        else
                        {
                            UnityPackageImporter.Warn("The prefab is malformed!!! the transform with an id \"" + id.ToString() + "\" did not find it's parent transform's game object! this will split your import in half heiarchy wise!");
                        }
                    }
                    else
                    {

                        UnityPackageImporter.Warn("Slot did not find it's parent. If there is more than one of these messages in an import then that is bad! Transform id: " + m_FatherID.ToString());
                    }
                }
                else
                {
                    UnityPackageImporter.Warn("The prefab is malformed!!! the transform with an id \"" + id.ToString() + "\" did not find it's game object! ");
                    
                }

                
            }
            

        }

        //a detailed to string for debugging.
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("id: " + id.ToString());
            result.AppendLine("instanciated: " + instanciated.ToString());
            result.AppendLine("m_GameObjectID: " + m_GameObjectID.ToString());
            result.AppendLine("m_GameObject: " + m_GameObject.ToString());
            result.AppendLine("m_Father: " + m_Father.ToString());
            result.AppendLine("m_FatherID: " + m_FatherID.ToString());
            result.AppendLine("m_LocalRotation: " + m_LocalRotation.ToString());
            result.AppendLine("m_LocalPosition: " + m_LocalPosition.ToString());
            result.AppendLine("m_LocalScale: " + m_LocalScale.ToString());
            


            return result.ToString();
        }
    }
}
