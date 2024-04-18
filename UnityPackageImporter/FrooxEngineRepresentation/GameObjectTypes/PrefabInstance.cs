using Elements.Core;
using FrooxEngine;
using Leap.Unity;
using Leap.Unity.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityPackageImporter.Models;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Serialization;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    public class PrefabInstance : IUnityObject
    {
        public bool instanciated { get; set; }
        public ulong id { get; set; }

        public SourceObj m_CorrespondingSourceObject { get; set; }

        public GameObject ImportRoot { get; set; }
        public ModificationRoot m_Modification { get; set; }

        public Dictionary<string, ulong> m_PrefabInstance { get; set; }

        public Dictionary<SourceObj, IUnityObject> PrefabHashes = new Dictionary<SourceObj, IUnityObject>(new SourceObjCompare());

        //This will hold up the importing process but who cares we're only importing each fbx once
        public async Task instanciateAsync(IUnityStructureImporter importer)
        {
            if (!instanciated)
            {
                UnityPackageImporter.Msg("is m_CorrespondingSourceObject instanciated?: " + (m_CorrespondingSourceObject == null));
                UnityPackageImporter.Msg("is m_Modification instanciated?: " + (m_Modification == null));
                if (importer.unityProjectImporter.AssetIDDict.ContainsKey(m_CorrespondingSourceObject.guid))
                {
                    //find FBX's in our scene that need importing, so we can import them and then attach our prefab objects to it.
                    //We are sharing the FBX's, so if it's a project with X prefabs of the same model but with different changes can all use the same base model
                    //next we associate each shared FBX to prefabs that use them. then we duplicate the shared FBX's to each scene including this one, and then those duplicated FBXs become the prefabs themselves
                    //instanciate our objects to generate our prefab entirely, using the ids we assigned ealier to identify our prefab elements in our list.
                    Slot targetParent;
                    if(importer.existingIUnityObjects.TryGetValue(m_Modification.m_TransformParent["fileID"], out IUnityObject foundobjectparent))
                    {
                        await foundobjectparent.instanciateAsync(importer);
                        targetParent = (foundobjectparent as GameObject).frooxEngineSlot;
                    }
                    else
                    {
                        targetParent = importer.CurrentStructureRootSlot;
                    }
                    UnityPackageImporter.Msg("is targetParent instanciated?: " + (targetParent == null));
                    await default(ToWorld);
                    ImportRoot.frooxEngineSlot = importer.unityProjectImporter.SharedImportedFBXScenes[m_CorrespondingSourceObject.guid].FinishedFileSlot.Duplicate();
                    ImportRoot.frooxEngineSlot.SetParent(targetParent);
                    await default(ToBackground);

                    this.PrefabHashes = importer.unityProjectImporter.SharedImportedFBXScenes[m_CorrespondingSourceObject.guid].FILEID_To_Slot_Pairs;

                    foreach(Modification mod in m_Modification.m_Modifications)
                    {
                        UnityPackageImporter.Msg("is mod instanciated?: " + (mod == null));
                        if (this.PrefabHashes.TryGetValue(mod.target, out IUnityObject targetobj)){

                            
                            Type targettype = targetobj.GetType();
                            try
                            {
                                //This is so bad, since it does a lot of reflection, but there's no better way of doing this since the hash of the object we're targeting doesn't actually represent the class of the type we made from it. - @989onan
                                if (targettype.GetProperty(mod.propertyPath).GetValue(targetobj).GetType() == typeof(int))
                                {
                                    int.TryParse(mod.value, out int value);
                                    targettype.GetProperty(mod.propertyPath).SetValue(Convert.ChangeType(targetobj, targettype), value);
                                }
                                else if (targettype.GetProperty(mod.propertyPath).GetValue(targetobj).GetType() == typeof(float))
                                {
                                    float.TryParse(mod.value, out float value);
                                    targettype.GetProperty(mod.propertyPath).SetValue(Convert.ChangeType(targetobj, targettype), value);
                                }
                                else if (targettype.GetProperty(mod.propertyPath).GetValue(targetobj).GetType() == typeof(bool))
                                {
                                    bool.TryParse(mod.value, out bool value);
                                    targettype.GetProperty(mod.propertyPath).SetValue(Convert.ChangeType(targetobj, targettype), value);
                                }
                                else if (targettype.GetProperty(mod.propertyPath).GetValue(targetobj).GetType() == typeof(string))
                                {
                                    targettype.GetProperty(mod.propertyPath).SetValue(Convert.ChangeType(targetobj, targettype), mod.value);
                                }
                                else
                                {
                                    UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the current scene is malformed!!! The modification with the hash \"" + mod.target.fileID + "\" has a value of \"" + mod.value + "\", which doesn't cast to any: " +
                                        "int, float, string" +
                                        ".");

                                }
                                
                            }
                            catch
                            {
                                UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the current scene is malformed!!! The modification with the hash \"" + mod.target.fileID + "\" does not exist! ");
                            }


                        }
                        else
                        {
                            UnityPackageImporter.Warn("The prefab with a file id of \"" + id.ToString() + "\" in the current scene is malformed!!! The modification with the hash \"" + mod.target.fileID + "\" does not match any in the list of hashes on the prefab \""+ id .ToString()+ "\"");
                        }
                        
                    }


                }
                else
                {
                    UnityPackageImporter.Warn("The prefab with a file id of \""+ id.ToString()+ "\" in the current scene is malformed!!! The file source of the prefab doesn't exist in the imported package or file list set.");
                }
            }
        }


        //a detailed to string for debugging.
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("m_Modification: " + m_Modification.ToString());
            result.AppendLine("m_PrefabInstance: " + m_PrefabInstance.ToString());


            return result.ToString();
        }

        




    }
    public class Modification
    {
        public SourceObj target;
        public string propertyPath;
        public string value;
        public Dictionary<string, string> objectReference;

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("target: " + target.ToString());
            result.AppendLine("propertyPath: " + propertyPath.ToString());
            result.AppendLine("value: " + value.ToString());
            result.AppendLine("objectReference: " + objectReference.ToString()); //same here - @989onan

            return result.ToString();
        }
    }

    public class ModificationRoot
    {
        public Dictionary<string, ulong> m_TransformParent;
        public List<Modification> m_Modifications;
        public List<string> m_RemovedComponents;
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("m_TransformParent: " + m_TransformParent.ToString());
            result.AppendLine("m_Modifications: " + m_Modifications.ToString());
            return result.ToString();
        }
    }
}
