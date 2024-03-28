using Elements.Core;
using System;
using System.Collections.Generic;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{

    [Serializable]
    public class Transform: IUnityObject
    {
        public Dictionary<string, ulong> m_GameObject;
        public floatQ m_LocalRotation;
        public float3 m_LocalPosition;
        public float3 m_LocalScale;
        public Dictionary<string, ulong> m_Father;
        public ulong id { get; set; }
        public bool instanciated {  get; set; }

        public ulong m_FatherID;
        public ulong m_GameObjectID;


        public Transform()
        {
        }

        //this is the magic that allows us to construct an entire game object prefab with just yaml parsing.
        public void instanciate(Dictionary<ulong, IUnityObject> existing_prefab_entries)
        {
            if (!instanciated)
            {
                instanciated = true;
                m_FatherID = m_Father["fileID"];
                m_GameObjectID = m_GameObject["fileID"];
                GameObject parentobj;
                if (existing_prefab_entries.TryGetValue(m_GameObjectID, out IUnityObject foundobject) && foundobject.GetType() == typeof(GameObject))
                {
                    parentobj = (GameObject)foundobject;
                    parentobj.instanciate(existing_prefab_entries);

                    parentobj.frooxEngineSlot.LocalPosition = m_LocalPosition;
                    parentobj.frooxEngineSlot.LocalRotation = m_LocalRotation;
                    parentobj.frooxEngineSlot.LocalScale = m_LocalScale;
                    if (existing_prefab_entries.TryGetValue(m_FatherID, out IUnityObject foundobjectparent) && foundobject.GetType() == typeof(Transform))
                    {
                        Transform parentTransform = (Transform)foundobjectparent;

                        parentTransform.instanciate(existing_prefab_entries);
                        GameObject parentobj_parent = (GameObject)existing_prefab_entries.GetValueOrDefault(parentTransform.m_GameObjectID);
                        if (parentobj_parent != null)
                        {
                            parentobj.frooxEngineSlot.SetParent(parentobj_parent.frooxEngineSlot);
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
    }
}
