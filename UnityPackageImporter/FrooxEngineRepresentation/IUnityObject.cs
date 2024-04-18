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
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("fileID: " + fileID.ToString());
            result.AppendLine("guid: " + guid.ToString());
            result.AppendLine("type: " + type.ToString());
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
