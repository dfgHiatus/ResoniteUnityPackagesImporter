﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes;

[Serializable]
public class GameObject: IUnityObject
{
    public string m_Name;
    public int m_IsActive = 1;
    public bool instanciated { get; set; }
    public ulong id { get; set; }
    public SourceObj m_CorrespondingSourceObject { get; set; }
    public Dictionary<string, ulong> m_PrefabInstance { get; set; }

    public Slot frooxEngineSlot;

    public async Task InstanciateAsync(IUnityStructureImporter importer)
    {
        if (instanciated) return;

        await default(ToWorld);
        if (m_CorrespondingSourceObject.guid == null && frooxEngineSlot == null)
        {
            frooxEngineSlot = importer.unityProjectImporter.world.AddSlot(this.m_Name);
            frooxEngineSlot.SetParent(importer.CurrentStructureRootSlot, true); // Let in-game user managers not freak out that we're doing stuff in root.
            frooxEngineSlot.ActiveSelf = m_IsActive == 1 ? true : false;
        }
        else if (frooxEngineSlot != null)
        {
            // If being instanciated through prefab import while still in prefab hashes.
            // Since it will have modifications done to it but have an in world object still.
            frooxEngineSlot.ActiveSelf = m_IsActive == 1 ? true : false;
            frooxEngineSlot.Name = m_Name;
        }
        else if (importer.existingIUnityObjects.TryGetValue(m_PrefabInstance["fileID"], out IUnityObject prefab_inc))
        {
            PrefabInstance prefab = prefab_inc as PrefabInstance;
            await default(ToWorld);
            await prefab.InstanciateAsync(importer);
            await default(ToBackground);
            if (prefab.PrefabHashes.TryGetValue(m_CorrespondingSourceObject, out IUnityObject existingobject))
            {
                GameObject existing = existingobject as GameObject;
                await default(ToWorld);
                m_Name = existing.m_Name;
                frooxEngineSlot = existing.frooxEngineSlot;
                frooxEngineSlot.Name = m_Name;
                frooxEngineSlot.ActiveSelf = existing.m_IsActive == 1;
                await default(ToBackground);
            } 
        }

        await default(ToBackground);
        instanciated = true;
    }

    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        result.AppendLine("id: " + id.ToString());
        result.AppendLine("instanciated: " + instanciated.ToString());
        result.AppendLine("m_IsActive: "+ m_IsActive.ToString());

        if (m_CorrespondingSourceObject != null)
            result.AppendLine("m_CorrespondingSourceObject: " + m_CorrespondingSourceObject.ToString());
        else
            result.AppendLine("m_CorrespondingSourceObject: null");

        if (m_PrefabInstance != null)
            result.AppendLine("m_PrefabInstance: " + m_PrefabInstance.ToString());
        else
            result.AppendLine("m_PrefabInstance: null");

        if (m_Name != null)
            result.AppendLine("m_Name: " + m_Name.ToString());
        else 
            result.AppendLine("m_Name: null");

        if (frooxEngineSlot != null)
            result.AppendLine("frooxEngineSlot: " + frooxEngineSlot.ToString());
        else
            result.AppendLine("frooxEngineSlot: null");

        return result.ToString();
    }
}
