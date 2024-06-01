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
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>() { { "https://github.com/AstroTheRabbit/Action-Groups-Mod-SFS/releases/latest/download/ActionGroups.dll", new FolderPath(ModFolder).ExtendToFile("ActionGroups.dll") } };

        public override void Early_Load()
        {
            new Harmony(ModNameID).PatchAll();
            main = this;
        }

        public override void Load()
        {
            // SceneHelper.OnWorldSceneLoaded += () => GUI.CreateUI("world");
            // SceneHelper.OnBuildSceneLoaded += () => GUI.CreateUI("build");

            // SceneHelper.OnWorldSceneLoaded += () => PlayerController.main.player.OnChange += Patches.OnPlayerChange;
            // SceneHelper.OnWorldSceneUnloaded += () => PlayerController.main.player.OnChange -= Patches.OnPlayerChange;

            // SceneHelper.OnHomeSceneUnloaded += () =>
            // {
            //     Debug.Log("CustomBlueprintHelper.AddOnSave? " + CustomBlueprintHelper.onSave == null);
            //     Debug.Log("CustomBlueprintHelper.AddOnLoad? " + CustomBlueprintHelper.onLoad == null);
            //     Debug.Log("CustomBlueprintHelper.AddOnLaunch? " + CustomBlueprintHelper.onLaunch == null);
            //     Debug.Log("CustomRocketSaveHelper.AddOnSave? " + CustomRocketSaveHelper.onSave == null);
            //     Debug.Log("CustomRocketSaveHelper.AddOnLoad? " + CustomRocketSaveHelper.onLoad == null);
            // };

            CustomBlueprintHelper.AddOnSave
            (
                (CustomBlueprint bp) =>
                {
                    Debug.Log("CustomBlueprintHelper.AddOnSave!!!");
                    bp.AddCustomData(ModNameID, ActionGroupSave.CreateSavesBuild());
                }
            );

            CustomBlueprintHelper.AddOnLoad
            (
                (CustomBlueprint bp) =>
                {
                    Debug.Log("CustomBlueprintHelper.AddOnLoad!!!");
                    List<ActionGroupSave> saves = bp.GetCustomData<List<ActionGroupSave>>(ModNameID, out bool successful);
                    if (successful)
                        ActionGroupSave.LoadSavesBuild(saves);
                    else
                        ActionGroupManager.buildActionGroups = new List<ActionGroup>();
                    GUI.UpdateUI(null);
                }
            );
            CustomBlueprintHelper.AddOnLaunch
            (
                (CustomBlueprint bp, Rocket[] rockets, Part[] parts) =>
                {
                    Debug.Log("CustomBlueprintHelper.AddOnLaunch!!!");
                    List<ActionGroupSave> saves = bp.GetCustomData<List<ActionGroupSave>>(ModNameID, out bool successful);
                    if (successful)
                        ActionGroupSave.OnBlueprintSpawn(saves, parts);
                }
            );

            CustomRocketSaveHelper.AddOnSave
            (
                (CustomRocketSave save, Rocket rocket) =>
                {
                    Debug.Log("CustomRocketSaveHelper.AddOnSave!!!");
                    if (rocket.TryGetComponent(out ActionGroupModule actionGroupModule))
                        save.AddCustomData(ModNameID, actionGroupModule.actionGroups);
                }
            );
            CustomRocketSaveHelper.AddOnLoad
            (
                (CustomRocketSave save, Rocket rocket) =>
                {
                    Debug.Log("CustomRocketSaveHelper.AddOnLoad!!!");
                    var saves = save.GetCustomData<List<ActionGroupSave>>(ModNameID, out bool successful);
                    if (successful)
                        ActionGroupSave.LoadSavesWorld(saves, rocket);
                    GUI.UpdateUI(null);
                }
            );
        }
    }
}
