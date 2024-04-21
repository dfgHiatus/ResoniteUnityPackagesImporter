﻿using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    [Serializable]
    public class GameObject: IUnityObject
    {

        public string m_Name;
        public int m_IsActive = 1;
        public bool instanciated { get; set; }
        public ulong id { get; set; }
        public SourceObj m_CorrespondingSourceObject { get; set; }
        public Dictionary<string, ulong> m_PrefabInstance { get; set; }

        public Slot frooxEngineSlot;

        public GameObject() {
        }


        public async Task instanciateAsync(IUnityStructureImporter importer)
        {
            if (!instanciated)
            {
                
                await default(ToWorld);
                if(m_CorrespondingSourceObject.guid == null)
                {
                    frooxEngineSlot = Engine.Current.WorldManager.FocusedWorld.AddSlot(this.m_Name);
                    frooxEngineSlot.SetParent(importer.CurrentStructureRootSlot, true); //let user managers not freak out that we're doing stuff in root.
                    frooxEngineSlot.ActiveSelf = m_IsActive == 1 ? true : false;
                }
                else
                {
                    if(importer.existingIUnityObjects.TryGetValue(m_PrefabInstance["fileID"], out IUnityObject prefab))
                    {
                        await default(ToWorld);
                        await prefab.instanciateAsync(importer);
                        await default(ToBackground);
                        if ((prefab as PrefabInstance).PrefabHashes.TryGetValue(m_CorrespondingSourceObject, out IUnityObject existingobject))
                        {
                            this.frooxEngineSlot = (existingobject as GameObject).frooxEngineSlot;
                        }




                    }

                }

                
                await default(ToBackground);

                instanciated = true;
            }

        }


        //a detailed to string for debugging.
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("id: " + id.ToString());
            result.AppendLine("instanciated: " + instanciated.ToString());
            result.AppendLine("m_IsActive: "+ m_IsActive.ToString());
            if(m_CorrespondingSourceObject != null)
            {
                result.AppendLine("m_CorrespondingSourceObject: " + m_CorrespondingSourceObject.ToString());
                
            }
            else
            {
                result.AppendLine("m_CorrespondingSourceObject: null");
            }
            if (m_PrefabInstance != null)
            {
                result.AppendLine("m_PrefabInstance: " + m_PrefabInstance.ToString());
            }
            else
            {
                result.AppendLine("m_PrefabInstance: null");
            }

            if (m_Name != null){
                result.AppendLine("m_Name: " + m_Name.ToString());
            }
            else {
                result.AppendLine("m_Name: null");
            }
            if (frooxEngineSlot != null)
            {
                result.AppendLine("frooxEngineSlot: " + frooxEngineSlot.ToString());
            }
            else
            {
                result.AppendLine("frooxEngineSlot: null");
            }



            return result.ToString();
        }
    }
}
