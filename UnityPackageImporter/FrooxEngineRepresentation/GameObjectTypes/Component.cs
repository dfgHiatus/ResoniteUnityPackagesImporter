using System;
using System.Collections.Generic;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    [Serializable]
    public class Component : IUnityObject
    {
        public bool instanciated { get; set; }

        public ulong id { get; set; }
        public void instanciate(Dictionary<ulong, IUnityObject> existing_prefab_entries)
        {
            if(!instanciated)
            {
                instanciated = true;
                UnityPackageImporter.Msg("Tried to instanciate unknown Component prefab element type! id:\"" + id.ToString() + "\". It is probably not supported!");
            }
            
        }
    }
}
