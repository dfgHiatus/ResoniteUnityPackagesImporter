using HarmonyLib;
using NeosModLoader;

namespace ModNameGoesHere
{
    public class ModNameGoesHere : NeosMod
    {
        public override string Name => "ModNameGoesHere";
        public override string Author => "username";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/GithubUsername/RepoName/";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("net.username.Template");
            harmony.PatchAll();
        }
		
        [HarmonyPatch(typeof(class you want to patch), "name of method you want to patch")]
        class ModNameGoesHerePatch
        {
            public static bool Prefix()
            {
                return false;//dont run rest of method
            }
        }
    }
}