using FrooxEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.FrooxEngineRepresentation;
using UnityPackageImporter.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace UnityPackageImporter.Models
{
    public class UnitySceneImportTask: IUnityStructureImporter
    {
        public Dictionary<ulong, IUnityObject> existingIUnityObjects { get; set; }
        public Slot CurrentStructureRootSlot { get; set; }

        public KeyValuePair<string, string> ID { get; set; }

        public Slot allimportsroot { get; set; }
        public UnityProjectImporter unityProjectImporter { get; set; }

        public UnitySceneImportTask(Slot allimportsroot, KeyValuePair<string, string> ID)
        {
            this.ID = ID;
            
            this.allimportsroot = allimportsroot;
        }

        

        public async Task StartImport()
        {
            await default(ToWorld);

            this.CurrentStructureRootSlot = Engine.Current.WorldManager.FocusedWorld.AddSlot(Path.GetFileName(ID.Value));
            CurrentStructureRootSlot.SetParent(this.allimportsroot, false);
            await default(ToBackground);





            using var sr = File.OpenText(ID.Value);

            var deserializer = new DeserializerBuilder().WithNodeTypeResolver(new UnityNodeTypeResolver()).IgnoreUnmatchedProperties().Build();

            var parser = new Parser(sr);

            StringBuilder debugScene = new StringBuilder();
            parser.Consume<StreamStart>();
            DocumentStart variable;
            //parse loop
            //now using the power of yaml we can make this a bit more reliable and hopefully smaller.
            //reading unity prefabs as yaml allows us to much more easily obtain the data we need.
            //Unity yamls are different, but with a little trickery we can still read them with a library.
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

            UnityPackageImporter.Msg("Loaded " + existingIUnityObjects.Count.ToString() + " Unity objects/components/prefabs/lights ETC!");


            //instanciate prefabs first, since those have modifications to other already instanciated objects and would get complicated otherwise.
            await default(ToWorld);
            foreach (var obj in existingIUnityObjects)
            {
                if(obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.PrefabInstance)){
                    await obj.Value.instanciateAsync(this);
                    debugScene.Append(obj.Value.ToString());
                }
            }
            await default(ToBackground);



            //instanciate everything else.
            await default(ToWorld);
            foreach (var obj in existingIUnityObjects)
            {
                if (obj.Value.GetType() != typeof(FrooxEngineRepresentation.GameObjectTypes.PrefabInstance))
                {
                    await obj.Value.instanciateAsync(this);
                    debugScene.Append(obj.Value.ToString());
                }
            }
            await default(ToBackground);



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


            UnityPackageImporter.Debug("now debugging every object after instanciation!");
            UnityPackageImporter.Debug(debugScene.ToString());
            UnityPackageImporter.Msg("Yaml generation done");
            UnityPackageImporter.Msg("Scene finished!");
        }

    }
}
