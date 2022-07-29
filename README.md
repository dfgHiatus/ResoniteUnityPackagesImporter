# UnityPackageImporter

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/) that facilitates the import of Unity Packages.

## Installation
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader).
1. Place [UnityPackageImporter](https://github.com/dfgHiatus/NeosUnityPackagesImporter/releases/download/v1.1.0/unityPackageImporter_v1.1.0.zip) into your `nml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Neos logs.

## FAQs
1. <b>What does this mod do exactly?</b> This mod solely extracts the content of Unity Packages and imports them into Neos
1. <b>Will this mod setup avatars for me?</b> No, you'll need to set them up normally.
1. <b>What kind of unity packages can I import?</b> Anything! Avatar packages, world packages, anything you can think of!
1. <b>Will this mod run on the Linux version of the game?</b> Yes
1. <b>Do I need the non-steam build or steam build for this mod to work?</b> Both builds are compatible 
1. <b>How many packages can I import at once?</b> In theory you should be able to import many at once, but in my experience one-at-a-time is your best friend here given the <i>massive</i> file size Unity Packages can be
1. <b>Help! The files I imported are file-looking things!</b> Using [NeosModSettings](https://github.com/badhaloninja/NeosModSettings), you'll see the top most option is to "Import files directly into Neos". You can set this to false, but be wary as you may import thousands of things all at once!
1. <b>What kind of files does this mod import?</b>
Presently, it supports:
- Text
- Images
- Documents 
- 3D Models (including point clouds)
- Audio
- Fonts
- Videos
- And raw binary variants of the above for file sharing

## Known Issues and Limitations
- The importer doesn't place files under a single root, which can be a little messy
- This might hang a little bit during import. Recommended to import one unity package at a time, pairs nicely with [NeosModSettings](https://github.com/badhaloninja/NeosModSettings)
- Should be able to mix and match packages with the import of other files, currently untested
