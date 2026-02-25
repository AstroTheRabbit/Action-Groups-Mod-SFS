using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SFS.Audio;
using SFS.Builds;
using SFS.Input;
using SFS.Parts;
using SFS.UI;
using SFS.World;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace ActionGroupsMod
{
    public static class PrivateMethodExtensions
    {
        private static readonly MethodInfo method_CanStagePart = typeof(StagingDrawer).GetMethod("CanStagePart", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo method_IsKeyDown = typeof(KeybindingsPC.Key).GetMethod("SFS.Input.I_Key.IsKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo method_IsKeyUp = typeof(KeybindingsPC.Key).GetMethod("SFS.Input.I_Key.IsKeyUp", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool CanStagePart(this Part part) => (bool) method_CanStagePart.Invoke(null, new object[] { part, false });
        public static bool IsKeyDown(this KeybindingsPC.Key key) => (bool) method_IsKeyDown.Invoke(key, null);
        public static bool IsKeyUp(this KeybindingsPC.Key key) => (bool) method_IsKeyUp.Invoke(key, null);
    }

    public static class Patches
    {
        public static StagingDrawer StagingDrawer => BuildManager.main != null ? BuildManager.main.buildMenus.stagingDrawer : StagingDrawer.main;
        
        [HarmonyPatch(typeof(StagingDrawer), nameof(StagingDrawer.SetSelected))]
        public static class StagingDrawer_SetSelected
        {
            public static void Prefix(StageUI a)
            {
                if (a != null && GUI.windowHolder != null)
                {
                    GUI.UpdateUI(null);
                }
            }
        }

        [HarmonyPatch(typeof(BuildMenus), nameof(BuildMenus.OnAreaSelect))]
        public static class BuildMenus_OnAreaSelect
        {
            public static bool Prefix(Part[] parts)
            {
                if (GUI.SelectedActionGroup != null)
                {
                    bool requiresRedraw = false;
                    if (parts.All(part => !part.CanStagePart() || GUI.SelectedActionGroup.parts.Contains(part)))
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
        public static class BuildMenus_OnPartClick
        {
            public static bool Prefix(PartHit hit)
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
        public static class BuildMenus_OnEmptyClick
        {
            public static void Postfix()
            {
                GUI.UpdateUI(null);
            }
        }

        [HarmonyPatch(typeof(Rocket), "ClickPart")]
        public static class Rocket_ClickPart
        {
            public static bool Prefix(Rocket __instance, TouchPosition position, ref bool __result)
            {
                if (GUI.SelectedActionGroup != null)
                {
                    if (Part_Utility.RaycastParts(__instance.partHolder.GetArray(), position.World(0f), Mathf.Clamp((float)PlayerController.main.cameraDistance * 0.03f, 0f, 2f), out PartHit hit))
                    {
                        __result = true;
                        if (!hit.part.Rocket.hasControl)
                        {
                            MsgDrawer.main.Log("Rocket has no control, cannot use action groups");
                        }
                        else
                        {
                            GUI.SelectedActionGroup.TogglePart(hit.part, out bool requiresRedraw);
                            if (requiresRedraw)
                            {
                                GUI.UpdateUI(GUI.SelectedActionGroup);
                                SoundPlayer.main.pickupSound.Play();
                            }
                        }
                    }
                    else
                    {
                        __result = false;
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Screen_Game), nameof(Screen_Game.ProcessInput))]
        public static class Screen_Game_ProcessInput
        {
            public static bool Prefix()
            {
                return !GUI.editingText;
            }

            public static void Postfix(Screen_Game __instance)
            {
                if (GameManager.main?.world_Input == __instance || GameManager.main?.map_Input == __instance)
                {
                    List<ActionGroup> actionGroups = ActionGroupManager.GetCurrentActionGroups();
                    if (actionGroups != null)
                    {
                        Rocket rocket = PlayerController.main.player.Value as Rocket;
                        foreach (ActionGroup ag in actionGroups)
                        {
                            if (ag.key == null)
                                continue;

                            bool keyDown = ag.key.IsKeyDown();
                            bool keyUp = ag.key.IsKeyUp();

                            if (ag.holdToActivate)
                            {
                                if (ag.holdToggled && (keyUp || keyDown))
                                {
                                    if (CanToggle(rocket))
                                    {
                                        ag.holdToggled = false;
                                        ag.Activate();
                                    }
                                }
                                else if (!ag.holdToggled && keyDown)
                                {
                                    if (CanToggle(rocket))
                                    {
                                        ag.holdToggled = true;
                                        ag.Activate();
                                    }
                                }
                            }
                            else if (keyDown)
                            {
                                if (CanToggle(rocket))
                                    ag.Activate();
                            }
                        }
                    }
                }

                bool CanToggle(Rocket rocket)
                {
                    if (!rocket.hasControl)
                    {
                        MsgDrawer.main.Log("Rocket has no control, cannot use action groups");
                        return false;
                    }
                    else if (!rocket.physics.PhysicsMode)
                    {
                        MsgDrawer.main.Log("Cannot use action groups while time warping");
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RocketManager), nameof(RocketManager.MergeRockets))]
        public class RocketManager_MergeRockets
        {
            public static void Prefix(Rocket rocket_A, Rocket rocket_B)
            {
                ActionGroupModule agm_a = rocket_A.GetOrAddComponent<ActionGroupModule>();
                ActionGroupModule agm_b = rocket_B.GetOrAddComponent<ActionGroupModule>();

                foreach (ActionGroup ag_b in agm_b.actionGroups)
                {
                    if (agm_a.actionGroups.Find(ag => ag.name == ag_b.name) is ActionGroup ag_a)
                    {
                        ag_a.parts.AddRange(ag_b.parts);
                    }
                    else
                    {
                        agm_a.actionGroups.Add(ag_b);
                    }
                }

                GUI.UpdateUI(GUI.SelectedActionGroup);
            }
        }

        [HarmonyPatch(typeof(RocketManager), nameof(RocketManager.CreateRocket_Child))]
        public static class RocketManager_CreateRocket_Child
        {
            public static void Postfix(Rocket parentRocket, ref Rocket __result)
            {
                ActionGroupModule agm_parent = parentRocket.GetOrAddComponent<ActionGroupModule>();
                ActionGroupModule agm_child = __result.GetOrAddComponent<ActionGroupModule>();

                List<ActionGroup> toDestroy = new List<ActionGroup>();
                foreach (ActionGroup ag_parent in agm_parent.actionGroups)
                {
                    List<Part> toMove = ag_parent.parts.Where(__result.partHolder.ContainsPart).ToList();
                    if (toMove.Count > 0)
                    {
                        ActionGroup ag_child = new ActionGroup
                        {
                            name = ag_parent.name,
                            key = ag_parent.key,
                            holdToActivate = ag_parent.holdToActivate
                        };
                        foreach (Part part in toMove)
                        {
                            ag_parent.RemovePart(part, out _);
                            ag_child.AddPart(part, out _);
                        }
                        agm_child.actionGroups.Add(ag_child);
                        
                        if (ag_parent.parts.Count == 0)
                        {
                            toDestroy.Add(ag_parent);
                        }
                    }
                }
                agm_parent.actionGroups.RemoveRange(toDestroy);
            }
        }

        [HarmonyPatch(typeof(BuildState), "Clear")]
        public class BuildState_Clear
        {
            public static void Postfix()
            {
                ActionGroupManager.buildActionGroups.Clear();
                GUI.UpdateUI(null);
            }
        }
    }
}