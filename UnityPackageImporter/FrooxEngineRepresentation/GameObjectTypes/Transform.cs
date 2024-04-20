using Elements.Core;
using FrooxEngine;
using Leap.Unity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    public class Transform: IUnityObject
    {
        
        public TransformFloat4 m_LocalRotation;
        public TransformFloat3 m_LocalPosition;
        public TransformFloat3 m_LocalScale;
        
        
        public ulong id { get; set; }
        public int m_RootOrder;
        public bool instanciated {  get; set; }
        
        public Dictionary<string, ulong> m_PrefabInstance { get; set; }

        public ulong m_FatherID; //this is used during instanciation, and should never be used elsewhere. besides, you'll never need this for attaching components or anything - @989onan 
        public ulong m_GameObjectID; //use this when trying to find the parent gameobject of a component that points to a transform.
        public Dictionary<string, ulong> m_GameObject;
        public Dictionary<string, ulong> m_Father;
        public GameObject parentHashedGameObj; //instanciated on Transforms imported by FBXs. We can use this to find our parent game object via the prefab file hashing system.

        public SourceObj m_CorrespondingSourceObject { get; set; }

        public Transform()
        {
        }

        //this is the magic that allows us to construct an entire game object prefab with just yaml parsing.
        public async Task instanciateAsync(IUnityStructureImporter importer)
        {
            if (!instanciated)
            {
                instanciated = true;
                
                if (m_CorrespondingSourceObject.guid == null)
                {
                    await createSelf(importer);
                }
                else
                {
                    if (importer.existingIUnityObjects.TryGetValue(m_PrefabInstance["fileID"], out IUnityObject PrefabInstanceObject))
                    {
                        PrefabInstance prefab = PrefabInstanceObject as PrefabInstance;
                        await default(ToWorld);
                        await prefab.instanciateAsync(importer);
                        await default(ToBackground);

                        if (prefab.PrefabHashes.TryGetValue(m_CorrespondingSourceObject, out IUnityObject targetObj)) {

                            Transform alreadydefined = (targetObj as Transform);



                            

                            try
                            {
                                this.parentHashedGameObj = alreadydefined.parentHashedGameObj;

                                

                                foreach (IUnityObject variab in importer.existingIUnityObjects.Values)
                                {
                                    if(variab.GetType() == typeof(GameObject))
                                    {
                                        GameObject actual = variab as GameObject;
                                        if(actual.m_CorrespondingSourceObject != null)
                                        {
                                            if(actual.m_CorrespondingSourceObject.fileID != 0)
                                            {
                                                if (actual.m_CorrespondingSourceObject.fileID == alreadydefined.parentHashedGameObj.m_CorrespondingSourceObject.fileID
                                                    && actual.m_CorrespondingSourceObject.guid.Equals(alreadydefined.parentHashedGameObj.m_CorrespondingSourceObject.guid)
                                                    )
                                                {
                                                    this.parentHashedGameObj = actual;
                                                }
                                            }
                                        }

                                    }

                                }

                                await default(ToWorld);
                                await this.parentHashedGameObj.instanciateAsync(importer);
                                await default(ToBackground);
                                if (this.parentHashedGameObj.id == alreadydefined.parentHashedGameObj.id)
                                {
                                    try
                                    {
                                        UnityPackageImporter.Warn("The inline prefab is malformed!!! the transform with an id \"" + id.ToString() + "\" and a target game object id of \"" + alreadydefined.parentHashedGameObj + "\"did not find it's parent transform's game object! this will split your import in half heiarchy wise! This should never happen!");
                                    }
                                    catch
                                    {
                                        UnityPackageImporter.Warn("The inline prefab is malformed!!! the transform with an id \"" + id.ToString() + "\" and a guid of \"null\"did not find it's parent transform's game object! this will split your import in half heiarchy wise! This should never happen!");
                                    }
                                }



                                this.m_GameObject = new Dictionary<string, ulong>
                                {
                                    { "fileID", this.parentHashedGameObj.id}
                                };
                                m_GameObjectID = m_GameObject["fileID"];

                                this.m_LocalPosition = new TransformFloat3(this.parentHashedGameObj.frooxEngineSlot.LocalPosition.x, this.parentHashedGameObj.frooxEngineSlot.LocalPosition.y, this.parentHashedGameObj.frooxEngineSlot.LocalPosition.z);
                                this.m_LocalRotation = new TransformFloat4(this.parentHashedGameObj.frooxEngineSlot.LocalRotation.x, this.parentHashedGameObj.frooxEngineSlot.LocalRotation.y, this.parentHashedGameObj.frooxEngineSlot.LocalRotation.z, this.parentHashedGameObj.frooxEngineSlot.LocalRotation.w);
                                this.m_LocalScale = new TransformFloat3(this.parentHashedGameObj.frooxEngineSlot.LocalScale.x, this.parentHashedGameObj.frooxEngineSlot.LocalScale.y, this.parentHashedGameObj.frooxEngineSlot.LocalScale.z);
                            }
                            catch (Exception ex)
                            {
                                UnityPackageImporter.Warn("The inline prefab is malformed!!! the transform with an id \"" + id.ToString() + "\" did not find it's parent transform's game object! this will split your import in half heiarchy wise! This should never happen!");
                                UnityPackageImporter.Warn(ex.Message, ex.StackTrace);
                            }
                            

                        }
                    }
                    instanciated = true;


                }



            }



        }

        private async Task createSelf(IUnityStructureImporter importer)
        {
            m_FatherID = m_Father["fileID"];
            m_GameObjectID = m_GameObject["fileID"];
            if (importer.existingIUnityObjects.TryGetValue(m_GameObjectID, out IUnityObject foundobject) && foundobject.GetType() == typeof(GameObject))
            {
                
                
                GameObject parentobj;
                parentobj = foundobject as GameObject;
                await parentobj.instanciateAsync(importer);

                //heh the dictionary stuff in yamls are weird
                await default(ToWorld);
                parentobj.frooxEngineSlot.LocalPosition = new float3(m_LocalPosition.x, m_LocalPosition.y, m_LocalPosition.z);
                parentobj.frooxEngineSlot.LocalRotation = new floatQ(m_LocalRotation.x, m_LocalRotation.y, m_LocalRotation.z, m_LocalRotation.w);
                parentobj.frooxEngineSlot.LocalScale = new float3(m_LocalScale.x, m_LocalScale.y, m_LocalScale.z);
                await default(ToBackground);
                if (importer.existingIUnityObjects.TryGetValue(m_FatherID, out IUnityObject foundobjectparent) && foundobjectparent.GetType() == typeof(Transform))
                {
                    Transform parentTransform = foundobjectparent as Transform;

                    await parentTransform.instanciateAsync(importer);
                    if (importer.existingIUnityObjects.TryGetValue(parentTransform.m_GameObjectID, out IUnityObject parentobj_parent) && parentobj_parent.GetType() == typeof(GameObject))
                    {
                        await default(ToWorld);
                        parentobj.frooxEngineSlot.SetParent((parentobj_parent as GameObject).frooxEngineSlot, false);
                        await default(ToBackground);
                    }
                    else if(parentTransform.parentHashedGameObj != null)
                    {
                        //if this transform was instanciated through the FileImportTaskScene class, we parent ourselves to it's game object here, which is filled by the FileImportTaskScene class.
                        await default(ToWorld);
                        parentobj.frooxEngineSlot.SetParent(parentTransform.parentHashedGameObj.frooxEngineSlot, false);
                        await default(ToBackground);
                    }
                    else
                    {
                        UnityPackageImporter.Warn("The prefab is malformed!!! the transform with an id \"" + id.ToString() + "\" did not find it's parent transform's game object! this will split your import in half heiarchy wise!");
                    }
                }
                else
                {
                    //this is to solve a problem where the root of prefabs can be called 
                    if(m_FatherID == 0)
                    {
                        parentobj.frooxEngineSlot.Name = "RootNode";
                    }
                    


                    UnityPackageImporter.Warn("Slot did not find it's parent. If there is more than one of these messages in an import then that is bad! Transform id: " + m_FatherID.ToString());
                }
            }
            else
            {
                UnityPackageImporter.Warn("The prefab is malformed!!! the transform with an id \"" + id.ToString() + "\" did not find it's game object! ");

            }
        }

        //a detailed to string for debugging.
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("id: " + id.ToString());
            result.AppendLine("instanciated: " + (instanciated.ToString()));
            result.AppendLine("m_GameObjectID: " + (m_GameObjectID == 0? "null":m_GameObjectID.ToString()));
            result.AppendLine("m_GameObject: " + (m_GameObject == null ? "null" : m_GameObject.ToString()));
            result.AppendLine("m_Father: " + (m_Father == null ? "null" : m_Father.ToString()));
            result.AppendLine("m_FatherID: " + (m_FatherID == 0 ? "null" : m_FatherID.ToString()));
            result.AppendLine("m_LocalRotation: " + (m_LocalRotation == null ? "null" : m_LocalRotation.ToString()));
            result.AppendLine("m_LocalPosition: " + (m_LocalPosition == null ? "null" : m_LocalPosition.ToString()));
            result.AppendLine("m_LocalScale: " + (m_LocalScale == null ? "null" : m_LocalScale.ToString()));
            result.AppendLine("m_CorrespondingSourceObject" + (m_CorrespondingSourceObject == null ? "null" : m_CorrespondingSourceObject.ToString()));
            result.AppendLine("m_PrefabInstance: " + (m_PrefabInstance == null ? "null" : m_PrefabInstance.ToString()));
            


            return result.ToString();
        }

        public async Task UpdateSelf(IUnityStructureImporter importer)
        {
            await createSelf(importer);
        }
    }

    public class TransformFloat4
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("x: " + x.ToString());
            result.AppendLine("y: " + y.ToString());
            result.AppendLine("z: " + z.ToString());
            result.AppendLine("w: " + w.ToString());

            return result.ToString();
        }

        public TransformFloat4()
        {
        }
        public TransformFloat4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

    }

    public class TransformFloat3
    {
        public TransformFloat3()
        {
        }
        public float x;
        public float y;
        public float z;

        public TransformFloat3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("x: " + x.ToString());
            result.AppendLine("y: " + y.ToString());
            result.AppendLine("z: " + z.ToString());

            return result.ToString();
        }
    }
}
