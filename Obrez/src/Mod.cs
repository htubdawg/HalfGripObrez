using BepInEx.Configuration;
using HalfGripTools;
using UnityEngine;

namespace HalfGripObrez
{
    public sealed class Mod : HalfGripMod
    {
        internal static ModContext Context;
        internal static ConfigEntry<float> Damage, Inaccuracy, Recoil;
        internal static ConfigEntry<int> Packs;

        public override void Load(ModContext ctx)
        {
            Context = ctx;
            Damage = ctx.Config.Bind("Obrez", "Damage", 65f,
                new ConfigDescription("Damage per round. Applied when the catalog is built; restart after changing.", new AcceptableValueRange<float>(1f, 200f)));
            Inaccuracy = ctx.Config.Bind("Obrez", "Inaccuracy", 1.5f,
                new ConfigDescription("Spread in degrees; the short barrel trades accuracy for size.", new AcceptableValueRange<float>(0f, 15f)));
            Recoil = ctx.Config.Bind("Obrez", "RecoilMultiplier", 1.4f,
                new ConfigDescription("Recoil relative to the revolver.", new AcceptableValueRange<float>(0.1f, 5f)));
            Packs = ctx.Config.Bind("Obrez", "LoadingPacks", 3,
                new ConfigDescription("Five-round packs provided at race start.", new AcceptableValueRange<int>(1, 20)));
            ctx.Items.Add(ObrezItem.Id, ObrezItem.Create);
            ctx.PatchAll();
            ctx.Log.LogInfo("Mosin Obrez 0.1.0 loaded: five rounds, manual bolt, no scope.");
        }

        public override void Unload()
        {
            RemoveSceneParts();
            ObrezItem.Clear();
            Context = null;
        }

        internal static void RemoveSceneParts()
        {
            foreach (ObrezAmmo ammo in Object.FindObjectsByType<ObrezAmmo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                ammo.Restore();
                Object.DestroyImmediate(ammo);
            }
            foreach (ObrezGun gun in Object.FindObjectsByType<ObrezGun>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                gun.Restore();
                Object.DestroyImmediate(gun);
            }
        }

        internal static void InstallSceneParts()
        {
            if (!ObrezItem.IsEquipped) return;
            foreach (NAutomaticGun gun in Object.FindObjectsByType<NAutomaticGun>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (gun.RequiredGunType == GunType.SMG_RIFLE && !gun.GetComponent<ObrezGun>())
                    gun.gameObject.AddComponent<ObrezGun>().Initialize(gun);
            foreach (NMagazine magazine in Object.FindObjectsByType<NMagazine>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (magazine.gun && magazine.gun.RequiredGunType == GunType.SMG_RIFLE && !magazine.GetComponent<ObrezAmmo>())
                    magazine.gameObject.AddComponent<ObrezAmmo>();
        }
    }
}
