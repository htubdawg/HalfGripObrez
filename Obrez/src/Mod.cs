using BepInEx.Configuration;
using HalfGripTools;
using UnityEngine;

namespace HalfGripObrez
{
    public sealed class Mod : HalfGripMod
    {
        internal static ModContext Context;
        internal static ConfigEntry<float> Damage, Inaccuracy, Recoil, DockScale;
        internal static ConfigEntry<int> Rounds;

        public override void Load(ModContext ctx)
        {
            Context = ctx;
            Damage = ctx.Config.Bind("Obrez", "Damage", 65f,
                new ConfigDescription("Damage per round. Applied when the catalog is built; restart after changing.", new AcceptableValueRange<float>(1f, 200f)));
            Inaccuracy = ctx.Config.Bind("Obrez", "Inaccuracy", 1.5f,
                new ConfigDescription("Spread in degrees; the short barrel trades accuracy for size.", new AcceptableValueRange<float>(0f, 15f)));
            Recoil = ctx.Config.Bind("Obrez", "RecoilMultiplier", 1.4f,
                new ConfigDescription("Recoil relative to the revolver.", new AcceptableValueRange<float>(0.1f, 5f)));
            Rounds = ctx.Config.Bind("Obrez", "LooseRounds", 5,
                new ConfigDescription("Total individual cartridges supplied at race start (four or five).", new AcceptableValueRange<int>(4, 5)));
            // Existing 0.2 configs contain 15; migrate them instead of retaining the broken ammo balance.
            Rounds.Value = Mathf.Clamp(Rounds.Value, 4, 5);
            DockScale = ctx.Config.Bind("Obrez", "DockScale", 1f,
                new ConfigDescription("Docked gun length relative to the stock SMG. Cartridges and bolt use the same physical scale.", new AcceptableValueRange<float>(0.5f, 1.5f)));
            ctx.Items.Add(ObrezItem.Id, ObrezItem.Create);
            ctx.PatchAll();
            ctx.Log.LogInfo("Mosin Obrez 0.2.2 loaded: five-shot supply and manual bolt.");
        }

        public override void Unload()
        {
            RemoveSceneParts();
            ObrezItem.Clear();
            Context = null;
        }

        internal static void RemoveSceneParts()
        {
            ObrezAmmo.RestoreAll();
            foreach (ObrezGun gun in Object.FindObjectsByType<ObrezGun>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                gun.Restore();
                Object.DestroyImmediate(gun);
            }
        }

        internal static void InstallSceneParts()
        {
            if (!ObrezItem.IsEquipped)
            {
                foreach (MagazineManager rack in Object.FindObjectsByType<MagazineManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (rack.GetComponentInParent<NAutomaticGun>(true))
                        HarmonyLib.AccessTools.Method(typeof(MagazineManager), "FitAuthoredCount").Invoke(rack, null);
                return;
            }
            foreach (NAutomaticGun gun in Object.FindObjectsByType<NAutomaticGun>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (gun.RequiredGunType == GunType.SMG_RIFLE && !gun.GetComponent<ObrezGun>())
                    gun.gameObject.AddComponent<ObrezGun>().Initialize(gun);
            foreach (MagazineManager rack in Object.FindObjectsByType<MagazineManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                ObrezAmmo.ConvertRack(rack);
                if (rack.GetComponent<ObrezRack>())
                    HarmonyLib.AccessTools.Method(typeof(MagazineManager), "FitAuthoredCount").Invoke(rack, null);
            }
        }
    }
}
