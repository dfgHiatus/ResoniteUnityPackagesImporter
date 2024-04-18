﻿using FrooxEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.FrooxEngineRepresentation;
using YamlDotNet.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace UnityPackageImporter.Models
{
    internal class UnityPrefabImportTask : IUnityStructureImporter
    {

        public List<Slot> oldSlots = new List<Slot>();
        public Dictionary<ulong, IUnityObject> existingIUnityObjects { get; set; }
        public Slot CurrentStructureRootSlot { get; set; }
        public Slot allimportsroot { get; set; }
        public KeyValuePair<string, string> ID { get; set; }


        public UnityProjectImporter unityProjectImporter { get; set; }

        public UnityPrefabImportTask(Slot root, KeyValuePair<string, string> ID)
        {
            this.ID = ID;
            this.allimportsroot = root;
        }
        public async Task StartImport()
        {




            await default(ToWorld);
            this.CurrentStructureRootSlot = Engine.Current.WorldManager.FocusedWorld.AddSlot(Path.GetFileName(ID.Value));
            this.CurrentStructureRootSlot.SetParent(this.allimportsroot, false);
            await default(ToBackground);
            //begin the parsing of our prefabs.
            //begin the parsing of our prefabs.

            //parse loop
            //now using the power of yaml we can make this a bit more reliable and hopefully smaller.
            //reading unity prefabs as yaml allows us to much more easily obtain the data we need.
            //Unity yamls are different, but with a little trickery we can still read them with a library.



            using var sr = File.OpenText(ID.Value);

            var deserializer = new DeserializerBuilder().WithNodeTypeResolver(new UnityNodeTypeResolver()).IgnoreUnmatchedProperties().Build();

            var parser = new Parser(sr);

            StringBuilder debugPrefab = new StringBuilder();
            parser.Consume<StreamStart>();
            DocumentStart variable;
            while (parser.Accept<DocumentStart>(out variable) == true)
            {
                // Deserialize the document
                try
                {
                    UnityEngineObjectWrapper docWrapped = deserializer.Deserialize<UnityEngineObjectWrapper>(parser);
                    IUnityObject doc = docWrapped.Result();
                    doc.id = UnityNodeTypeResolver.anchor; //this works because they're separate documents and we're deserializing them one by one. Not nessarily in order, we're just gathering them.
                    //since deserializing happens before adding to the list and those are done syncronously with each other, it is fine.
                    existingIUnityObjects.Add(doc.id, doc);
                }
                catch (Exception e)
                {
                    UnityPackageImporter.Msg("Couldn't evaluate node type. stacktrace below");
                    UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    try
                    {
                        IUnityObject doc = new FrooxEngineRepresentation.GameObjectTypes.NullType();
                        doc.id = UnityNodeTypeResolver.anchor;
                        existingIUnityObjects.Add(doc.id, doc);
                    }
                    catch (ArgumentException e2)
                    {/*idc.*/
                        UnityPackageImporter.Msg("Duplicate key probably. just ignore this.");
                        UnityPackageImporter.Warn(e2.Message + e2.StackTrace);
                    }

                }



            }

            //some debugging for the user to show them it worked or failed.

            UnityPackageImporter.Msg("Loaded " + existingIUnityObjects.Count.ToString() + " Unity objects/components/meshes for scene!");

            //instanciate inline prefabs first, since those have modifications to other already instanciated objects and would get complicated otherwise.
            await default(ToWorld);
            foreach (var obj in existingIUnityObjects)
            {
                if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.PrefabInstance))
                {
                    await obj.Value.instanciateAsync(this);
                    debugPrefab.Append(obj.Value.ToString());
                }
            }
            await default(ToBackground);

            //instanciate our objects to generate our prefab entirely, using the ids we assigned ealier to identify our prefab elements in our list.
            await default(ToWorld);
            foreach (var obj in existingIUnityObjects)
            {
                await obj.Value.instanciateAsync(this);
                debugPrefab.Append(obj.Value.ToString());
            }
            await default(ToBackground);

            List<IUnityObject> destroythese = new List<IUnityObject>();

            foreach (var obj in existingIUnityObjects)
            {
                if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.Transform))
                {
                    FrooxEngineRepresentation.GameObjectTypes.Transform trans = obj.Value as FrooxEngineRepresentation.GameObjectTypes.Transform;
                    if (trans != null)
                    {
                        if (trans.m_FatherID == 0)
                        {
                            destroythese.Add(trans);
                            destroythese.Add(existingIUnityObjects[trans.m_GameObjectID]);
                        }
                    }
                }
            }

            //getting rid of objects that should go under the prefab slot.
            foreach (var obj in destroythese)
            {
                obj.instanciated = false;
                if (obj.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.GameObject))
                {
                    FrooxEngineRepresentation.GameObjectTypes.GameObject gameobj = obj as FrooxEngineRepresentation.GameObjectTypes.GameObject;
                    await default(ToWorld);
                    foreach (Slot prefabImmediateChild in gameobj.frooxEngineSlot.Children.ToArray())
                    {
                        prefabImmediateChild.SetParent(this.CurrentStructureRootSlot, false);
                    }
                    gameobj.frooxEngineSlot.Destroy();
                    await default(ToBackground);
                }
                existingIUnityObjects.Remove(obj.id);
            }



            UnityPackageImporter.Debug("now debugging every object after instanciation!");
            UnityPackageImporter.Debug(debugPrefab.ToString());



            UnityPackageImporter.Msg("Yaml generation done");
            UnityPackageImporter.Msg("Importing models");

            //create humanoid stuff for prefabs that are inline.
            await default(ToWorld);
            foreach (var obj in existingIUnityObjects)
            {
                if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.PrefabInstance))
                {
                    FrooxEngineRepresentation.GameObjectTypes.PrefabInstance prefab = obj.Value as FrooxEngineRepresentation.GameObjectTypes.PrefabInstance;

                    await UnityProjectImporter.SettupHumanoid(
                        unityProjectImporter.SharedImportedFBXScenes[prefab.m_CorrespondingSourceObject.guid].file,
                        prefab.ImportRoot.frooxEngineSlot);
                }
            }

            await default(ToBackground);
            foreach (var obj in existingIUnityObjects)
            {
                if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer))
                {
                    FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer newobj = (obj.Value as FrooxEngineRepresentation.GameObjectTypes.SkinnedMeshRenderer);
                    if (newobj.parentobj.frooxEngineSlot.Parent.Name == "RootNode")
                    {
                        if(this.unityProjectImporter.SharedImportedFBXScenes.TryGetValue(newobj.m_Mesh.guid, out FileImportTaskScene importedfbx)){
                            await UnityProjectImporter.SettupHumanoid(importedfbx.file, this.CurrentStructureRootSlot);
                            break; //all skinned mesh renderers should go to the current prefab if they're under the root. I think that is the root above in the if statement with "RootNode" - @989onan
                        }
                        else
                        {
                            UnityPackageImporter.Warn("This is not good, the prefab with the name \""+ "\" cannot find it's source file for a skinned mesh renderer! Stacktrace:");
                            UnityPackageImporter.Warn("\""+newobj.parentobj.frooxEngineSlot.Parent.Name+"\" == \"RootNode = \""+ (newobj.parentobj.frooxEngineSlot.Parent.Name == "RootNode") + "\"");
                            UnityPackageImporter.Warn("Skinned Mesh Renderer: " + newobj.ToString());
                        }
                        
                    }
                    
                }
            }
            
            await default(ToBackground);
            UnityPackageImporter.Msg("Prefab finished!");
        }
    }
}