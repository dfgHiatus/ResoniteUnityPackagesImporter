using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elements.Core;

namespace UnityPackageImporter.FrooxEngineRepresentation;

public class MModificationsParser
{
    public static bool ParseModifcation(IUnityObject targetobj, ModsPrefab mod)
    {
        var targetType = targetobj.GetType();
        var path = mod.propertyPath.Split('.').ToList();

        foreach(string path2 in path) 
        {
            UnityPackageImporter.Msg("path item: " + path2);
        }

        // Use reflection to get the first part of the path.
        FieldInfo field = targetType.GetField(path.TakeFirst());
        return RecursiveSetTypeValue(targetobj, path, mod, field);
    }


    // m_rotation.w
    private static bool RecursiveSetTypeValue(object targetobj, List<string> path, ModsPrefab mod, FieldInfo field)
    {
        if (field == null) 
        {
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
                return RecursiveSetTypeValue(targetobj, path, mod, field);
            }
            else if (current.StartsWith("data["))
            {
                try
                {
                    UnityPackageImporter.Msg("array type is: " + field.FieldType);
                    UnityPackageImporter.Msg("array enclosed type is: " + field.FieldType.GetGenericArguments()[0]);
                    UnityPackageImporter.Msg("targetobj type is: " + targetobj.GetType());
                    int arrayindex = int.Parse(current.Replace("data[", "").Replace("]", ""));
                    UnityPackageImporter.Msg("arrayindex is: " + arrayindex.ToString());
                    Type arraytype = field.FieldType.GetGenericArguments()[0];
                    IList array = field.GetValue(targetobj) as IList; //this should work in most cases.
                    try
                    {
                        UnityPackageImporter.Msg("array: " + array.ToString());
                    }
                    catch
                    {
                        UnityPackageImporter.Msg("array: null");
                    }

                    //in case the array has nothing inside of it
                    try
                    {
                        array.RemoveAt(arrayindex);
                        array.Insert(arrayindex, (mod.value != null) ? Convert.ChangeType(mod.value, arraytype) : mod.objectReference);
                    }
                    catch
                    {
                        array.Add((mod.value != null) ? Convert.ChangeType(mod.value, arraytype) : mod.objectReference);
                    }
                    
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

                return RecursiveSetTypeValue(targetobj, path, mod, fieldnew);
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
