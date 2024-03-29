using Elements.Core;
using FrooxEngine;
using Microsoft.Cci;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UnityPackageImporter
{
    internal class MetaDataFile
    {
        public BipedRig modelBoneHumanoidAssignments;
        public float GlobalScale = -1;
        public MetaDataFile() { }

        public static async Task<MetaDataFile> ScanFile(string path, Slot ModelRootSlot)
        {
            //section identification
            int sectiontype = -1;

            //bonereading section
            string boneName = string.Empty;
            string boneNameHuman = string.Empty;


            bool inScaleBlock = false;
            MetaDataFile metaDataFile = new MetaDataFile();
            
            await default(ToWorld);
            metaDataFile.modelBoneHumanoidAssignments = ModelRootSlot.AttachComponent<BipedRig>();
            await default(ToBackground);
            foreach (string line in File.ReadLines(path))
            {
                
                if (line.StartsWith("  humanDescription:"))
                {
                    sectiontype = 0;
                    continue;

                }
                else if (line.StartsWith("    - name: Armature"))
                {
                    inScaleBlock = true;

                }
                if (inScaleBlock)
                {
                    if (line.StartsWith("      scale: "))
                    {
                        string numberStr = line.Split(':')[2].Split(',')[0].Trim();
                        UnityPackageImporter.Msg("found scale \"" + numberStr + "\", parsing and diving 1 by it to get our scale");
                        metaDataFile.GlobalScale = 1/float.Parse(numberStr);
                        inScaleBlock = false;
                    }
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
                            Rig.BoneNode bone = new Rig.BoneNode(ModelRootSlot.FindChild(boneName,false,false,-1), HumanoidNameToEnum(boneNameHuman));
                            
                            //so that we add the bone without parsing it's children
                            //if we use """AssignBones(Rig.BoneNode root, bool ignoreDuplicates)""" that will cause errors.
                            if (!metaDataFile.modelBoneHumanoidAssignments.Bones.ContainsKey(bone.boneType))
                            {
                                metaDataFile.modelBoneHumanoidAssignments.Bones.Add(bone.boneType, bone.bone);
                            }
                            await default(ToBackground);






                            boneName = string.Empty;
                            boneNameHuman = string.Empty;
                            
                        }


                        break;
                    

                }
            }
            await default(ToWorld);
            //to initialize our Rig's biped forward at the end for VRIK.
            metaDataFile.modelBoneHumanoidAssignments.GuessForwardFlipped();
            metaDataFile.modelBoneHumanoidAssignments.DetectHandRigs();
            await default(ToBackground);
            return metaDataFile;
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