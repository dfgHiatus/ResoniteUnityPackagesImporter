using FrooxEngine;
using System.Collections.Generic;

namespace UnityPackageImporter;

public class PrefabData
{
    public Dictionary<string, IWorldElement> Entities;
    public Dictionary<string, string> EntityChildID_To_EntityParentID;
    public Slot RootSlot;

    public PrefabData()
    {
        Entities = new Dictionary<string, IWorldElement>();
        EntityChildID_To_EntityParentID = new Dictionary<string, string>();
    }

    public PrefabData(Dictionary<string, IWorldElement> Entities, Dictionary<string, string> EntityChildID_To_EntityParentID, Slot RootSlot)
    {
        this.RootSlot = RootSlot;
        this.Entities = Entities;
        this.EntityChildID_To_EntityParentID = EntityChildID_To_EntityParentID;
    }
}