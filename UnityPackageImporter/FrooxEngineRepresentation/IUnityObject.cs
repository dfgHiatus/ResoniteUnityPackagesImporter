using System.Collections.Generic;
using System.Security.Util;
using System.Threading.Tasks;

namespace UnityPackageImporter.FrooxEngineRepresentation
{
    public interface IUnityObject
    {
        ulong id { get; set; }
        bool instanciated { get; set; }
        SourceObj m_CorrespondingSourceObject {  get; set; }
        ulong m_PrefabInstance {  get; set; }
        Task instanciateAsync(Dictionary<ulong, IUnityObject> existing_prefab_entries, UnityStructureImporter importer);

    }

    public class SourceObj
    {
        public long fileID;
        public string guid;
        public int type;

        
    }
}
