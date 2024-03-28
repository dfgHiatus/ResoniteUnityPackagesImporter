using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    internal class NullType : IUnityObject
    {
        public bool instanciated { get; set; }
        public ulong id { get; set; }
        public void instanciate(Dictionary<ulong, IUnityObject> existing_prefab_entries)
        {
            if(!instanciated)
            {
                instanciated = true;
                UnityPackageImporter.Msg("Tried to instanciate unknown prefab element type! id:\"" + id.ToString() + "\". No idea what it is but check your prefab for that using control+f.");
            }
        }
    }
}
