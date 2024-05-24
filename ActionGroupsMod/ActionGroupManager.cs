using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using SFS.Input;
using SFS.World;
using SFS.Parts;

namespace ActionGroupsMod
{
    public static class ActionGroupManager
    {
        public static bool InWorld => SceneManager.GetActiveScene().name == "World_PC";
        public static bool InBuild => SceneManager.GetActiveScene().name == "Build_PC";

        public static List<ActionGroup> buildActionGroups = new List<ActionGroup>()
        {
            new ActionGroup(),
            new ActionGroup(),
            new ActionGroup(),
            new ActionGroup(),
            new ActionGroup(),
            new ActionGroup(),
        };

        public static List<ActionGroup> GetCurrentActionGroups()
        {
            if (InWorld)
            {
                if (PlayerController.main.player.Value is Rocket rocket)
                {
                    return rocket.GetOrAddComponent<ActionGroupModule>().actionGroups;
                }
            }
            else if (InBuild)
            {
                return buildActionGroups;
            }
            return null;
        }
    }

    [Serializable]
    public class ActionGroup
    {
        public string name = "Unnamed";
        public KeybindingsPC.Key key = null;
        public List<Part> parts = new List<Part>();

        public void RemoveDestroyedParts()
        {
            parts = parts.Where((Part p) => p != null).ToList();
        }
    }
}