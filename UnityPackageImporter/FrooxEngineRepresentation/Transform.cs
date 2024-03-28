using Assimp;
using Elements.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{

    [Serializable]
    public class Transform: IUnityObject
    {
        public Dictionary<string, int> m_GameObject;
        public floatQ m_LocalRotation;
        public float3 m_LocalPosition;
        public float3 m_LocalScale;
        public Dictionary<string, int> m_Father;

        public int m_FatherID;
        public int m_GameObjectID;

        public int id { get; set; }

        public Transform()
        {
        }

        public void instanciate()
        {
            m_FatherID = m_Father["fileID"];
            m_GameObjectID = m_GameObject["fileID"];
        }
    }
}
