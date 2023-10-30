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

                            metaDataFile.modelBoneHumanoidAssignments.AssignBones(bone, true);




                            boneName = string.Empty;
                            boneNameHuman = string.Empty;
                            
                        }


                        break;
                    

                }















                
            }

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