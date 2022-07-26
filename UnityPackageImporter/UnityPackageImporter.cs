using BaseX;
using CodeX;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityPackageImporter.Extractor;

namespace UnityPackageImporter
{
    public class UnityPackageImporter : NeosMod
    {
        public override string Name => "UnityPackageImporter";
        public override string Author => "dfgHiatus, eia485, delta";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/dfgHiatus/NeosUnityPackagesImporter";
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

        public static string DecomposeUnityPackage(string[] files)
        {
            Dictionary<string, string> hashToFile = new();
            foreach (string file in files)
            {
                hashToFile.Add(GenerateMD5(file), file);
            }

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
            //var files = Directory.GetFiles(extractedPath);
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
            return;
        }

        private static bool ContainsUnicodeCharacter(string input)
        {
            const int MaxAnsiCode = 255;
            return input.Any(c => c > MaxAnsiCode);
        }

        [HarmonyPatch(typeof(UniversalImporter), "Import")]
        class UniversalImporterPatch
        {
            static bool Prefix(ref IEnumerable<string> files)
            {
                string[] hasUnityPackage = new string[] { };
                string[] notUnityPackage = new string[] { };
                foreach (string file in files)
                {
                    if (file.ToLower().EndsWith(".unitypackage"))
                    {
                        hasUnityPackage.AddItem(file);
                    }
                    else
                    {
                        notUnityPackage.AddItem(file);
                    }
                }






                if (notUnityPackage.Length > 0)
                {
                    files = notUnityPackage.ToArray();
                    return true;
                }
                return false;
            }
        }

        //credit to delta for this method https://github.com/XDelta/
        private static string GenerateMD5(string filepath)
        {
            using (var hasher = MD5.Create())
            {
                using (var stream = File.OpenRead(filepath))
                {
                    var hash = hasher.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }
    }
}