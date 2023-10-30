using FrooxEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityPackageImporter;

public class ImportTaskClass
{
    public Task ImportTask;
    public Slot ImportRoot;
    public string AssetID;
    public PrefabData Prefabdata;
    public List<string> BoneArrayIDs;

    public ImportTaskClass()
    {
        BoneArrayIDs = new List<string>();
    }

    public ImportTaskClass(Task t, Slot r, string id, PrefabData pd)
    {
        AssetID = id;
        ImportTask = t;
        ImportRoot = r;
        Prefabdata = pd;
        BoneArrayIDs = new List<string>();
    }
};