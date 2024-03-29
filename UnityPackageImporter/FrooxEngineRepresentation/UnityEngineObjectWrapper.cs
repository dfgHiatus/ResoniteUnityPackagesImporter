using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes;


//I hate that I have to do this, but we must wrap every object type in a wrapper, since it's like:
/*
--- !u!95 &6171425307837110800
Animator:
  serializedVersion: 3
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 6171425307844536436}
  m_Enabled: 1
  m_Avatar: {fileID: 9000000, guid: f58171603392fa247ae0cd88484fa81c, type: 3}
  m_Controller: {fileID: 0}
  m_CullingMode: 1
  m_UpdateMode: 0
  m_ApplyRootMotion: 1
  m_LinearVelocityBlending: 0
  m_WarningMessage: 
  m_HasTransformHierarchy: 1
  m_AllowConstantClipSamplingOptimization: 1
  m_KeepAnimatorControllerStateOnDisable: 0
*/
// and not:
/*
--- !u!95 &6171425307837110800
serializedVersion: 3
m_ObjectHideFlags: 0
m_CorrespondingSourceObject: {fileID: 0}
m_PrefabInstance: {fileID: 0}
m_PrefabAsset: {fileID: 0}
m_GameObject: {fileID: 6171425307844536436}
m_Enabled: 1
m_Avatar: {fileID: 9000000, guid: f58171603392fa247ae0cd88484fa81c, type: 3}
m_Controller: {fileID: 0}
m_CullingMode: 1
m_UpdateMode: 0
m_ApplyRootMotion: 1
m_LinearVelocityBlending: 0
m_WarningMessage: 
m_HasTransformHierarchy: 1
m_AllowConstantClipSamplingOptimization: 1
m_KeepAnimatorControllerStateOnDisable: 0
*/


namespace UnityPackageImporter.FrooxEngineRepresentation
{
    public class UnityEngineObjectWrapper
    {
        public Component Component;
        public GameObject GameObject;
        public Transform Transform;
        public NullType NullType;


        public UnityEngineObjectWrapper()
        {

        }

        public IUnityObject Result()
        {
            IUnityObject returned = new List<IUnityObject>(){
                NullType,
                GameObject,
                Component,
                Transform
            }.Find(i => i != null);

            if(returned != null) {
                return returned;
            }
            else
            {
                return new NullType();//in case our object couldn't be turned into an object
            }
            
        }
    }
}
