using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityPackageImporter.FrooxEngineRepresentation;
using UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes;
using UnityPackageImporter.Models;

namespace UnityPackageImporter;

public class ImportMeshTask
{
    public FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer requestingMeshRenderer; //this is our key value, it tells us which mesh this model was being imported for
    public FileImportTask fileImportTask; //this can be the same as another tasks value for this very often (Aka our task may be the same as another import task). This tells us what file the mesh is in, and the task we have to wait for before it exists.
    public Slot PrefabRoot; //what prefab of the many we may be importing that this task belongs to.
    public UnityStructureImporter importer;
    public ImportMeshTask(FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer requestingMeshRenderer, FileImportTask fileImportTask, Slot PrefabRoot, UnityStructureImporter importer)
    {
        this.fileImportTask = fileImportTask;
        this.requestingMeshRenderer = requestingMeshRenderer;
        this.PrefabRoot = PrefabRoot;
        this.importer = importer;
    }

    public async Task ImportMeshTaskRunner()
    {
        await default(ToWorld);
        UnityPackageImporter.Msg("finding skin mesh renderer.");
        FrooxEngine.SkinnedMeshRenderer FoundMesh = this.fileImportTask.data.skinnedRenderers.Find(i => i.Slot.Name == requestingMeshRenderer.Name);
        requestingMeshRenderer.createdMeshRenderer = FoundMesh;
        if (FoundMesh == null)
        {
            UnityPackageImporter.Warn("Could not find skinned mesh renderer!! dumping data for debug!");
            UnityPackageImporter.Msg("mesh renderer we're trying to find: \"" + requestingMeshRenderer.Name + "\"");
            UnityPackageImporter.Msg("Mesh slot names:");
            foreach (FrooxEngine.SkinnedMeshRenderer mesh in this.fileImportTask.data.skinnedRenderers)
            {
                UnityPackageImporter.Msg("\""+mesh.Slot.Name+"\"");
            }
            return;
        }


        UnityPackageImporter.Msg("Waiting for mesh assets for: \"" + FoundMesh.Slot.Name + "\"");
        await default(ToWorld);
        while (!FoundMesh.Mesh.IsAssetAvailable)
        {
            await default(NextUpdate);
            await Task.Delay(1000);
        }
        UnityPackageImporter.Msg("finished waiting for mesh assets for: \"" + FoundMesh.Slot.Name + "\"");

        await default(ToWorld);
        FoundMesh.Enabled = requestingMeshRenderer.m_Enabled == 1; //in case it's disabled in unity, this will make it disabled when imported.
        FoundMesh.Slot.ActiveSelf = requestingMeshRenderer.parentobj.frooxEngineSlot.ActiveSelf; //same here
        requestingMeshRenderer.parentobj.frooxEngineSlot.Destroy(); //get rid of the old slot, so we have the one froox engine imported with assimp.
        requestingMeshRenderer.parentobj.frooxEngineSlot = requestingMeshRenderer.createdMeshRenderer.Slot; //change it's slot to the new one so we don't cause strange errors with other code.
        
        FoundMesh.ExplicitLocalBounds.Value = 
            Elements.Core.BoundingBox.CenterSize(
                new Elements.Core.float3(
                    requestingMeshRenderer.m_AABB.m_Center["x"],
                    requestingMeshRenderer.m_AABB.m_Center["y"],
                    requestingMeshRenderer.m_AABB.m_Center["z"]), 
                new Elements.Core.float3(requestingMeshRenderer.m_AABB.m_Extent["x"], 
                requestingMeshRenderer.m_AABB.m_Extent["y"], 
                requestingMeshRenderer.m_AABB.m_Extent["z"]
                ));
        FoundMesh.BoundsComputeMethod.Value = SkinnedBounds.Explicit; //use Unity's skinned bounds methods. 
        await default(ToBackground);
        
        Dictionary<string, Slot> bonemappings = new Dictionary<string, Slot>();
        UnityPackageImporter.Msg("gathering bones for model.");
        foreach (Dictionary<string, ulong> bonemap in requestingMeshRenderer.m_Bones)
        { 
            if (this.importer.unityprefabimports.TryGetValue(bonemap["fileID"], out IUnityObject unityObject))
            {
                FrooxEngineRepresentation.GameObjectTypes.Transform obj = unityObject as FrooxEngineRepresentation.GameObjectTypes.Transform;
                this.importer.unityprefabimports.TryGetValue(obj.m_GameObjectID, out IUnityObject unityObjectGame);
                GameObject gamneobj = unityObjectGame as GameObject;
                bonemappings.Add(gamneobj.m_Name, gamneobj.frooxEngineSlot);
            }
            else
            {
                UnityPackageImporter.Msg("Couldn't find bone for skinned mesh renderer on your prefab! This is bad!");
            }


        }
        await default(ToWorld);
        UnityPackageImporter.Msg("setting up bones for model.");
        FoundMesh.SetupBones(bonemappings);
        await default(ToBackground);
        await default(ToWorld);
        UnityPackageImporter.Msg("clearing bad material objects for: \"" + FoundMesh.Slot.Name + "\"");
        FoundMesh.Materials.Clear();
        await default(ToBackground);
        UnityPackageImporter.Msg("getting good material objects for: \"" + FoundMesh.Slot.Name + "\"");
        for (int index = 0; index < requestingMeshRenderer.m_Materials.Count(); index++)
        {
            
            if (requestingMeshRenderer.materials.TryGetValue(index, out FileImportHelperTaskMaterial materialtask))
            {
                try
                {
                    await default(ToWorld);
                    FoundMesh.Materials.Add().Target = await materialtask.runImportFileMaterialsAsync();
                    await default(ToBackground);
                }
                catch (Exception e)
                {
                    UnityPackageImporter.Msg("Could not attach material \"" + index.ToString() + "\" on mesh \"" + FoundMesh.Slot.Name + "\" from prefab data. It's probably not in the project or in the files you dragged over.");
                    UnityPackageImporter.Msg("stacktrace for material \"" + index.ToString() + "\" on mesh \"" + FoundMesh.Slot.Name + "\"");
                    UnityPackageImporter.Msg(e.Message);
                    await default(ToWorld);
                    FoundMesh.Materials.Add();
                    await default(ToBackground);
                }
            }
                
            

        }

        UnityPackageImporter.Msg("Setting up blend shapes");
        await default(ToWorld);
        FoundMesh.SetupBlendShapes();
        await default(ToBackground);

    }
}