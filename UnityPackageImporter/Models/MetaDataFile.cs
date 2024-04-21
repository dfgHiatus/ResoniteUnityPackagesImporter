﻿using Elements.Core;
using FrooxEngine;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityPackageImporter.FrooxEngineRepresentation;
using UnityPackageImporter.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;
using static FrooxEngine.Rig;

namespace UnityPackageImporter
{
    public class MetaDataFile
    {
        public BipedRig modelBoneHumanoidAssignments;
        public float GlobalScale = -1;

        public Dictionary<string, SourceObj> externalObjects = new Dictionary<string, SourceObj>();

        private Dictionary<BodyNode, Slot> storagebones = new Dictionary<BodyNode, Slot>();

        public Dictionary<long, string> fileIDToRecycleName = new Dictionary<long, string>();
        public MetaDataFile() {
            
            

        }
        //the second I tried to make this a yaml parser the parser broke. if anyone else can get it to work that's fine by me. - @989onan

        //this creates our biped rig under the slot we specify. in this case, it is the root.
        //this MetaDataFile object class gives us access to the biped rig component directly if desired
        public async Task GenerateComponents(Slot ModelRootSlot) {

            if(this.modelBoneHumanoidAssignments == null)
            {

                await default(ToWorld);
                this.modelBoneHumanoidAssignments = ModelRootSlot.AttachComponent<BipedRig>();
                UnityPackageImporter.Msg("Humanoid bone description being instanciated is: " + (modelBoneHumanoidAssignments != null));
                await default(ToBackground);




                //so that we add the bone without parsing it's children
                //if we use """AssignBones(Rig.BoneNode root, bool ignoreDuplicates)""" that will cause errors.
                foreach (var BoneNode in storagebones)
                {
                    if (BoneNode.Value != null)
                    {
                        UnityPackageImporter.Msg("assigning bone: " + BoneNode.Value.Name);
                        await default(ToWorld);
                        this.modelBoneHumanoidAssignments.Bones.Add(BoneNode.Key, BoneNode.Value);
                        await default(ToBackground);
                    }
                    else
                    {
                        UnityPackageImporter.Msg("assigning bone was null! Idk what it was...");
                    }
                    
                }

                await default(ToWorld);
                UnityPackageImporter.Msg("detecting forward flipped of model biped.");
                //this is here to initialize our Rig's biped forward at the end of the import for VRIK later on.
                modelBoneHumanoidAssignments.GuessForwardFlipped();
                modelBoneHumanoidAssignments.DetectHandRigs();
                await default(ToBackground);
            }
        }

        //it scans the file's metadata to make it work.
        //it also finds alternative GUID's that unity makes for fbx objects
        //but we're using it to also get our global scale.
        public async Task ScanFile(FileImportTaskScene task, Slot ModelRootSlot)
        {
            //section identification
            int sectiontype = -1;

            //clear previous before we start
            storagebones.Clear();
            fileIDToRecycleName.Clear();
            externalObjects.Clear();
            //bonereading section
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
                if(line.StartsWith("  externalObjects:")){
                    sectiontype = 2;
                    continue;
                }

                if (line.StartsWith("  humanDescription:"))
                {
                    sectiontype = 0;
                    continue;

                }
                if (line.StartsWith("    globalScale:"))
                {
                    try
                    {
                        string numberStr = line.Split(':')[1].Trim();
                        UnityPackageImporter.Msg("found scale \"" + numberStr + "\", parsing to get our scale");
                        GlobalScale = float.Parse(numberStr);
                    }
                    catch
                    {
                        GlobalScale = 1;
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
        }

        //Since Unity names and Froox Engine names are the same, just parse them as enums and return.
        public static BodyNode HumanoidNameToEnum(string boneNameHuman)
        {
            //edge cases
            boneNameHuman = boneNameHuman.Replace(" Metacarpal", "_Metacarpal");
            boneNameHuman = boneNameHuman.Replace(" Proximal", "_Proximal");
            boneNameHuman = boneNameHuman.Replace(" Distal", "_Distal");
            

            boneNameHuman = boneNameHuman.Replace(" Little Metacarpal", "Pinky_Metacarpal");
            boneNameHuman = boneNameHuman.Replace(" Little Proximal", "Pinky_Proximal");
            boneNameHuman = boneNameHuman.Replace(" Little Distal", "Pinky_Distal");
            boneNameHuman = boneNameHuman.Replace(" ", "");

            //now parse
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
}