using System.Collections.Generic;
using System.Threading.Tasks;
using FrooxEngine;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes;

public class MonoBehaviour : IUnityObject
{
    public ulong id { get; set; }
    public bool instanciated { get; set; }
    public Dictionary<string, ulong> m_GameObject { get; set; }
    public SourceObj m_CorrespondingSourceObject { get; set; }
    public Dictionary<string, ulong> m_PrefabInstance { get; set; }

    public async Task InstanciateAsync(IUnityStructureImporter importer)
    {
        if (instanciated) return;

        if (importer.existingIUnityObjects.TryGetValue(m_GameObject["fileID"], out IUnityObject slotOnity))
        {
            await default(ToWorld);
            await slotOnity.InstanciateAsync(importer);
            await default(ToBackground);
            Slot componenttarget = (slotOnity as GameObject).frooxEngineSlot;
        }
    }
}
