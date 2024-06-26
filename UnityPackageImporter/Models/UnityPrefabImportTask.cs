using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using SkyFrost.Base;
using UnityPackageImporter.FrooxEngineRepresentation;


namespace UnityPackageImporter.Models;

internal class UnityPrefabImportTask : IUnityStructureImporter
{
    public List<Slot> oldSlots = new List<Slot>();
    public Dictionary<ulong, IUnityObject> existingIUnityObjects { get; set; }
    public Slot CurrentStructureRootSlot { get; set; }
    public Slot allimportsroot { get; set; }
    public KeyValuePair<string, string> ID { get; set; }
    private float3 GlobalIndicatorPosition;
    public ProgressBarInterface progressIndicator { get; set; }
    public UnityProjectImporter unityProjectImporter { get; set; }

    public UnityPrefabImportTask(float3 globalPosition, Slot root, KeyValuePair<string, string> ID, UnityProjectImporter unityProjectImporter)
    {
        this.ID = ID;
        this.unityProjectImporter = unityProjectImporter;
        this.allimportsroot = root;
        this.GlobalIndicatorPosition = globalPosition;
    }

    public async Task StartImport()
    {
        StringBuilder debugPrefab = new StringBuilder();
        try 
        {
            existingIUnityObjects = new Dictionary<ulong, IUnityObject>();
            await default(ToWorld);
            this.CurrentStructureRootSlot = unityProjectImporter.world.AddSlot(Path.GetFileName(ID.Value));
            this.CurrentStructureRootSlot.SetParent(this.allimportsroot, false);
            this.CurrentStructureRootSlot.GlobalPosition = this.GlobalIndicatorPosition;
            Slot indicator = this.unityProjectImporter.root.AddSlot("Unity Prefab Import Indicator");
            indicator.GlobalPosition = this.GlobalIndicatorPosition;
            indicator.PersistentSelf = false;
            this.progressIndicator = await indicator.SpawnEntity<ProgressBarInterface, LegacySegmentCircleProgress>(FavoriteEntity.ProgressBar);
            progressIndicator?.Initialize(false);
            await default(ToBackground);


            progressIndicator?.UpdateProgress(0f, "", "now loading unity YAML objects for Prefab.");
            // We first have to remove "stripped" since those cause yaml parsing errors
            string[] initialstream = File.ReadAllLines(ID.Value);
            string[] newcontent = new string[initialstream.Length];
            for (int i = 0; i < initialstream.Length; i++)
            {
                string line = initialstream[i];
                newcontent[i] = line;
                if (line.StartsWith("--- !u!"))
                {
                    newcontent[i] = newcontent[i].Replace(" stripped", "");
                }
            }

            File.WriteAllLines(ID.Value, newcontent);

            int totalProgress = 0;
            foreach (KeyValuePair<ulong, IUnityObject> obj in existingIUnityObjects)
            {
                Type type = obj.Value.GetType();
                int progressitem = 4;

                UnityEngineObjectWrapper.addedProgress.TryGetValue(type, out progressitem);
                totalProgress += progressitem;
            }

            this.existingIUnityObjects  = YamlToFrooxEngine.parseYaml(this.ID.Value);

            // Some debugging for the user to show them it worked or failed.

            UnityPackageImporter.Msg("Loaded " + existingIUnityObjects.Count.ToString() + " Unity objects/components/meshes for prefab!");

            await default(ToWorld);
            int counter = 0;
            int progress = 0;
            // Instanciate our objects to generate our prefab entirely, using the ids we assigned ealier to identify our prefab elements in our list.
            foreach (KeyValuePair<ulong,IUnityObject> obj in existingIUnityObjects)
            {
                counter++;
                Type type = obj.Value.GetType();
                int progressitem = 4;

                UnityEngineObjectWrapper.addedProgress.TryGetValue(type, out progressitem);
                progress += progressitem;
                progressIndicator?.UpdateProgress(MathX.Clamp01((float)progress / (float)totalProgress), "", "now loading " + this.existingIUnityObjects.Count.ToString() + "/" + counter.ToString() + " objects for Prefab");
                UnityPackageImporter.Msg("loading object for prefab \"" + ID.Value + "\" with an id of \"" + obj.Value.id.ToString() + "\"");
                try
                {
                    await obj.Value.InstanciateAsync(this);
                }
                catch (Exception e)
                {
                    UnityPackageImporter.Warn("Prefab IUnityObject failed to instanciate!");
                    UnityPackageImporter.Msg("Prefab IUnityObject ID: \"" + obj.Value.id.ToString() + "\"");
                    UnityPackageImporter.Warn(e.Message + e.StackTrace);
                }
                try
                {
                    debugPrefab.Append(obj.Value.ToString());
                }
                catch (Exception e)
                {
                    UnityPackageImporter.Warn("Prefab IUnityObject could not be turned into a string!");
                    UnityPackageImporter.Msg("Prefab IUnityObject ID: \"" + obj.Value.id.ToString() + "\"");
                    UnityPackageImporter.Warn(e.Message + e.StackTrace);
                }
            }

            progressIndicator?.UpdateProgress(0f, "", "instanciated " + existingIUnityObjects.Count.ToString() + " Unity objects/components/meshes for prefab! Now cleaning up.");

            List<IUnityObject> movethese = new List<IUnityObject>();

            foreach (var obj in existingIUnityObjects)
            {
                if (obj.Value.GetType() != typeof(FrooxEngineRepresentation.GameObjectTypes.Transform))
                    continue;

                var trans = obj.Value as FrooxEngineRepresentation.GameObjectTypes.Transform;
                if (trans == null)
                    continue;

                if (trans.m_FatherID != 0 || trans.parentHashedGameObj != null)
                    continue;

                try
                {
                    movethese.Add(existingIUnityObjects[trans.m_GameObjectID]);
                }
                catch
                {
                    UnityPackageImporter.Warn("transform with id \"" + trans.id.ToString() + "\" does not have a parent game object! This is bad!");
                }
            }

            UnityPackageImporter.Msg("Moving orphaned objects");
            // Getting rid of objects that should go under the prefab slot.
            foreach (var obj in movethese)
            {
                FrooxEngineRepresentation.GameObjectTypes.GameObject gameobj = obj as FrooxEngineRepresentation.GameObjectTypes.GameObject;
                await default(ToWorld);
                gameobj.frooxEngineSlot.SetParent(this.CurrentStructureRootSlot, false);
                await default(ToBackground);
            }

            UnityPackageImporter.Msg("Yaml generation done");
            UnityPackageImporter.Msg("Setting up IK Inline");
            
            // Create humanoid stuff for prefabs that are inline.
            await default(ToWorld);
            foreach (var obj in existingIUnityObjects)
            {
                if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.PrefabInstance))
                {
                    FrooxEngineRepresentation.GameObjectTypes.PrefabInstance prefab = obj.Value as FrooxEngineRepresentation.GameObjectTypes.PrefabInstance;

                    await UnityProjectImporter.SettupHumanoid(
                        prefab.importask,
                        prefab.ImportRoot.frooxEngineSlot,
                        true);
                }
            }

            
            foreach (var obj in existingIUnityObjects)
            {
                if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer))
                {
                    var newobj = (obj.Value as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer);
                    await default(ToWorld);
                    newobj.createdMeshRenderer.Enabled = newobj.m_Enabled == 1;
                    await default(ToBackground);
                }
            }

            progressIndicator?.UpdateProgress(0f, "", "setting up IK for prefab.");
            
            foreach (var obj in existingIUnityObjects)
            {
                if (obj.Value.GetType() != typeof(FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer))
                    continue;

                var newobj = (obj.Value as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer);
                if (newobj.createdMeshRenderer.Slot.Parent.Name != "RootNode")
                    continue;

                if (this.unityProjectImporter.SharedImportedFBXScenes.TryGetValue(newobj.m_Mesh.guid, out FileImportTaskScene importedfbx))
                {
                    await default(ToWorld);
                    await UnityProjectImporter.SettupHumanoid(importedfbx, this.CurrentStructureRootSlot, true);
                    await default(ToBackground);
                    break; 
                    // All skinned mesh renderers should go to the current prefab if they're under the root.
                    // I think that is the root above in the if statement with "RootNode" - @989onan
                }
                else
                {
                    UnityPackageImporter.Msg("A prefab (source fbx id: \"" + newobj.m_Mesh.guid + "\") in prefab \"" + this.ID.Value + "\" that probably points to another prefab was attempted to be imported. TODO: FIX THIS"); //TODO: FIX THIS!
                }
            }

            progressIndicator?.ProgressDone("Finished Prefab!");
            progressIndicator?.UpdateProgress(1f, "", "Finished!");

        }
        catch (Exception e)
        {
            UnityPackageImporter.Warn("Prefab hit critical import error! dumping!");
            UnityPackageImporter.Warn(e.Message + e.StackTrace);
            UnityPackageImporter.Msg(debugPrefab.ToString());
            FrooxEngineBootstrap.LogStream.Flush();
            progressIndicator?.ProgressFail("Failed to decode the Unity Prefab due to an error!");
            throw e;
        }

        await default(ToBackground);
        UnityPackageImporter.Msg("Yaml generation done");
        UnityPackageImporter.Msg("Prefab finished!");
    }
}
