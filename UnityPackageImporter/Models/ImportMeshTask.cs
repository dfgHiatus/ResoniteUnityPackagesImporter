using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter;

public class ImportMeshTask
{
    public FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer requestingMeshRenderer; //this is our key value, it tells us which mesh this model was being imported for
    public FileImportTask fileImportTask; //this can be the same as another tasks value for this very often (Aka our task may be the same as another import task). This tells us what file the mesh is in, and the task we have to wait for before it exists.
    public Slot PrefabRoot; //what prefab of the many we may be importing that this task belongs to.
    public ImportMeshTask(FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer requestingMeshRenderer, FileImportTask fileImportTask, Slot PrefabRoot)
    {
        this.fileImportTask = fileImportTask;
        this.requestingMeshRenderer = requestingMeshRenderer;
        this.PrefabRoot = PrefabRoot;
    }



    public async Task ImportMeshTaskASync()
    {
        /*var prefab = task.PrefabRoot;
        foreach (SkinnedMeshRenderer skinnedMeshrender in fileImportTask.data.skinnedRenderers)
        {
            if (!(skinnedMeshrender.Bones.Count() > 0))
            {
                skinnedMeshrender.Bones.Clear();
                Msg("setting up mesh: \"" + skinnedMeshrender.Slot.Name + "\"");
                for (int index = 0; index < task.BoneArrayIDs.Count(); index++)
                {
                    //this takes the id of the current bone slot, which is the transform component, gets it's parent which is the game object id, then turns that into a slot using entities.
                    Slot otherBone = (Slot)prefab.Entities[
                        prefab.EntityChildID_To_EntityParentID[task.BoneArrayIDs[index]]
                    ];
                    rig.Bones.AddUnique(otherBone);
                    skinnedMeshrender.Bones.Add().Target = otherBone;

                }
                
                Msg("getting material objects for: \"" + skinnedMeshrender.Slot.Name + "\"");
                skinnedMeshrender.Materials.Clear();
                for (int index = 0; index < task.materialArrayIDs.Count(); index++)
                {
                    try
                    {
                        skinnedMeshrender.Materials[index] = TasksMaterials.First(i => i.myID == task.materialArrayIDs[index]).finalMaterial;
                    }
                    catch
                    {

                        try
                        {
                            skinnedMeshrender.Materials.Add().Target = TasksMaterials.First(i => i.myID == task.materialArrayIDs[index]).finalMaterial;
                        }
                        catch (Exception e)
                        {
                            Msg("Could not attach material \"" + task.materialArrayIDs[index] + "\" from prefab data. It's probably not in the project or in the files you dragged over.");
                            Msg("stacktrace for material \"" + task.materialArrayIDs[index] + "\"");
                            Msg(e.StackTrace);
                        }
                    }

                }


                


            }

        }

        Msg("Finding our mesh for this task");
        SkinnedMeshRenderer ournewmesh;*/
        await default(ToBackground);
        FrooxEngine.SkinnedMeshRenderer renderer = this.fileImportTask.data.skinnedRenderers.Find(i => i.Slot.Name == requestingMeshRenderer.createdMeshRenderer.Slot.Name);
        UnityPackageImporter.Msg("Waiting for mesh assets for: \"" + renderer.Slot.Name + "\"");
        while (renderer.Mesh.Value == RefID.Null)
        {
            await Task.Delay(1000);
        }
        

        try
        {
            //this allows us to pull the skinned mesh renderers we imported and then delete their slots later.
            Slot oldSlot = renderer.Slot;

            if (!UnityPackageImporter.UniversalImporterPatch.oldSlots.Contains(oldSlot))
            {
                UnityPackageImporter.UniversalImporterPatch.oldSlots.Add(oldSlot);

            }

            await default(ToWorld);
            requestingMeshRenderer.createdMeshRenderer.Mesh.Target = renderer.Mesh.Target;
            requestingMeshRenderer.createdMeshRenderer.SetupBlendShapes();
            await default(ToBackground);
        }
        catch (Exception e)
        {
            UnityPackageImporter.Msg("Imported mesh failed to find it's prefab counterpart!");
            UnityPackageImporter.Msg("Import task for file \"" + fileImportTask.file + "\" tried to put its skinnedMeshRenderer Component called \"" + renderer.Slot.Name + "\" under a slot imported by the prefab importer, but it errored! Here's the stacktrace:");
            UnityPackageImporter.Msg(e.StackTrace);
        }
        await default(ToBackground);

    }
}