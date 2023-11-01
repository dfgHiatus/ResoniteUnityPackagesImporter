using Elements.Core;
using FrooxEngine;
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

        public static MetaDataFile ScanFile(string path, Slot ModelRootSlot)
        {
            //section identification
            int sectiontype = -1;

            //bonereading section
            string boneName = string.Empty;
            string boneNameHuman = string.Empty;



            MetaDataFile metaDataFile = new MetaDataFile();
            metaDataFile.modelBoneHumanoidAssignments = ModelRootSlot.AttachComponent<BipedRig>();
            foreach (string line in File.ReadLines(path))
            {
                
                if (line.StartsWith("  humanDescription:"))
                {
                    sectiontype = 0;
                    continue;

                }
                if (line.StartsWith("    previousCalculatedGlobalScale:"))
                {
                    string numberStr = line.Split(':')[1].Trim();
                    UnityPackageImporter.Msg("found scale \"" + numberStr + "\", parsing");
                    metaDataFile.GlobalScale = float.Parse(numberStr);
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
                            Rig.BoneNode bone = new Rig.BoneNode(ModelRootSlot.FindChild(boneName,false,false,-1), HumanoidNameToEnum(boneNameHuman));

                            //so that we add the bone without parsing it's children
                            //if we use """AssignBones(Rig.BoneNode root, bool ignoreDuplicates)""" that will cause errors.
                            if (!metaDataFile.modelBoneHumanoidAssignments.Bones.ContainsKey(bone.boneType))
                            {
                                metaDataFile.modelBoneHumanoidAssignments.Bones.Add(bone.boneType, bone.bone);
                            }


                            




                            boneName = string.Empty;
                            boneNameHuman = string.Empty;
                            
                        }


                        break;
                    

                }
            }
            //to initialize our Rig's biped forward at the end for VRIK.
            metaDataFile.modelBoneHumanoidAssignments.GuessForwardFlipped();
            return metaDataFile;
        }

        //Since Unity names and Froox Engine names are the same, just parse them as enums and return.
        public static BodyNode HumanoidNameToEnum(string boneNameHuman)
        {
            boneNameHuman = boneNameHuman.Replace(" ", "");

            if(Enum.TryParse(boneNameHuman, out BodyNode bodyNode))
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