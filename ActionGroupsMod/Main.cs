using HarmonyLib;
using UnityEngine;
using ModLoader;
using ModLoader.Helpers;
using UITools;
using SFS.IO;
using System.Collections.Generic;

namespace ActionGroupsMod
{
    public class Main : Mod
    {
        public static Main main;
        public override string ModNameID => "actiongroups";
        public override string DisplayName => "Action Groups";
        public override string Author => "Astro The Rabbit";
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string ModVersion => "v1.0";
        public override string Description => "Adds KSP-like part action groups to SFS.";

        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.1" } };
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>() { { "https://github.com/AstroTheRabbit/Action-Groups-Mod-SFS/releases/latest/download/ActionGroups.dll", new FolderPath(ModFolder).ExtendToFile("ActionGroups.dll") } };

        public override void Early_Load()
        {
            new Harmony(ModNameID).PatchAll();
            main = this;
        }

        public override void Load()
        {
            SceneHelper.OnBuildSceneLoaded += () => GUI.CreateUI("build");
            SceneHelper.OnWorldSceneLoaded += () => GUI.CreateUI("world");
            SceneHelper.OnBuildSceneUnloaded += GUI.DestroyWindow;
            SceneHelper.OnWorldSceneUnloaded += GUI.DestroyWindow;
        }
    }
}
