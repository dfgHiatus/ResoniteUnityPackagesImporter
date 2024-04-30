using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    public class MonoBehaviour : IUnityObject
    {
        public ulong id { get; set; }
        public bool instanciated { get; set; }

        public Dictionary<string, ulong> m_GameObject { get; set; }
        public SourceObj m_CorrespondingSourceObject { get; set; }
        public Dictionary<string, ulong> m_PrefabInstance { get; set; }

        public async Task instanciateAsync(IUnityStructureImporter importer)
        {
            if (instanciated) return;

            if (importer.existingIUnityObjects.TryGetValue(m_GameObject["fileID"], out IUnityObject slotunity))
            {
                await default(ToWorld);
                await slotunity.instanciateAsync(importer);
                await default(ToBackground);
                Slot componenttarget = (slotunity as GameObject).frooxEngineSlot;
            }
            else
            {

            }
        }
    }
}
