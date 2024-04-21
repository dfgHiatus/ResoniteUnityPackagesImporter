using Elements.Core;
using FrooxEngine;
using Leap.Unity;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter.FrooxEngineRepresentation.GameObjectTypes;

public class MeshCollider: IUnityObject
{
    #region MeshCollider Fields
    public SourceObj m_Mesh { get; set; }
    public Dictionary<string, ulong> m_GameObject;
    public bool instanciated { get; set; }

    public ulong m_IsTrigger { get; set; }
    public ulong m_Enabled { get; set; }
    public ulong m_Convex { get; set; }
    #endregion

    public ulong m_GameObjectID { get; set; }
    public ulong id { get; set; }
    public SourceObj m_CorrespondingSourceObject { get; set; }
    public Dictionary<string, ulong> m_PrefabInstance { get; set; }

    public async Task instanciateAsync(IUnityStructureImporter importer)
    {
        if (instanciated) return;

        try
        {
            if(importer.existingIUnityObjects.TryGetValue(m_GameObject["fileID"], out IUnityObject parentobj_inc))
            {
                await default(ToWorld);
                await CreateSelf(importer);
                await default(ToBackground);
            }
            else
            {
                UnityPackageImporter.Warn("The prefab is malformed!!! A mesh collider couldn't find it's parent an id of \"" + m_GameObject["fileID"].ToString() + "\" Your collider will come out mis-shapen!");
            }
        }
        catch (Exception e)
        {
            UnityPackageImporter.Warn("an instanciating mesh collider hit an error! Stacktrace:");
            UnityPackageImporter.Warn(e.Message,e.StackTrace);
        }

        

        instanciated = true;
    }

    private async Task CreateSelf(IUnityStructureImporter importer)
    {
        m_GameObjectID = m_GameObject["fileID"];
        if (importer.existingIUnityObjects.TryGetValue(m_GameObjectID, out IUnityObject foundobject) &&
            foundobject.GetType() == typeof(GameObject))
        {

            var parentobj = foundobject as GameObject;
            await default(ToWorld);
            await parentobj.instanciateAsync(importer);
            await default(ToBackground);

            // Heh the dictionary stuff in yamls are weird
            await default(ToWorld);

            // ICollider does not define ColliderType.Value
            if (m_Convex == 1)
            {
                var convexCollider = parentobj.frooxEngineSlot.AttachComponent<FrooxEngine.ConvexHullCollider>();
                convexCollider.Type.Value = Utils.GetColliderFromULong(m_IsTrigger);
                convexCollider.Enabled = Utils.GetBoolFromULong(m_Enabled);
            }
            else
            {
                var meshCollider = parentobj.frooxEngineSlot.AttachComponent<FrooxEngine.MeshCollider>();
                meshCollider.Type.Value = Utils.GetColliderFromULong(m_IsTrigger);
                meshCollider.Enabled = Utils.GetBoolFromULong(m_Enabled);
            }

            await default(ToBackground);
        }
        else
        {
            UnityPackageImporter.Warn("The prefab is malformed!!! the mesh with an id \"" + id.ToString() + "\" did not find it's game object! ");
        }
    }



    public override string ToString()
    {
        StringBuilder result = new StringBuilder(base.ToString());

        return result.ToString();
    }
}
