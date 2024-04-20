using FrooxEngine;
using System;
using System.CodeDom;
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
using YamlDotNet.Serialization.NamingConventions;

namespace UnityPackageImporter.Models
{
    public class UnitySceneImportTask: IUnityStructureImporter
    {
        public Dictionary<ulong, IUnityObject> existingIUnityObjects { get; set; }
        public Slot CurrentStructureRootSlot { get; set; }

        public KeyValuePair<string, string> ID { get; set; }

        public Slot allimportsroot { get; set; }
        public UnityProjectImporter unityProjectImporter { get; set; }

        public UnitySceneImportTask(Slot allimportsroot, KeyValuePair<string, string> ID, UnityProjectImporter unityProjectImporter)
        {
            this.ID = ID;
            this.unityProjectImporter = unityProjectImporter;
            this.allimportsroot = allimportsroot;
        }

        

        public async Task StartImport()
        {
            StringBuilder debugScene = new StringBuilder();
            try
            {
                existingIUnityObjects = new Dictionary<ulong, IUnityObject>();
                await default(ToWorld);

                this.CurrentStructureRootSlot = Engine.Current.WorldManager.FocusedWorld.AddSlot(Path.GetFileName(ID.Value));
                UnityPackageImporter.Msg("Current slot for scene import is: ");
                UnityPackageImporter.Msg(CurrentStructureRootSlot.ToString());
                this.CurrentStructureRootSlot.SetParent(this.allimportsroot, false);
                await default(ToBackground);



                //we first have to remove "stripped" since those cause yaml parsing errors
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




                using var sr = File.OpenText(ID.Value);

                UnityNodeTypeResolver noderesolver = new UnityNodeTypeResolver();

                var deserializer = new DeserializerBuilder().WithNodeTypeResolver(noderesolver)
                    .IgnoreUnmatchedProperties()
                    .WithNamingConvention(NullNamingConvention.Instance)//outta here with that crappy conversion!!!! We got unity crap we deal with unity crap. - @989onan
                    .Build();

                var parser = new Parser(sr);

                
                parser.Consume<StreamStart>();
                DocumentStart variable;
                //begin the parsing of our scenes.

                //parse loop
                //now using the power of yaml we can make this a bit more reliable and hopefully smaller.
                //reading unity scenes as yaml allows us to much more easily obtain the data we need.
                //Unity yamls are different, but with a little trickery we can still read them with a library.
                while (parser.Accept<DocumentStart>(out variable) == true)
                {
                    // Deserialize the document
                    try
                    {
                        try
                        {
                            UnityEngineObjectWrapper docWrapped = deserializer.Deserialize<UnityEngineObjectWrapper>(parser);
                            IUnityObject doc = docWrapped.Result();
                            doc.id = noderesolver.anchor; //this works because they're separate documents and we're deserializing them one by one. Not nessarily in order, we're just gathering them.
                                                          //since deserializing happens before adding to the list and those are done syncronously with each other, it is fine.

                            existingIUnityObjects.Add(doc.id, doc);
                        }
                        catch
                        {
                            try
                            {
                                IUnityObject doc = new FrooxEngineRepresentation.GameObjectTypes.NullType();
                                doc.id = noderesolver.anchor;
                                existingIUnityObjects.Add(doc.id, doc);
                            }
                            catch (ArgumentException e2)
                            {/*idc.*/
                                UnityPackageImporter.Msg("Duplicate key probably for Scene \"" + ID.Value + "\" just ignore this.");
                                UnityPackageImporter.Warn(e2.Message + e2.StackTrace);
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Msg("Couldn't evaluate node type for the Scene\"" + ID.Value + "\". stacktrace below");
                        UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    }



                }

                //some debugging for the user to show them it worked or failed.

                UnityPackageImporter.Msg("Loaded " + existingIUnityObjects.Count.ToString() + " Unity objects/components/prefabs/lights ETC!");


                //instanciate prefabs first, since those have modifications to other already instanciated objects and would get complicated otherwise.
                await default(ToWorld);
                foreach (var obj in existingIUnityObjects)
                {
                    if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.PrefabInstance))
                    {
                        try
                        {
                            await obj.Value.instanciateAsync(this);
                            
                        }
                        catch (Exception e)
                        {
                            UnityPackageImporter.Warn("Scene Prefab failed to instanciate!");
                            UnityPackageImporter.Msg("Scene Prefab ID: \"" + obj.Value.id.ToString() + "\"");
                            UnityPackageImporter.Warn(e.Message + e.StackTrace);
                        }
                        try
                        {
                            debugScene.Append(obj.Value.ToString());
                        }
                        catch (Exception e)
                        {
                            UnityPackageImporter.Warn("Scene prefab could not be turned into a string!");
                            UnityPackageImporter.Msg("Scene prefab ID: \"" + obj.Value.id.ToString() + "\"");
                            UnityPackageImporter.Warn(e.Message + e.StackTrace);
                        }
                    }

                }
                await default(ToBackground);

                UnityPackageImporter.Msg("Loaded all prefabs for scene \"" + ID.Value + "\"");

                //instanciate everything else.
                await default(ToWorld);
                foreach (var obj in existingIUnityObjects)
                {
                    try
                    {
                        try
                        {
                            debugScene.Append(obj.Value.ToString());
                        }
                        catch (Exception e)
                        {
                            UnityPackageImporter.Warn("Scene object could not be turned into a string!");
                            UnityPackageImporter.Msg("Scene object ID: \"" + obj.Value.id.ToString() + "\"");
                            UnityPackageImporter.Warn(e.Message + e.StackTrace);
                        }
                        UnityPackageImporter.Msg("loading object for scene \"" + ID.Value + "\" with an id of \"" + obj.Value.id.ToString() + "\"");
                        if (obj.Value.GetType() != typeof(FrooxEngineRepresentation.GameObjectTypes.PrefabInstance))
                        {
                            await obj.Value.instanciateAsync(this);
                        }
                        

                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Warn("Scene object failed to instanciate!");
                        UnityPackageImporter.Msg("Scene object ID: \"" + obj.Value.id.ToString() + "\"");
                        UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    }
                    
                }
                await default(ToBackground);



                await default(ToBackground);

                List<IUnityObject> movethese = new List<IUnityObject>();

                foreach (var obj in existingIUnityObjects)
                {
                    if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.Transform))
                    {
                        FrooxEngineRepresentation.GameObjectTypes.Transform trans = obj.Value as FrooxEngineRepresentation.GameObjectTypes.Transform;
                        if (trans != null)
                        {
                            if (trans.m_FatherID == 0 && trans.parentHashedGameObj == null)
                            {
                                try
                                {
                                    movethese.Add(existingIUnityObjects[trans.m_GameObjectID]);
                                }
                                catch
                                {
                                    UnityPackageImporter.Warn("transform with id \""+trans.id.ToString()+"\" does not have a parent game object! This is bad!" );
                                }
                            }
                        }
                    }
                }

                UnityPackageImporter.Msg("Moving orphaned objects");
                //getting rid of objects that should go under the prefab slot.
                foreach (var obj in movethese)
                {
                    FrooxEngineRepresentation.GameObjectTypes.GameObject gameobj = obj as FrooxEngineRepresentation.GameObjectTypes.GameObject;
                    await default(ToWorld);
                    gameobj.frooxEngineSlot.SetParent(this.CurrentStructureRootSlot, false);
                    await default(ToBackground);
                }

                UnityPackageImporter.Msg("Setting up IK for Prefabs in scene \""+ID.Value+"\"");
                await default(ToWorld);
                foreach (var obj in existingIUnityObjects)
                {
                    if (obj.Value.GetType() == typeof(FrooxEngineRepresentation.GameObjectTypes.PrefabInstance))
                    {
                        FrooxEngineRepresentation.GameObjectTypes.PrefabInstance prefab = obj.Value as FrooxEngineRepresentation.GameObjectTypes.PrefabInstance;

                        await UnityProjectImporter.SettupHumanoid(
                            unityProjectImporter.SharedImportedFBXScenes[prefab.m_SourcePrefab.guid],
                            prefab.ImportRoot.frooxEngineSlot);
                    }
                }
                await default(ToBackground);
            }
            catch (Exception e)
            {
                UnityPackageImporter.Warn("Scene \"" + ID.Value + "\" hit critical import error! dumping!");
                UnityPackageImporter.Warn(e.Message + e.StackTrace);
                
            }
            


            UnityPackageImporter.Msg("now debugging every object after instanciation!");
            UnityPackageImporter.Msg("Yaml generation done");
            UnityPackageImporter.Msg(debugScene.ToString());
            UnityPackageImporter.Msg("Scene finished!");
        }

    }
}
