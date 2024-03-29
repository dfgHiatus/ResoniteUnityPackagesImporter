using FrooxEngine;
using System.Collections.Generic;

namespace UnityPackageImporter;

public class PrefabData
{
    public Slot RootSlot;

    public PrefabData()
    {
        //Entities = new Dictionary<string, IWorldElement>();
    }

    public PrefabData(Dictionary<string, IWorldElement> Entities, Dictionary<string, string> EntityChildID_To_EntityParentID, Slot RootSlot)
    {
        
    }
}