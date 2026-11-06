using HarmonyLib;

namespace HalfGripObrez
{
    [HarmonyPatch(typeof(Gun), "ExitAimMode")]
    internal static class LoweredAmmoVisibilityPatch
    {
        private static void Postfix(Gun __instance)
        {
            if (!__instance.GetComponent<ObrezGun>()) return;
            foreach (ObrezRound round in UnityEngine.Object.FindObjectsByType<ObrezRound>(
                UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
                if (round.SourceGun == __instance) round.RefreshVisibility();
        }
    }
    // Any equipment refit refreshes all native sprites, even when the gun item did not change.
    [HarmonyPatch(typeof(SpriteUpgradeChanger), "Refresh")]
    internal static class SpriteRefreshPatch
    {
        private static void Postfix(SpriteUpgradeChanger __instance)
        {
            if (!ObrezItem.IsEquipped) return;
            var renderer = __instance.GetComponent<UnityEngine.SpriteRenderer>();
            if (!renderer) return;
            __instance.GetComponentInParent<ObrezGun>(true)?.Visuals?.Reapply(renderer);
            __instance.GetComponentInParent<ObrezRound>(true)?.Reapply(renderer);
        }
    }
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
    [HarmonyPatch(typeof(MagazineManager), "Awake")]
    internal static class RackPatch
    {
        private static void Prefix(MagazineManager __instance)
        {
            if (ObrezItem.IsEquipped) ObrezAmmo.ConvertRack(__instance, true);
        }
    }
    // Four/five shots total from the initial supply. Native extra-magazine upgrades,
    // kills, pickups and refill effects must not multiply it into stacks of ammunition.
    [HarmonyPatch(typeof(MagazineManager), "SpawnEntities")]
    internal static class SupplyPatch
    {
        private static bool Prefix(MagazineManager __instance, ref int __result)
        {
            if (!__instance.GetComponent<ObrezRack>()) return true;
            __result = 0;
            return false;
        }
    }
    [HarmonyPatch(typeof(NAutomaticGun), "UpdateSlide")]
    internal static class BoltPatch
    {
        private static bool Prefix(NAutomaticGun __instance)
        {
            ObrezGun obrez = __instance.GetComponent<ObrezGun>();
            if (!obrez) return true;
            obrez.UpdateBolt();
            return false;
        }
    }
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
            if (!__instance.GetComponent<ObrezGun>()) return true;
            __result = false;
            return false;
        }
    }
    [HarmonyPatch(typeof(NAutomaticGun), "ShotsLeft")]
    internal static class AmmoCountPatch
    {
        private static bool Prefix(NAutomaticGun __instance, ref int __result)
        {
            ObrezGun obrez = __instance.GetComponent<ObrezGun>();
            if (!obrez) return true;
            __result = obrez.Action.LiveRounds;
            return false;
        }
    }
    [HarmonyPatch(typeof(NAutomaticGun), "RegisterTutorial")]
    internal static class TutorialPatch
    {
        private static bool Prefix(NAutomaticGun __instance) => !__instance.GetComponent<ObrezGun>();
    }
}
