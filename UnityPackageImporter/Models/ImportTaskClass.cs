using FrooxEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter;

public class ImportTaskClass
{
    public Slot ImportRoot; //this is the slot which we want our mesh on that's being imported by the import task.
    public string meshFileID; //this is our key value, it tells us which mesh this model was being imported for
    public FileImportHelperTask fileImportTask; //this can be the same as another tasks value for this very often. This tells us what file the mesh is in, and the task we have to wait for before it exists.
    public PrefabData Prefabdata; //keeps stuff like the parent child relationships for the prefab that this model is being attached to once imported.
    public List<string> BoneArrayIDs; // this tells us what bones our mesh needs to use when imported.

    public ImportTaskClass()
    {
        BoneArrayIDs = new List<string>();
    }

    public ImportTaskClass(FileImportHelperTask fileImportTask, string thisSlotsMeshID, PrefabData pd)
    {
        this.fileImportTask = fileImportTask;
        Prefabdata = pd;
        BoneArrayIDs = new List<string>();
    }
}