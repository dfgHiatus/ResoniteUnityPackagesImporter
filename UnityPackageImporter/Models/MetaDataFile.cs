using Elements.Core;
using FrooxEngine;
using Microsoft.Cci;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityPackageImporter.Models;

namespace UnityPackageImporter
{
    public class MetaDataFile
    {
        public BipedRig modelBoneHumanoidAssignments;
        public float GlobalScale = -1;
        public MetaDataFile() {
            
            
        }

        //the second I tried to make this a yaml parser the parser broke. if anyone else can get it to work that's fine by me. - @989onan

        //this creates our biped rig under the slot we specify. in this case, it is the root.
        //it scans the file's metadata to make it work.
        //this MetaDataFile object class gives us access to the biped rig component directly if desired
        //but we're using it to also get our global scale.
        public async Task ScanFile(FileImportTaskScene task, Slot ModelRootSlot)
        {
            //section identification
            int sectiontype = -1;

            //bonereading section
            string boneName = string.Empty;
            string boneNameHuman = string.Empty;


            bool inScaleBlock1 = false;
            bool inScaleBlock2 = false;
            bool inScaleBlock3 = false;

            await default(ToWorld);
            this.modelBoneHumanoidAssignments = ModelRootSlot.AttachComponent<BipedRig>();
            await default(ToBackground);

            UnityPackageImporter.Msg("Humanoid bone description being instanciated is: "+(modelBoneHumanoidAssignments != null));
            foreach (string line in File.ReadLines(task.file + UnityPackageImporter.UNITY_META_EXTENSION))
            {
                
                if (line.StartsWith("  humanDescription:"))
                {
                    sectiontype = 0;
                    continue;

                }
                else if (line.StartsWith("    skeleton:"))
                {
                    inScaleBlock1 = true;

                }
                if (inScaleBlock1)
                {
                    if (line.StartsWith("    - name: "))
                    {
                        inScaleBlock1 = false;
                        inScaleBlock2 = true;
                        continue;
                    }
                }
                if (inScaleBlock2)
                {
                    if (line.StartsWith("    - name: "))
                    {
                        inScaleBlock2 = false;
                        inScaleBlock3 = true;
                        
                        
                    }
                }
                if (inScaleBlock3)
                {
                    if (line.StartsWith("      scale: "))
                    {
                        string numberStr = line.Split(':')[2].Split(',')[0].Trim();
                        UnityPackageImporter.Msg("found scale \"" + numberStr + "\", parsing to get our scale");
                        GlobalScale = float.Parse(numberStr);
                        inScaleBlock3 = false;
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
                            UnityPackageImporter.Msg("finding bone: "+ boneName);
                            Rig.BoneNode bone = new Rig.BoneNode(ModelRootSlot.FindChild(boneName,false,false,-1), HumanoidNameToEnum(boneNameHuman));
                            
                            //so that we add the bone without parsing it's children
                            //if we use """AssignBones(Rig.BoneNode root, bool ignoreDuplicates)""" that will cause errors.
                            if (!modelBoneHumanoidAssignments.Bones.ContainsKey(bone.boneType))
                            {
                                modelBoneHumanoidAssignments.Bones.Add(bone.boneType, bone.bone);
                            }
                            await default(ToBackground);






                            boneName = string.Empty;
                            boneNameHuman = string.Empty;
                            
                        }


                        break;
                    

                }
            }
            await default(ToWorld);
            UnityPackageImporter.Msg("detecting forward flipped of model biped.");
            //this is here to initialize our Rig's biped forward at the end of the import for VRIK later on.
            modelBoneHumanoidAssignments.GuessForwardFlipped();
            modelBoneHumanoidAssignments.DetectHandRigs();
            await default(ToBackground);
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