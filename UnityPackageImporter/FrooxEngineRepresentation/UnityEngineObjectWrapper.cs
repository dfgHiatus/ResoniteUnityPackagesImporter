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


        /// <summary>
        /// Use game object as a reference point.
        /// this is how long the objects are generally gonna take. These values are realtive and don't represent time units.
        /// instead, these get added one by one depending on the objects being imported into a total number.
        /// </summary>
        public static Dictionary<Type, int> addedProgress = new Dictionary<Type, int>
        {
            {typeof(Component), 0},
            {typeof(GameObject), 1},
            {typeof(Transform), 2},
            {typeof(NullType), 0},
            {typeof(SkinnedMeshRenderer), 10},
            {typeof(PrefabInstance), 20},
            {typeof(RotationConstraint), 2},
            {typeof(MeshCollider), 10},
            {typeof(MonoBehaviour), 30}
        };
        public Component Component;
        public GameObject GameObject;
        public Transform Transform;
        public NullType NullType;
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        public PrefabInstance PrefabInstance;
        public RotationConstraint RotationConstraint;
        public MeshCollider MeshCollider;
        public MonoBehaviour MonoBehaviour;

        public UnityEngineObjectWrapper()
        {

        }

        public IUnityObject Result()
        {
            IUnityObject returned = new List<IUnityObject>(){
                PrefabInstance,
                GameObject,
                Component,
                Transform,
                SkinnedMeshRenderer,
                MeshCollider,
                RotationConstraint,
                MonoBehaviour,
                NullType
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
