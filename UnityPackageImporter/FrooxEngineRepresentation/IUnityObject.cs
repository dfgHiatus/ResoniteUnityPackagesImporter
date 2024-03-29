using System.Collections.Generic;
using System.Security.Util;
using System.Threading.Tasks;

namespace UnityPackageImporter.FrooxEngineRepresentation
{
    public interface IUnityObject
    {
        ulong id { get; set; }
        bool instanciated { get; set; }

        Task instanciateAsync(Dictionary<ulong, IUnityObject> existing_prefab_entries);
    }
}
