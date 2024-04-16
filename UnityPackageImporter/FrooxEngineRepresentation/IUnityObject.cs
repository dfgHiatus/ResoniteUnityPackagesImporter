using Elements.Assets;
using System;
using System.Collections.Generic;
using System.Security.Util;
using System.Threading.Tasks;

namespace UnityPackageImporter.FrooxEngineRepresentation
{
    public interface IUnityObject
    {
        ulong id { get; set; }
        bool instanciated { get; set; }
        SourceObj m_CorrespondingSourceObject { get; set; }
        ulong m_PrefabInstance { get; set; }
        Task instanciateAsync(Dictionary<ulong, IUnityObject> existing_prefab_entries, UnityStructureImporter importer);

    }

    public class SourceObj
    {
        public long fileID;
        public string guid;
        public int type;
    }

    public class SourceObjCompare : EqualityComparer<SourceObj>
    {
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
