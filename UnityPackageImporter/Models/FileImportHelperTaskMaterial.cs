using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Policy;
using System.Threading.Tasks;
using UnityPackageImporter.FrooxEngineRepresentation;
using static FrooxEngine.ModelImporter;

namespace UnityPackageImporter.Models
{
    public class FileImportHelperTaskMaterial
    {
        public string file;
        public string myID;
        public Slot assetsRoot;
        public Slot matslot;
        public UnityProjectImporter importer;

        public FileImportHelperTaskMaterial(string myID, string file, UnityProjectImporter importer)
        {
            this.importer = importer;

            UnityPackageImporter.Msg("Importing material with ID: \""+myID+"\"");
            UnityPackageImporter.Msg("and a file of: \"" + this.file + "\"");
            UnityPackageImporter.Msg("and a slot assets root of " + importer.importTaskAssetRoot.ToString());
            assetsRoot = importer.importTaskAssetRoot;
            matslot = assetsRoot.AddSlot(Path.GetFileNameWithoutExtension(file) + " - Material");
            UnityPackageImporter.Msg("and a mat slot of " + matslot.ToString());
            this.myID = myID;
        }

        public async Task<FrooxEngine.PBS_Metallic> runImportFileMaterialsAsync()
        {
            await default(ToBackground);
            return await ImportFileMaterial();
        }


        private async Task<FrooxEngine.PBS_Metallic> ImportFileMaterial()
        {
            await default(ToWorld);
            FrooxEngine.PBS_Metallic finalMaterial = matslot.AttachComponent<FrooxEngine.PBS_Metallic>();
            await default(ToBackground);
            bool inTextureBlock = false;
            await default(ToBackground);
            string texturename = string.Empty;
            var lines = File.ReadLines(file).ToArray();

            for (int i=0; i < lines.Length;i++ )
            {
                if (lines[i].Trim().StartsWith("m_TexEnvs:"))
                {
                    inTextureBlock = true;
                    continue;
                }
                else if (lines[i].Trim().StartsWith("m_Floats:"))
                {
                    inTextureBlock = false;
                    continue;
                }




                if (inTextureBlock)
                {
                    if (!lines[i].Contains('}')){
                        StaticTexture2D importedTexture;
                        try
                        {
                            texturename = lines[i].Split('_')[1].Split(':')[0].Trim();
                            string textureid = lines[i + 1].Split(':')[3].Split(',')[0].Trim();
                            UnityPackageImporter.Msg("Assigning texture for material, Material id is:\"" + this.myID + "\" ID of texture is: \"" + textureid + "\"");
                            await default(ToWorld);
                            importedTexture = await ImportTexture(textureid);
                            await default(ToBackground);
                            UnityPackageImporter.Msg("Finished importing texture\"" + textureid + "\" Material id is:\"" + this.myID + "\"");
                        }
                        catch(Exception)
                        {
                            continue;
                        }
                        switch (texturename) {
                            case "BumpMap":
                                try
                                {
                                    await default(ToWorld);

                                    finalMaterial.NormalMap.Target = importedTexture;
                                    await default(ToBackground);
                                }
                                catch(Exception e){
                                    UnityPackageImporter.Msg("Failed to find " + "BumpMap" + " texture! Stacktrace: ");
                                    UnityPackageImporter.Msg(e.StackTrace);
                                }
                                

                                break;
                            case "DetailAlbedoMap":
                                try
                                {
                                    await default(ToWorld);
                                    finalMaterial.DetailAlbedoTexture.Target = importedTexture;
                                    await default(ToBackground);

                                }
                                catch (Exception e)
                                {
                                    UnityPackageImporter.Msg("Failed to find " + "DetailAlbedoMap" + " texture! Stacktrace: ");
                                    UnityPackageImporter.Msg(e.StackTrace);
                                }
                                await default(ToWorld);
                                float.TryParse(lines[i + 2].Split(':')[2].Split(',')[0].Trim(), out float x);
                                float.TryParse(lines[i + 2].Split(':')[3].Split('}')[0].Trim(), out float y);

                                finalMaterial.DetailTextureScale.Value = new float2(x, y);

                                float.TryParse(lines[i + 3].Split(':')[2].Split(',')[0].Trim(), out float x2);
                                float.TryParse(lines[i + 3].Split(':')[3].Split('}')[0].Trim(), out float y2);

                                finalMaterial.DetailTextureOffset.Value = new float2(x2, y2);
                                await default(ToBackground);
                                break;
                            /*case "DetailMask":
                                try
                                {
                                    //unimplemented by frooxengine.


                                    //UnityPackageImporter.Msg("Assigning texture for material. ID of texture is: " + textureid);
                                    //StaticTexture2D albedo = await ImportTexture(textureid);
                                    //await default(ToWorld);
                                    //finalMaterial.DetailMask.Target = albedo;
                                    //await default(ToBackground);
                                    //UnityPackageImporter.Msg("Finished importing " + textureid);

                                }
                                catch (Exception e)
                                {
                                    UnityPackageImporter.Msg("Failed to find " + "DetailMask" + " texture! Stacktrace: ");
                                    UnityPackageImporter.Msg(e.StackTrace);
                                }
                                break;*/
                            case "DetailNormalMap":
                                try
                                {
                                    await default(ToWorld);
                                    finalMaterial.DetailNormalMap.Target = importedTexture;
                                    await default(ToBackground);
                                }
                                catch (Exception e)
                                {
                                    UnityPackageImporter.Msg("Failed to find " + "DetailNormalMap" + " texture! Stacktrace: ");
                                    UnityPackageImporter.Msg(e.StackTrace);
                                }
                                break;
                            case "EmissionMap":
                                try
                                {
                                    await default(ToWorld);
                                    finalMaterial.EmissiveMap.Target = importedTexture;
                                    await default(ToBackground);
                                }
                                catch (Exception e)
                                {
                                    UnityPackageImporter.Msg("Failed to find " + "EmissionMap" + " texture! Stacktrace: ");
                                    UnityPackageImporter.Msg(e.StackTrace);
                                }
                                break;
                            case "MainTex":
                                try
                                {
                                    await default(ToWorld);
                                    finalMaterial.AlbedoTexture.Target = importedTexture;
                                    await default(ToBackground);
                                }
                                catch (Exception e)
                                {
                                    UnityPackageImporter.Msg("Failed to find " + "MainTex" + " texture! Stacktrace: ");
                                    UnityPackageImporter.Msg(e.StackTrace);
                                }
                                await default(ToWorld);
                                float.TryParse(lines[i + 2].Split(':')[2].Split(',')[0].Trim(), out float x1);
                                float.TryParse(lines[i + 2].Split(':')[3].Split('}')[0].Trim(), out float y1);

                                finalMaterial.TextureScale.Value = new float2(x1, y1);

                                float.TryParse(lines[i + 3].Split(':')[2].Split(',')[0].Trim(), out float x1_2);
                                float.TryParse(lines[i + 3].Split(':')[3].Split('}')[0].Trim(), out float y1_2);

                                finalMaterial.TextureOffset.Value = new float2(x1_2, y1_2);
                                await default(ToBackground);
                                break;
                            case "OcclusionMap":
                                try
                                {
                                    await default(ToWorld);
                                    finalMaterial.OcclusionMap.Target = importedTexture;
                                    await default(ToBackground);
                                }
                                catch (Exception e)
                                {
                                    UnityPackageImporter.Msg("Failed to find " + "OcclusionMap" + " texture! Stacktrace: ");
                                    UnityPackageImporter.Msg(e.StackTrace);
                                }
                                break;
                        }
                    }



                    


                }



            }

            await default(ToBackground);
            return finalMaterial;

        }



        public async Task<StaticTexture2D> ImportTexture(string idtarget)
        {
            await default(ToBackground);
            StaticTexture2D staticTexture2D = null;
            try
            {
                UnityPackageImporter.Msg("Path is being found for texture " + idtarget);
                string f = importer.AssetIDDict[idtarget];
                UnityPackageImporter.Msg("Path is found for texture " + idtarget + " path is: \"" + f + "\"");
                await default(ToWorld);
                UnityPackageImporter.Msg("adding tex slot for texture " + idtarget + " path is: \"" + f + "\"");
                //so that we don't try importing the same texture twice.
                if (assetsRoot.GetAllChildren().Exists(i => i.Name.Equals(Path.GetFileName(f) + " - Texture")))
                {
                    return assetsRoot.GetAllChildren().First(i => i.Name.Equals(Path.GetFileName(f) + " - Texture")).GetComponent<StaticTexture2D>();
                }
                Slot slot = matslot.AddSlot(Path.GetFileName(f) + " - Texture");
                UnityPackageImporter.Msg("adding tex onto slot for texture " + idtarget + " path is: \"" + f + "\"");
                staticTexture2D = slot.AttachComponent<StaticTexture2D>();
                UnityPackageImporter.Msg("Importing URI for texture " + idtarget);
                Uri url = await this.assetsRoot.World.Engine.LocalDB.ImportLocalAssetAsync(f, LocalDB.ImportLocation.Copy);
                UnityPackageImporter.Msg("Imported URI for texture " + idtarget + " URI is: " + url.ToString());
                staticTexture2D.URL.Value = url;
                UnityPackageImporter.Msg("URI is assigned for texture " + idtarget + " URI is: " + url.ToString());
                await default(ToBackground);

            }
            catch (Exception e)
            {
                UnityPackageImporter.Msg("Texture import for ID: \"" + idtarget + "\" failed! Assigning missing texure. Stacktrace:");
                UnityPackageImporter.Msg(e.StackTrace);
            }
            await default(ToBackground);
            return staticTexture2D;

        }
    }
}
