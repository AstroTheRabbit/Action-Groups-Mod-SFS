using System.Collections.Generic;
using HarmonyLib;
using UITools;
using CustomSaveData;
using ModLoader;
using ModLoader.Helpers;
using SFS.IO;
using SFS.World;
using SFS.Parts;
using UnityEngine;


namespace ActionGroupsMod
{
    public class Main : Mod, IUpdatable
    {
        public static Main main;
        public override string ModNameID => "actiongroups";
        public override string DisplayName => "Action Groups";
        public override string Author => "Astro The Rabbit";
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string ModVersion => "v1.0";
        public override string Description => "Adds KSP-like part action groups to SFS.";

        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.1" }, { "customsavedata", "1.0" } };
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>() ;// { { "https://github.com/AstroTheRabbit/Action-Groups-Mod-SFS/releases/latest/download/ActionGroups.dll", new FolderPath(ModFolder).ExtendToFile("ActionGroups.dll") } };

        public override void Early_Load()
        {
            new Harmony(ModNameID).PatchAll();
            main = this;
        }

        public override void Load()
        {
            SceneHelper.OnWorldSceneLoaded += () => PlayerController.main.player.OnChange += Patches.OnPlayerChange;
            SceneHelper.OnWorldSceneUnloaded += () => PlayerController.main.player.OnChange -= Patches.OnPlayerChange;

            CustomSaveData.Main.BlueprintHelper.OnSave += (CustomBlueprint bp) =>
            {
                bp.AddCustomData(ModNameID, ActionGroupSave.CreateSavesBuild());
            };

            CustomSaveData.Main.BlueprintHelper.OnLoad += (CustomBlueprint bp) =>
            {
                List<ActionGroupSave> saves = bp.GetCustomData<List<ActionGroupSave>>(ModNameID, out bool successful);
                if (successful)
                    ActionGroupSave.LoadSavesBuild(saves);
                else
                    ActionGroupManager.buildActionGroups = new List<ActionGroup>();
                GUI.UpdateUI(null);
            };
            CustomSaveData.Main.BlueprintHelper.OnLaunch += (CustomBlueprint bp, Rocket[] rockets, Part[] parts) =>
            {
                List<ActionGroupSave> saves = bp.GetCustomData<List<ActionGroupSave>>(ModNameID, out bool successful);
                if (successful)
                    ActionGroupSave.OnBlueprintSpawn(saves, parts);
                GUI.UpdateUI(null);
            };

            CustomSaveData.Main.RocketSaveHelper.OnSave += (CustomRocketSave save, Rocket rocket) =>
            {
                if (rocket.TryGetComponent(out ActionGroupModule actionGroupModule))
                    save.AddCustomData(ModNameID, actionGroupModule.actionGroups);
            };
            CustomSaveData.Main.RocketSaveHelper.OnLoad += (CustomRocketSave save, Rocket rocket) =>
            {
                var saves = save.GetCustomData<List<ActionGroupSave>>(ModNameID, out bool successful);
                if (successful)
                    ActionGroupSave.LoadSavesWorld(saves, rocket);
                GUI.UpdateUI(null);
            };
        }
    }
}
