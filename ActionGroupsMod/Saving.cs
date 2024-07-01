using System;
using System.Linq;
using System.Collections.Generic;
using SFS.Input;
using SFS.Parts;
using SFS.World;
using SFS.Builds;

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
            partIndices = ag.parts.Select((Part p) => parts.IndexOf(p)).Where((int idx) => idx != -1).ToList();
        }

        static List<ActionGroupSave> CreateSaves(List<ActionGroup> actionGroups, List<Part> parts)
        {
            return actionGroups.Select((ActionGroup ag) => new ActionGroupSave(ag, parts)).ToList();
        }

        public static List<ActionGroupSave> CreateSavesBuild()
        {
            return CreateSaves(ActionGroupManager.buildActionGroups, BuildState.main.buildGrid.activeGrid.partsHolder.parts);
        }

        public static List<ActionGroupSave> CreateSavesWorld(Rocket rocket)
        {
            return CreateSaves(rocket.GetOrAddComponent<ActionGroupModule>().actionGroups, rocket.partHolder.parts);
        }

        static List<ActionGroup> LoadSaves(List<ActionGroupSave> saves, List<Part> parts)
        {
            return saves.Select((ActionGroupSave save) => new ActionGroup(save, parts)).ToList();
        }

        public static void LoadSavesBuild(List<ActionGroupSave> saves)
        {
            ActionGroupManager.buildActionGroups = LoadSaves(saves, BuildState.main.buildGrid.activeGrid.partsHolder.parts);
        }

        public static void LoadSavesWorld(List<ActionGroupSave> saves, Rocket rocket)
        {
            rocket.GetOrAddComponent<ActionGroupModule>().actionGroups = LoadSaves(saves, rocket.partHolder.parts);
        }

        // ? Similar to `SFS.World.Staging.CreateStages`.
        public static void OnBlueprintSpawn(List<ActionGroupSave> saves, Part[] parts)
        {
            foreach (ActionGroupSave save in saves)
            {
                List<Rocket> rockets = new List<Rocket>();
                foreach (int idx in save.partIndices)
                {
                    if (!parts.IsValidIndex(idx))
                        continue;
                    
                    Part part = parts[idx];
                    List<ActionGroup> actionGroups = part.Rocket.GetOrAddComponent<ActionGroupModule>().actionGroups;
                    if (!rockets.Contains(part.Rocket))
                    {
                        actionGroups.Add
                        (
                            new ActionGroup()
                            {
                                name = save.name,
                                key = save.key,
                                holdToActivate = save.holdToActivate,
                                parts = new List<Part>() { part },
                            }
                        );
                        rockets.Add(part.Rocket);
                    }
                    else
                    {
                        actionGroups.Last().parts.Add(part);
                    }
                }
            }
        }
    }
}