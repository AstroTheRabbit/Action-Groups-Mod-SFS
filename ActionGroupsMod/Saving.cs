using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SFS.Input;
using SFS.Parts;
using SFS.World;
using SFS.Builds;
using CustomSaveData;

namespace ActionGroupsMod
{
    [Serializable]
    public class ActionGroupSave
    {
        public string name;
        public KeybindingsPC.Key key;
        public bool holdToActivate;
        public List<int> partIndices;

        public ActionGroupSave() {}

        public ActionGroupSave(ActionGroup ag, List<Part> parts)
        {
            name = ag.name;
            key = ag.key;
            holdToActivate = ag.holdToActivate;
            partIndices = ag.parts.Select(p => parts.IndexOf(p)).Where(idx => idx != -1).ToList();
        }
    }

    public static class SavingHelpers
    {
        const string dataId = "actionGroups";

        static List<ActionGroupSave> CreateSaves(List<ActionGroup> actionGroups, List<Part> parts)
        {
            return actionGroups.Select(ag => new ActionGroupSave(ag, parts)).ToList();
        }
        static List<ActionGroup> LoadSaves(List<ActionGroupSave> saves, List<Part> parts)
        {
            return saves.Select(save => new ActionGroup(save, parts)).ToList();
        }

        public static void AddHelpers()
        {
            CustomSaveData.Main.BlueprintHelper.OnSave += Blueprint_OnSave;
            CustomSaveData.Main.BlueprintHelper.OnLoad += Blueprint_OnLoad;
            CustomSaveData.Main.BlueprintHelper.OnLaunch += Blueprint_OnLaunch;
            CustomSaveData.Main.RocketSaveHelper.OnSave += RocketSave_OnSave;
            CustomSaveData.Main.RocketSaveHelper.OnLoad += RocketSave_OnLoad;
        }

        static void Blueprint_OnSave(CustomBlueprint blueprint)
        {
            List<ActionGroupSave> saves = CreateSaves(ActionGroupManager.buildActionGroups, BuildState.main.buildGrid.activeGrid.partsHolder.parts);
            blueprint.AddCustomData(dataId, saves);
        }
        static void Blueprint_OnLoad(CustomBlueprint blueprint)
        {
            if (blueprint.GetCustomData(dataId, out List<ActionGroupSave> saves))
            {
                ActionGroupManager.buildActionGroups = LoadSaves(saves, BuildState.main.buildGrid.activeGrid.partsHolder.parts);
            }
            else
            {
                Debug.LogWarning("Missing action group data when loading blueprint.");
                ActionGroupManager.buildActionGroups = new List<ActionGroup>();
            }
            GUI.UpdateUI(null);
        }
        static void Blueprint_OnLaunch(CustomBlueprint blueprint, Rocket[] rockets, Part[] parts)
        {
            if (!blueprint.GetCustomData(dataId, out List<ActionGroupSave> saves))
            {
                Debug.LogWarning("Missing action group data when launching rocket.");
            }

            foreach (ActionGroupSave save in saves)
            {
                Dictionary<Rocket, ActionGroup> groups = new Dictionary<Rocket, ActionGroup>();
                foreach (int idx in save.partIndices)
                {
                    if (!parts.IsValidIndex(idx))
                        continue;

                    Part part = parts[idx];

                    if (groups.TryGetValue(part.Rocket, out ActionGroup group))
                    {
                        group.AddPart(part, out _);
                    }
                    else
                    {
                        group = new ActionGroup()
                        {
                            name = save.name,
                            key = save.key,
                            holdToActivate = save.holdToActivate,
                            parts = new List<Part>() { part },
                        };
                        part.Rocket.GetOrAddComponent<ActionGroupModule>().actionGroups.Add(group);
                        groups.Add(part.Rocket, group);
                    }
                }
            }
            GUI.UpdateUI(null);
        }
        static void RocketSave_OnSave(CustomRocketSave rocketSave, Rocket rocket)
        {
            ActionGroupModule module = rocket.GetOrAddComponent<ActionGroupModule>();
            List<ActionGroupSave> saves = CreateSaves(module.actionGroups, rocket.partHolder.parts);
            rocketSave.AddCustomData(dataId, saves);
        }
        static void RocketSave_OnLoad(CustomRocketSave rocketSave, Rocket rocket)
        {
            if (rocketSave.GetCustomData(dataId, out List<ActionGroupSave> saves))
            {
                rocket.GetOrAddComponent<ActionGroupModule>().actionGroups = LoadSaves(saves, rocket.partHolder.parts);
            }
            else
            {
                Debug.LogWarning("Missing action group data when loading blueprint.");
            }
            GUI.UpdateUI(null);
        }
    }
}