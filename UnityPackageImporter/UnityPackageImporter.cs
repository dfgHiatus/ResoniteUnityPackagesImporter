using BaseX;
using CodeX;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityPackageImporter.Extractor;

namespace UnityPackageImporter
{
    public class UnityPackageImporter : NeosMod
    {
        public override string Name => "UnityPackageImporter";
        public override string Author => "dfgHiatus";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/GithubUsername/RepoName/";
        public static ModConfiguration config;

        public override void OnEngineInit()
        {
            new Harmony("net.dfgHiatus.UnityPackageImporter").PatchAll();
            config = GetConfiguration();
            Engine.Current.RunPostInit(() => AssetPatch());
        }
        private static void AssetPatch()
        {
            var aExt = Traverse.Create(typeof(AssetHelper)).Field<Dictionary<AssetClass, List<string>>>("associatedExtensions");
            aExt.Value[AssetClass.Special].Add("unitypackage");
        }

        [HarmonyPatch(typeof(ModelPreimporter), "Preimport")]
        public class FileImporterPatch
        {
            public static void Postfix(ref string __result, string model, string tempPath)
            {
                var modelName = Path.GetFileNameWithoutExtension(model);
                if (ContainsUnicodeCharacter(modelName))
                {
                    throw new ArgumentException("Imported unity package cannot have unicode characters in its file name.");
                }

                var trueCachePath = Path.Combine(Engine.Current.CachePath, "Cache");
                var time = DateTime.Now.Ticks.ToString();

                // Make a new directory for the extracted files under trueCachePath
                UnityPackageExtractor extractor = new UnityPackageExtractor();
                var extractedPath = Path.Combine(trueCachePath, modelName + "_" + time);
                extractor.Unpack(model, extractedPath);

                // Inside the extracted directory, create a new directory for the model
                var modelPath = Path.Combine(extractedPath, "_neosImports");
                Directory.CreateDirectory(modelPath);

                // Move all the files to the new directory whose extensions are in the valid2DFileExtensions list and valid3DFileExtensions list
                var files = Directory.GetFiles(extractedPath);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var fileExt = Path.GetExtension(file);
                    if (UnityPackageExtractor.valid2DFileExtensions.Contains(fileExt) || UnityPackageExtractor.valid3DFileExtensions.Contains(fileExt))
                    {
                        var newFilePath = Path.Combine(modelPath, fileName);
                        File.Move(file, newFilePath);
                    }
                }

                // Using the static BatchFolderImporter, import the contents of the new directory
                var importSlot = Engine.Current.WorldManager.FocusedWorld.RootSlot.AddSlot(modelName);
                importSlot.PositionInFrontOfUser();
                BatchFolderImporter.IndividualImport(importSlot, extractedPath);

                // As we are done at this point.
                __result = string.Empty;
                return;
            }

            private static bool ContainsUnicodeCharacter(string input)
            {
                const int MaxAnsiCode = 255;
                return input.Any(c => c > MaxAnsiCode);
            }
        }
    }
}