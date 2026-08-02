using HarmonyLib;

namespace ImageScreens
{
    // Harmony patch data for the WorldManager class.
    [HarmonyPatch(typeof(WorldManager))]
    internal static class PatchWorldManager
    {
        // Called after WorldManager.ClearWorld.
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WorldManager.ClearWorld))]
        private static void AfterClearWorld()
        {
            ImageScreens.Instance.OnClearWorld();
        }
    }
}
