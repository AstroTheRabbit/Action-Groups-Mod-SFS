using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using ModLoader;
using ModLoader.Helpers;
using SFS.IO;
using SFS.World;
using UITools;

namespace ActionGroupsMod
{
    [UsedImplicitly]
    public class Entrypoint : Mod, IUpdatable
    {
        public static Entrypoint Main { get; private set; }
        public override string ModNameID => "actiongroups";
        public override string DisplayName => "Action Groups";
        public override string Author => "Astro The Rabbit";
        public override string MinimumGameVersionNecessary => "1.6.0.14";
        public override string ModVersion => "1.5";
        public override string Description => "Adds KSP-like part action groups to SFS.";

        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string>
        {
            { "UITools", "1.1.5" },
            { "customsavedata", "1.3" }
        };
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>()
        {
            {
                "https://github.com/AstroTheRabbit/Action-Groups-Mod-SFS/releases/latest/download/ActionGroupsMod.dll",
                new FolderPath(ModFolder).ExtendToFile("ActionGroupsMod.dll")
            }
        };

        public override void Early_Load()
        {
            new Harmony(ModNameID).PatchAll();
            Main = this;
        }

        public override void Load()
        {
            Settings.Init();
            SavingHelpers.AddHelpers();
            SceneHelper.OnWorldSceneLoaded += () => PlayerController.main.player.OnChange += GUI.OnPlayerChange;
            SceneHelper.OnWorldSceneUnloaded += () => PlayerController.main.player.OnChange -= GUI.OnPlayerChange;
        }
    }
}
