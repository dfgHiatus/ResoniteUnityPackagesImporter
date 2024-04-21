using Elements.Assets;
using Elements.Core;
using System;
using System.Collections.Generic;
using System.Security.Util;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation
{
    public interface IUnityObject
    {
        ulong id { get; set; }
        bool instanciated { get; set; }
        SourceObj m_CorrespondingSourceObject { get; set; }
        Dictionary<string, ulong> m_PrefabInstance { get; set; }
        Task instanciateAsync(IUnityStructureImporter importer);

    }


    public class SourceObj
    {
        public long fileID { get; set; }
        public string guid { get; set; }
        public int type { get; set; }

        public SourceObj() { }
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            try
            {
                result.AppendLine("fileID: " + fileID.ToString());
            }
            catch
            {
                result.AppendLine("fileID: null");

            }
            try
            {
                result.AppendLine("guid: " + guid.ToString());
            }
            catch
            {
                result.AppendLine("guid: null");

            }
            try
            {
                result.AppendLine("type: " + type.ToString());
            }
            catch
            {
                result.AppendLine("type: null");

            }
            
            return result.ToString();
        }
    }

    public class ModsPrefab
    {
        public SourceObj target;
        public string propertyPath;
        public string value;
        public Dictionary<string, string> objectReference;

        public ModsPrefab() { }
        public ModsPrefab(SourceObj target, string propertyPath, string value, Dictionary<string, string> objectReference)
        {
            this.target = target;
            this.propertyPath = propertyPath;
            this.value = value;
            this.objectReference = objectReference;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (target != null)
            {
                result.AppendLine("target: " + target.ToString());
            }
            else
            {
                result.AppendLine("target: null");
            }
            if (propertyPath != null)
            {
                result.AppendLine("propertyPath: " + propertyPath.ToString());
            }
            else
            {
                result.AppendLine("propertyPath: null");
            }
            if (value != null)
            {
                result.AppendLine("value: " + value.ToString());
            }
            else
            {
                result.AppendLine("value: null");
            }
            if (objectReference != null)
            {
                foreach (var pair in objectReference)
                {
                    result.AppendLine("objectReference+Item: (" + pair.Key.ToString() + ", " + pair.Value.ToString());
                }
            }
            else
            {
                result.AppendLine("objectReference: null");
            }
            return result.ToString();
        }
    }

    public class ModPrefab
    {
        public Dictionary<string, ulong> m_TransformParent;
        public List<ModsPrefab> m_Modifications;
        public List<string> m_RemovedComponents;

        public ModPrefab()
        {

        }
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (m_TransformParent != null)
            {
                foreach(var pair in m_TransformParent)
                {
                    result.AppendLine("m_TransformParent+Item: (" + pair.Key.ToString() +", "+pair.Value.ToString());
                }
                

            }
            else
            {
                result.AppendLine("m_TransformParent: null");
            }
            
            if (m_Modifications != null)
            {
                foreach (var pair in m_Modifications)
                {
                    result.AppendLine("m_Modifications+Item: " + pair.ToString());
                }

            }
            else
            {
                result.AppendLine("m_Modification: null");
            }
            return result.ToString();
        }
    }

    public class SourceObjCompare : EqualityComparer<SourceObj>
    {//unitysceneimportsPrefabs.AddRange(importtask.FILEID_To_Slot_Pairs);
        public override bool Equals(SourceObj x, SourceObj y)
        {
            return x.fileID == y.fileID && x.guid == y.guid;
        }

        public override int GetHashCode(SourceObj obj)
        {
            return (obj.fileID.ToString() + obj.guid).GetHashCode();
        }

        
    }

}
