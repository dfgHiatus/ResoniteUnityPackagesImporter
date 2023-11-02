using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace UnityPackageImporter.Models
{
    internal class FileImportHelperTaskMaterial
    {
        public string file;
        public string myID;
        public Slot myMeshRendererSlot;
        public FrooxEngine.PBS_Metallic finalMaterial;
        public Slot assetsRoot;
        public Task task;
        public SharedData state;

        public FileImportHelperTaskMaterial(string file, string myID, Slot myMeshRendererSlot, Slot assetsRoot, SharedData __state )
        {
            this.file = file;
            this.myID = myID;
            this.myMeshRendererSlot = myMeshRendererSlot;
            Slot matslot = assetsRoot.AddSlot(Path.GetFileNameWithoutExtension(file) + " - Material");
            finalMaterial = matslot.AttachComponent<FrooxEngine.PBS_Metallic>();
            this.state = __state;
            this.task = myMeshRendererSlot.StartGlobalTask(async () => await runImportFileMaterialsAsync());
            
        }

        private async Task runImportFileMaterialsAsync()
        {
            await default(ToBackground);
            await ImportFileMaterial();
        }


        private async Task ImportFileMaterial()
        {
            bool inTextureBlock = false;
            await default(ToBackground);
            string texturename = string.Empty;
            var lines = File.ReadLines(file).ToArray();

            for (int i=0; i < lines.Length;i++ )
            {
                if (lines[i].StartsWith("    m_TexEnvs:"))
                {
                    inTextureBlock = true;
                    continue;
                }

                


                if (inTextureBlock)
                {
                    if (!lines[i].Contains("}")){
                        texturename = lines[i].Split('_')[1].Split(':')[0].Trim();
                        switch (texturename)
                        {
                            case "BumpMap":
                                try
                                {
                                    string textureid = lines[i + 1].Split(':')[3].Split(',')[0].Trim();

                                    Task<StaticTexture2D> waitFor = ImportTexture(textureid);
                                    Task.WaitAll(waitFor);
                                    UnityPackageImporter.Msg("Assigning texture for material. ID of texture is: " + textureid);
                                    StaticTexture2D albedo = waitFor.Result;

                                    finalMaterial.NormalMap.Target = albedo;
                                }
                                catch(Exception e){
                                    UnityPackageImporter.Msg("Failed to find " + "BumpMap" + " texture! Stacktrace: ");
                                    UnityPackageImporter.Msg(e.StackTrace);
                                }
                                

                                break;
                            case "DetailAlbedoMap":
                                try
                                {
                                    string textureid = lines[i + 1].Split(':')[3].Split(',')[0].Trim();

                                    Task<StaticTexture2D> waitFor = ImportTexture(textureid);
                                    Task.WaitAll(waitFor);
                                    UnityPackageImporter.Msg("Assigning texture for material. ID of texture is: " + textureid);
                                    StaticTexture2D albedo = waitFor.Result;

                                    finalMaterial.DetailAlbedoTexture.Target = albedo;
                                }
                                catch (Exception e)
                                {
                                    UnityPackageImporter.Msg("Failed to find " + "DetailAlbedoMap" + " texture! Stacktrace: ");
                                    UnityPackageImporter.Msg(e.StackTrace);
                                }
                                break;
                            case "DetailMask":
                                try
                                {
                                    //unimplemented by frooxengine.
                                    string textureid = lines[i + 1].Split(':')[3].Split(',')[0].Trim();

                                    Task<StaticTexture2D> waitFor = ImportTexture(textureid);
                                    Task.WaitAll(waitFor);
                                    UnityPackageImporter.Msg("Assigning texture for material. ID of texture is: " + textureid);
                                    StaticTexture2D albedo = waitFor.Result;

                                }
                                catch (Exception e)
                                {
                                    UnityPackageImporter.Msg("Failed to find " + "DetailMask" + " texture! Stacktrace: ");
                                    UnityPackageImporter.Msg(e.StackTrace);
                                }
                                break;
                            case "DetailNormalMap":
                                try
                                {
                                    string textureid = lines[i + 1].Split(':')[3].Split(',')[0].Trim();

                                    Task<StaticTexture2D> waitFor = ImportTexture(textureid);
                                    Task.WaitAll(waitFor);
                                    UnityPackageImporter.Msg("Assigning texture for material. ID of texture is: " + textureid);
                                    StaticTexture2D albedo = waitFor.Result;

                                    finalMaterial.DetailNormalMap.Target = albedo;
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
                                    string textureid = lines[i + 1].Split(':')[3].Split(',')[0].Trim();
                                    Task<StaticTexture2D> waitFor = ImportTexture(textureid);
                                    Task.WaitAll(waitFor);
                                    UnityPackageImporter.Msg("Assigning texture for material. ID of texture is: " + textureid);
                                    StaticTexture2D albedo = waitFor.Result;

                                    finalMaterial.EmissiveMap.Target = albedo;
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
                                    string textureid = lines[i + 1].Split(':')[3].Split(',')[0].Trim();

                                    Task<StaticTexture2D> waitFor = ImportTexture(textureid);
                                    Task.WaitAll(waitFor);
                                    UnityPackageImporter.Msg("Assigning texture for material. ID of texture is: " + textureid);
                                    StaticTexture2D albedo = waitFor.Result;

                                    finalMaterial.AlbedoTexture.Target = albedo;
                                }
                                catch (Exception e)
                                {
                                    UnityPackageImporter.Msg("Failed to find " + "MainTex" + " texture! Stacktrace: ");
                                    UnityPackageImporter.Msg(e.StackTrace);
                                }

                                float.TryParse(lines[i + 2].Split(':')[2].Split(',')[0].Trim(), out float x);
                                float.TryParse(lines[i + 2].Split(':')[3].Split(',')[0].Trim(), out float y);

                                finalMaterial.TextureScale.Value = new float2(x, y);

                                float.TryParse(lines[i + 3].Split(':')[2].Split(',')[0].Trim(), out float x2);
                                float.TryParse(lines[i + 3].Split(':')[3].Split(',')[0].Trim(), out float y2);

                                finalMaterial.TextureOffset.Value = new float2(x2, y2);
                                break;
                            case "OcclusionMap":
                                try
                                {
                                    string textureid = lines[i + 1].Split(':')[3].Split(',')[0].Trim();

                                    Task<StaticTexture2D> waitFor = ImportTexture(textureid);
                                    Task.WaitAll(waitFor);
                                    UnityPackageImporter.Msg("Assigning texture for material. ID of texture is: " + textureid);
                                    StaticTexture2D albedo = waitFor.Result;

                                    finalMaterial.OcclusionMap.Target = albedo;
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

        }



        public async Task<StaticTexture2D> ImportTexture(string idtarget)
        {
            await default(ToWorld);
            try
            {
                string metaFilePath = this.state.AssetIDDict.First(i => i.key.Equals(idtarget)).value;
                string f = metaFilePath.Remove(0, metaFilePath.Length - Path.GetExtension(metaFilePath).Length);
                StaticTexture2D staticTexture2D = assetsRoot.AddSlot(Path.GetFileName(f)).AttachComponent<StaticTexture2D>();
                Uri url;
                string tempFilePath = this.assetsRoot.World.Engine.LocalDB.GetTempFilePath("png");
                TextureEncoder.ConvertToPNG(f, tempFilePath, 4096 * 2);
                UnityPackageImporter.Msg("Importing URI for texture " + idtarget);
                Task<Uri> waitfor = this.assetsRoot.World.Engine.LocalDB.ImportLocalAssetAsync(tempFilePath, LocalDB.ImportLocation.Move);
                Task.WaitAll(waitfor);
                url = waitfor.Result;
                UnityPackageImporter.Msg("Imported URI for texture " + idtarget + " URI is: " + url.ToString());
                
                staticTexture2D.URL.Value = url;
                
            }
            catch (Exception e)
            {
                UnityPackageImporter.Msg("Texture import for ID: \"" + idtarget + "\" failed! Assigning missing texure. Stacktrace:");
                UnityPackageImporter.Msg(e.StackTrace);
            }
            
            StaticTexture2D returnThis = assetsRoot.AddSlot(idtarget).AttachComponent<StaticTexture2D>();
            await default(ToBackground);
            return returnThis;

        }
    }
}
