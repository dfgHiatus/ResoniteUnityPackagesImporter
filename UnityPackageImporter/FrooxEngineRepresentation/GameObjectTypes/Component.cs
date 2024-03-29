using System;
using System.Collections.Generic;
using System.Text;

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
        //a detailed to string for debugging.
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("id: " + id.ToString());
            result.AppendLine("instanciated: " + instanciated.ToString());
            result.AppendLine("Component type is not a valid type. This is all the info it has on it above.");

            return result.ToString();
        }
    }
}
