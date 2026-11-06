using HarmonyLib;

namespace HalfGripObrez
{
    // Garage refits can reuse an already-awake SMG assembly. Restore before its sprite refresh,
    // then install after the refit, so switching back to an ordinary SMG restores its behavior.
    [HarmonyPatch(typeof(LoadoutRefit), "Swap")]
    internal static class RefitPatch
    {
        private static void Prefix(UpgradeComponent off, UpgradeComponent on, out bool __state)
        {
            __state = off is GunUpgrade || on is GunUpgrade;
            if (__state) Mod.RemoveSceneParts();
        }
        private static void Postfix(bool __state)
        {
            if (__state) Mod.InstallSceneParts();
        }
    }

    [HarmonyPatch(typeof(NAutomaticGun), "Awake")]
    internal static class InstallPatch
    {
        private static void Postfix(NAutomaticGun __instance)
        {
            if (ObrezItem.IsEquipped && __instance.RequiredGunType == GunType.SMG_RIFLE && !__instance.GetComponent<ObrezGun>())
                __instance.gameObject.AddComponent<ObrezGun>().Initialize(__instance);
        }
    }

    // Keep the game's damage, stats, raycast, networking signals, effects and ammo consumption.
    // Suppress only its immediate post-shot feed; closing the bolt still calls the same feed normally.
    [HarmonyPatch(typeof(NAutomaticGun), "Shoot")]
    internal static class ShotPatch
    {
        private static bool Prefix(NAutomaticGun __instance, out ObrezGun __state)
        {
            __state = __instance.GetComponent<ObrezGun>();
            return !__state || __state.BeginShot();
        }

        private static void Finalizer(ObrezGun __state, System.Exception __exception)
        {
            if (__state) __state.EndShot(__exception == null);
        }
    }

    [HarmonyPatch(typeof(NAutomaticGun), "TryChamber")]
    internal static class FeedPatch
    {
        private static bool Prefix(NAutomaticGun __instance, ref bool __result)
        {
            ObrezGun obrez = __instance.GetComponent<ObrezGun>();
            if (!obrez || !obrez.InShot) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(NAutomaticGun), "OnSlidePulled")]
    internal static class EjectPatch
    {
        private static void Postfix(NAutomaticGun __instance)
        {
            __instance.GetComponent<ObrezGun>()?.EjectCase();
        }
    }
}
