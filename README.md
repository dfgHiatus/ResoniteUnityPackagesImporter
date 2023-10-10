# UnityPackageImporter

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite VR](https://Resonite.com/) that facilitates the import of Unity Packages.

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Place [UnityPackageImporter.dll](https://github.com/dfgHiatus/ResoniteUnityPackagesImporter/releases/latest/download/UnityPackageImporter.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Resonite logs.

## FAQs
1. <b>What does this mod do exactly?</b> This mod solely extracts the assets of Unity Packages and imports them into Resonite
1. <b>Will this mod setup avatars for me?</b> No, you'll need to set them up normally.
1. <b>What kind of unity packages can I import?</b> Anything! Avatar packages, world packages, anything you can think of!
1. <b>Will this mod run on the Linux version of the game?</b> Yes
1. <b>How many packages can I import at once?</b> In theory you should be able to import many at once, but in my experience one at a time is your best friend here given the <i>massive</i> file size Unity Packages can be
1. <b>Help! The files I imported are file-looking things!</b> Using [ResoniteModSettings](https://github.com/badhaloninja/ResoniteModSettings), you'll see the topmost option is to "Import files directly into Resonite". You can set this to false, but be wary as you may import thousands of things all at once!
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
9. <b>What will this mod NOT import?</b>
- Prefabs
- Scenes
- Particle systems
- Animations/Animators
- Dynamic Bones
- Phys Bones

## Known Issues and Limitations
- This will not work with the legacy file importer. Paste/drag and drop the UnityPackage onto the Resonite window instead
- This doesn't place files under a single root
- This might hang a little bit during import. Recommended to import one unity package at a time, pairs nicely with [ResoniteModSettings](https://github.com/badhaloninja/ResoniteModSettings)
