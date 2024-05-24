using HarmonyLib;
using SFS.Builds;
using SFS.World;

namespace ActionGroupsMod
{
    public static class Patches
    {
        public static StagingDrawer StagingDrawer => BuildManager.main != null ? BuildManager.main.buildMenus.stagingDrawer : StagingDrawer.main;

        [HarmonyPatch(typeof(StagingDrawer), nameof(StagingDrawer.SetSelected))]
        static class StagingDrawer_SetSelected
        {
            static void Prefix(StageUI a)
            {
                if (a != null && GUI.windowHolder != null)
                {
                    GUI.RedrawUI(null);
                }
            }
        }
    }
}