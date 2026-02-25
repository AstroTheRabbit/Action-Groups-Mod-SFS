using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Builds;
using SFS.Input;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ActionGroupsMod
{
    public static class ActionGroupManager
    {
        public static bool InWorld => SceneManager.GetActiveScene().name == "World_PC";
        public static bool InBuild => SceneManager.GetActiveScene().name == "Build_PC";

        public static List<ActionGroup> buildActionGroups = new List<ActionGroup>();

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

    public class ActionGroup
    {
        public string name = "Unnamed";
        public KeybindingsPC.Key key;
        public bool holdToActivate;
        public bool holdToggled = false;
        public List<Part> parts = new List<Part>();

        internal ActionGroup() { }

        public ActionGroup(ActionGroupSave save, List<Part> partsList)
        {
            name = save.name;
            key = save.key;
            holdToActivate = save.holdToActivate;
            foreach (int i in save.partIndices)
            {
                if (partsList.IsValidIndex(i))
                    AddPart(partsList[i], out _);
            }
        }

        public void TogglePart(Part part, out bool requiresRedraw)
        {
            if (parts.Contains(part))
                RemovePart(part, out requiresRedraw);
            else
                AddPart(part, out requiresRedraw);
        }

        public void AddPart(Part part, out bool requiresRedraw)
        {
            requiresRedraw = false;
            if (!parts.Contains(part) && part.CanStagePart())
            {
                parts.Add(part);
                requiresRedraw = GUI.windowHolder != null && GUI.SelectedActionGroup == this;
            }

            if (BuildManager.main != null)
                part.aboutToDestroy = (Action<Part>) Delegate.Combine(part.aboutToDestroy, new Action<Part>(OnPartDestroyed));
            else
                part.onPartDestroyed = (Action<Part>) Delegate.Combine(part.onPartDestroyed, new Action<Part>(OnPartDestroyed));
        }

        public void RemovePart(Part part, out bool requiresRedraw)
        {
            requiresRedraw = parts.Remove(part) && GUI.windowHolder != null & GUI.SelectedActionGroup == this;

            if (BuildManager.main != null)
                part.aboutToDestroy += (Action<Part>) Delegate.Remove(part.aboutToDestroy, new Action<Part>(OnPartDestroyed));
            else
                part.onPartDestroyed = (Action<Part>) Delegate.Remove(part.onPartDestroyed, new Action<Part>(OnPartDestroyed));
        }

        private void OnPartDestroyed(Part part)
        {
            RemovePart(part, out bool requiresRedraw);
            if (requiresRedraw)
                GUI.UpdateUI(GUI.SelectedActionGroup);
        }

        public void Activate()
        {
            Rocket.UseParts(false, parts.Select(part => (part, (PolygonData) null)).ToArray());
        }
    }

    public class ActionGroupModule : MonoBehaviour
    {
        public List<ActionGroup> actionGroups = new List<ActionGroup>();
    }
}