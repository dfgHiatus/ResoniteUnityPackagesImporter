﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FrooxEngine;
using UnityPackageImporter.FrooxEngineRepresentation;
using UnityPackageImporter.Models;
using YamlDotNet.Serialization;

namespace UnityPackageImporter;

public class MetaDataFile
{
    public float GlobalScale = 1;
    private float LastScaleGlobalScale = 1;
    public Dictionary<string, SourceObj> externalObjects = new Dictionary<string, SourceObj>();
    private Dictionary<BodyNode, Slot> storagebones = new Dictionary<BodyNode, Slot>();
    public Dictionary<long, string> fileIDToRecycleName = new Dictionary<long, string>();
    public MetaDataFile() { }
            
    // The second I tried to make this a yaml parser the parser broke. if anyone else can get it to work that's fine by me. - @989onan
    // This creates our biped rig under the slot we specify. in this case, it is the root.
    // This MetaDataFile object class gives us access to the biped rig component directly if desired
    public async Task GenerateComponents(BipedRig rig) 
    {
        UnityPackageImporter.Msg("Humanoid bone description being instanciated is: " + (rig != null));
        await default(ToBackground);

        // So that we add the bone without parsing it's children
        // If we use """AssignBones(Rig.BoneNode root, bool ignoreDuplicates)""" that will cause errors.
        foreach (var BoneNode in storagebones)
        {
            if (BoneNode.Value != null)
            {
                UnityPackageImporter.Msg("Assigning bone: " + BoneNode.Value.Name);
                await default(ToWorld);
                rig.Bones.Add(BoneNode.Key, BoneNode.Value);
                await default(ToBackground);
            }
            else
            {
                UnityPackageImporter.Msg("Assigning bone was null! Idk what it was...");
            }
        }
    }

    // It scans the file's metadata to make it work.
    // It also finds alternative GUID's that unity makes for fbx objects
    // But we're using it to also get our global scale.
    public async Task ScanFile(FileImportTaskScene task, Slot ModelRootSlot)
    {
        // Section identification
        int sectiontype = -1;

        // Clear previous before we start
        storagebones.Clear();
        fileIDToRecycleName.Clear();
        externalObjects.Clear();

        // Bonereading section
        string boneName = string.Empty;
        string boneNameHuman = string.Empty;
        string externalObjects_name = string.Empty;

        foreach (string line in File.ReadLines(task.file + UnityPackageImporter.UNITY_META_EXTENSION))
        {
            if(line.StartsWith("  fileIDToRecycleName:"))
            {
                sectiontype = 1;
                continue;
            }
            if(line.StartsWith("  internalIDToNameTable:"))
            {
                sectiontype = -1;
                continue;
            }
            if(line.StartsWith("  externalObjects:"))
            {
                sectiontype = 2;
                continue;
            }

            if (line.StartsWith("  humanDescription:"))
            {
                sectiontype = 0;
                continue;
            }
            if (line.StartsWith("    - name:"))
            {
                UnityPackageImporter.Msg("Name of scale is: \""+line.Split(':')[1].Trim()+"\"");
            }
            if (line.StartsWith("      scale:"))
            {
                UnityPackageImporter.Msg("scaleblock3.5");
                try
                {
                    string numberStr = line.Split(':')[2].Split(',')[0].Trim();
                    UnityPackageImporter.Msg("found scale last \"" + numberStr + "\", parsing to get our scale");
                    LastScaleGlobalScale = Math.Abs(float.Parse(numberStr));
                }
                catch
                {
                    UnityPackageImporter.Msg("scaleblock fail");
                    UnityPackageImporter.Msg("scaleblock fail");
                }
                continue;
            }

            switch (sectiontype)
            {
                case 0:
                    if(line.StartsWith("    - boneName:"))
                    {
                        boneName = line.Split(':')[1].Trim();
                    }
                    else if(line.StartsWith("      humanName:"))
                    {
                        boneNameHuman = line.Split(':')[1].Trim();
                    }

                    if (boneName.Length != 0 && boneNameHuman.Length != 0)
                    {
                        await default(ToWorld);
                        UnityPackageImporter.Msg("finding bone: "+ boneName);
                        Rig.BoneNode bone = new Rig.BoneNode(ModelRootSlot.FindChild(boneName, false, false, -1), HumanoidNameToEnum(boneNameHuman));
                        
                        //so that we add the bone without parsing it's children
                        //if we use """AssignBones(Rig.BoneNode root, bool ignoreDuplicates)""" that will cause errors.
                        if (!storagebones.ContainsKey(bone.boneType))
                        {
                            storagebones.Add(bone.boneType, bone.bone);
                        }
                        await default(ToBackground);
                        boneName = string.Empty;
                        boneNameHuman = string.Empty;            
                    }
                    break;
                case 1:
                    long.TryParse(line.Split(':')[0].Trim(), out long num);
                    string name = line.Split(':')[1].Trim();
                    fileIDToRecycleName.Add(num, name);
                    break;
                case 2:
                    if(line.StartsWith("  - first:"))
                    {
                        externalObjects_name = string.Empty;
                    }
                    if(line.StartsWith("      name:"))
                    {
                        externalObjects_name = line.Split(':')[1].Trim();
                    }
                    if (line.StartsWith("    second:"))
                    {
                        string unparsed = '{'+line.Split('{')[1].Trim();
                        var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

                        SourceObj second = deserializer.Deserialize<SourceObj>(unparsed); //this makes it easier to get our source object

                        if(externalObjects_name != string.Empty)
                        {
                            externalObjects.Add(externalObjects_name, second);
                        }   
                    }
                    break;
            }
        }
        GlobalScale = LastScaleGlobalScale;
    }

    // Since Unity names and FrooxEngine names are the same, just parse them as enums and return.
    public static BodyNode HumanoidNameToEnum(string boneNameHuman)
    {
        // Edge cases
        boneNameHuman = boneNameHuman.Replace(" Metacarpal", "_Metacarpal");
        boneNameHuman = boneNameHuman.Replace(" Proximal", "_Proximal");
        boneNameHuman = boneNameHuman.Replace(" Distal", "_Distal");
        boneNameHuman = boneNameHuman.Replace(" Little Metacarpal", "Pinky_Metacarpal");
        boneNameHuman = boneNameHuman.Replace(" Little Proximal", "Pinky_Proximal");
        boneNameHuman = boneNameHuman.Replace(" Little Distal", "Pinky_Distal");
        boneNameHuman = boneNameHuman.Replace(" ", "");

        // Now parse
        if (Enum.TryParse(boneNameHuman, out BodyNode bodyNode))
        {
            return bodyNode;
        }
        else
        {
            return BodyNode.NONE;
        }
    }
}