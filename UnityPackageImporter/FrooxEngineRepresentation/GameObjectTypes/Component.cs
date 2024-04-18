using Leap.Unity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    [Serializable]
    public class Component : IUnityObject
    {
        public bool instanciated { get; set; }

        public ulong id { get; set; }
        public SourceObj m_CorrespondingSourceObject { get; set; }
        public Dictionary<string, ulong> m_PrefabInstance { get; set; }

        public async Task instanciateAsync(IUnityStructureImporter importer)
        {
            if (!instanciated)
            {
                instanciated = true;
                await new Task(() => UnityPackageImporter.Msg("Tried to instanciate unknown Component prefab element type! id:\"" + id.ToString() + "\". It is probably not supported!"));
            }
        }

        //a detailed to string for debugging.
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("id: " + id.ToString());
            result.AppendLine("instanciated: " + instanciated.ToString());
            result.AppendLine("m_CorrespondingSourceObject" + m_CorrespondingSourceObject.ToString());
            result.AppendLine("m_PrefabInstance: " + m_PrefabInstance.ToArrayString());
            result.AppendLine("Component type is not a valid type. This is all the info it has on it above.");


            return result.ToString();
        }

        
    }
}
