using System.Collections.Generic;
using System.Security.Util;

namespace UnityPackageImporter.FrooxEngineRepresentation
{
    public interface IUnityObject
    {
        ulong id { get; set; }
        bool instanciated { get; set; }

        void instanciate(Dictionary<ulong, IUnityObject> existing_prefab_entries);
    }
}
