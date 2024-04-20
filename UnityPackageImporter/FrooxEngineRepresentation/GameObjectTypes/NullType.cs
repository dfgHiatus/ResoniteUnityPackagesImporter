
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    public class NullType : IUnityObject
    {
        public bool instanciated { get; set; }
        public ulong id { get; set; }
        public SourceObj m_CorrespondingSourceObject { get; set; }
        public Dictionary<string, ulong> m_PrefabInstance { get; set; }

        #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task instanciateAsync(IUnityStructureImporter importer)
        #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if(!instanciated)
            {
                instanciated = true;
                UnityPackageImporter.Msg("Tried to instanciate unknown element type! id:\"" + id.ToString() + "\". No idea what it is but check your prefab for that using control+f.");
            }
        }


        //a detailed to string for debugging.
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("id: " + id.ToString());
            result.AppendLine("instanciated: " + instanciated.ToString());
            if (m_CorrespondingSourceObject != null)
            {
                result.AppendLine("m_CorrespondingSourceObject: " + m_CorrespondingSourceObject.ToString());

            }
            else
            {
                result.AppendLine("m_CorrespondingSourceObject: null");
            }
            if (m_PrefabInstance != null)
            {
                result.AppendLine("m_PrefabInstance: " + m_PrefabInstance.ToString());
            }
            else
            {
                result.AppendLine("m_PrefabInstance: null");
            }
            result.AppendLine("Null type is not a valid type. This is all the info it has on it above.");



            return result.ToString();
        }
    }
}
