using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    public class NullType : IUnityObject
    {
        public bool instanciated { get; set; }
        public ulong id { get; set; }
        public SourceObj m_CorrespondingSourceObject { get; set; }
        public ulong m_PrefabInstance { get; set; }

        //who cares. we're just doing this to implement the interface and all we're doing is shoving in a message.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task instanciateAsync(Dictionary<ulong, IUnityObject> existing_prefab_entries, UnityStructureImporter importer)
        #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if(!instanciated)
            {
                instanciated = true;
                UnityPackageImporter.Msg("Tried to instanciate unknown prefab element type! id:\"" + id.ToString() + "\". No idea what it is but check your prefab for that using control+f.");
            }
        }


        //a detailed to string for debugging.
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("id: " + id.ToString());
            result.AppendLine("instanciated: " + instanciated.ToString());
            result.AppendLine("Null type is not a valid type. This is all the info it has on it above.");


            return result.ToString();
        }
    }
}
