using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes
{
    public class RotationConstraint: IUnityObject
    {
        public bool instanciated { get; set; }

        public ulong id { get; set; }
        public SourceObj m_CorrespondingSourceObject { get; set; }
        public Dictionary<string, ulong> m_PrefabInstance { get; set; }
        public Dictionary<string, ulong> m_GameObject { get; set; }
        public TransformFloat3 m_RotationAtRest { get; set; }
        public TransformFloat4 m_RotationOffset { get; set; }

        public int m_AffectRotationX;
        public int m_AffectRotationY;
        public int m_AffectRotationZ;

        public int m_IsContraintActive;
        public int m_IsLocked;


        public List<TransformSource> m_Sources;

        public async Task instanciateAsync(IUnityStructureImporter importer)
        {
            if(instanciated) return;
            


            if (importer.existingIUnityObjects.TryGetValue(m_GameObject["fileID"], out IUnityObject slotunity))
            {
                await default(ToWorld);
                await slotunity.instanciateAsync(importer);
                await default(ToBackground);
                Slot componenttarget = (slotunity as GameObject).frooxEngineSlot;

                foreach(TransformSource source in m_Sources)
                {
                    if(importer.existingIUnityObjects.TryGetValue(source.sourceTransform["fileID"], out IUnityObject targettransform))
                    {
                        await default(ToWorld);
                        await targettransform.instanciateAsync(importer);
                        await default(ToBackground);
                        try
                        {
                            ulong gameobjid = (targettransform as Transform).m_GameObjectID;
                            if (importer.existingIUnityObjects.TryGetValue(gameobjid, out IUnityObject targetgameobj))
                            {
                                await default(ToWorld);
                                await targetgameobj.instanciateAsync(importer);
                                await default(ToBackground);

                                Slot rotationsource = (targetgameobj as GameObject).frooxEngineSlot;
                                //source.weight; //use this!!!!!!!!


                                //TODO do something here. - @989onan 
                            }
                        }
                        catch (Exception e)
                        {
                            UnityPackageImporter.Msg("Rotation constraint failed to create itself! error:");
                            UnityPackageImporter.Msg(e.Message+e.StackTrace);
                        }
                        
                    }
                    
                }
            }

            instanciated = true;
        }
    }


    public class TransformSource
    {
        public Dictionary<string, ulong> sourceTransform;
        public float weight;
    }
}
