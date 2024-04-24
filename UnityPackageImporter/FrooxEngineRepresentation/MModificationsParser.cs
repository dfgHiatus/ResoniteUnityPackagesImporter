using Elements.Core;
using HarmonyLib;
using Leap.Unity.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection; 
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.Models;
using static HarmonyLib.AccessTools;

namespace UnityPackageImporter.FrooxEngineRepresentation
{
    public class MModificationsParser
    {




        public static bool ParseModifcation(IUnityObject targetobj, ModsPrefab mod)
        {
            Type targettype = targetobj.GetType();

            List<string> path = mod.propertyPath.Split('.').ToList();

            foreach(string path2 in path) {
                UnityPackageImporter.Msg("path item: " + path2);
            }

            //use reflection to get the first part of the path.
            FieldInfo field = targettype.GetField(path.TakeFirst());
            return recursiveSetTypeValue(targetobj, path, mod, field);
        }

        //m_rotation.w
        private static bool recursiveSetTypeValue(object targetobj, List<string> path, ModsPrefab mod, FieldInfo field)
        {
            if (field == null) {
                UnityPackageImporter.Error("Modification field \"" + mod.ToString() + "\" Field ended up at null path when parsing!");
                if (path.Count > 0)
                {
                    UnityPackageImporter.Msg("path item: " + path[0]);
                }
                UnityPackageImporter.Error("targetobj: "+(targetobj != null ? targetobj.ToString() : "null"));

                return false; 
                
            }

            if (path.Count > 0)
            {
                string current = path.TakeFirst();
                if (current.Equals("Array"))
                {
                    return recursiveSetTypeValue(targetobj, path, mod, field);
                }
                else if (current.StartsWith("data["))
                {
                    try
                    {
                        UnityPackageImporter.Msg("array type is: " + field.FieldType);
                        UnityPackageImporter.Msg("array enclosed type is: " + field.FieldType.GetGenericArguments()[0]);
                        UnityPackageImporter.Msg("targetobj type is: " + targetobj.GetType());
                        int arrayindex = int.Parse(current.Replace("data[", "").Replace("]", ""));

                        Type arraytype = field.FieldType.GetGenericArguments()[0];
                        Array array = field.GetValue(targetobj) as Array;
                        
                        array.SetValue(Convert.ChangeType(mod.value != null ? mod.value : mod.target, arraytype), arrayindex);
                        return true;
                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Error("Assigning final part of path for modification on an array hit an error!");
                        UnityPackageImporter.Error(e.Message, e.StackTrace);
                        return false;
                    }
                }
                else
                {
                    FieldInfo fieldnew;
                    try
                    {
                        
                        targetobj = field.GetValue(targetobj);
                        UnityPackageImporter.Msg(targetobj.GetType().ToString());
                        fieldnew = targetobj.GetType().GetField(current);
                        UnityPackageImporter.Msg(fieldnew.FieldType.ToString());
                    }
                    catch (Exception e)
                    {
                        UnityPackageImporter.Error("Finding next part of path for modification hit an error!");
                        UnityPackageImporter.Error(e.Message, e.StackTrace);
                        return false;
                    }

                    return recursiveSetTypeValue(targetobj, path, mod, fieldnew);
                }
            }
            else
            {
                try
                {
                    field.SetValue(targetobj, Convert.ChangeType(mod.value != null ? mod.value : mod.target, field.FieldType));
                    return true;
                }
                catch (Exception e)
                {
                    UnityPackageImporter.Error("Assigning final part of path for modification hit an error!");
                    UnityPackageImporter.Error(e.Message, e.StackTrace);
                    UnityPackageImporter.Msg(targetobj.GetType().ToString());
                    UnityPackageImporter.Msg(field.FieldType.ToString());
                    return false;
                }
                
            }
        }


    }
}
