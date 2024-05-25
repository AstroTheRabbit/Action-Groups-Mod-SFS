using System.Reflection;
using HarmonyLib;
using SFS.Parts;
using SFS.World;
using SFS.Audio;
using SFS.Builds;
using System.Linq;

namespace ActionGroupsMod
{
    public static class Patches
    {
        public static StagingDrawer StagingDrawer => BuildManager.main != null ? BuildManager.main.buildMenus.stagingDrawer : StagingDrawer.main;
        public static bool CanStagePart(Part part)
        {
            return (bool) typeof(StagingDrawer)
                .GetMethod("CanStagePart", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new[] { part, (object) false });
        }


        [HarmonyPatch(typeof(StagingDrawer), nameof(StagingDrawer.SetSelected))]
        static class StagingDrawer_SetSelected
        {
            static void Prefix(StageUI a)
            {
                if (a != null && GUI.windowHolder != null)
                {
                    GUI.UpdateUI(null);
                }
            }
        }

        [HarmonyPatch(typeof(BuildMenus), nameof(BuildMenus.OnAreaSelect))]
        static class BuildMenus_OnAreaSelect
        {
            static bool Prefix(Part[] parts)
            {
                if (GUI.SelectedActionGroup != null)
                {
                    bool requiresRedraw = false;
                    if (parts.All((Part part) => !CanStagePart(part) || GUI.SelectedActionGroup.parts.Contains(part)))
                    {
                        foreach (Part part in parts)
                        {
                            GUI.SelectedActionGroup.RemovePart(part, out bool rr);
                            requiresRedraw |= rr;
                        }
                    }
                    else
                    {
                        foreach (Part part in parts)
                        {
                            GUI.SelectedActionGroup.AddPart(part, out bool rr);
                            requiresRedraw |= rr;
                        }
                    }
                    if (requiresRedraw)
                    {
                        GUI.UpdateUI(GUI.SelectedActionGroup);
                        SoundPlayer.main.pickupSound.Play();
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BuildMenus), nameof(BuildMenus.OnPartClick))]
        static class BuildMenus_OnPartClick
        {
            static bool Prefix(PartHit hit)
            {
                if (GUI.SelectedActionGroup != null)
                {
                    GUI.SelectedActionGroup.TogglePart(hit.part, out bool requiresRedraw);
                    if (requiresRedraw)
                    {
                        GUI.UpdateUI(GUI.SelectedActionGroup);
                        SoundPlayer.main.pickupSound.Play();
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BuildMenus), nameof(BuildMenus.OnEmptyClick))]
        static class BuildMenus_OnEmptyClick
        {
            static void Postfix()
            {
                GUI.UpdateUI(null);
            }
        }
    }
}