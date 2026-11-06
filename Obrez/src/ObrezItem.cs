using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HalfGripObrez
{
    internal static class ObrezItem
    {
        internal const string Id = "obrez.mosin";
        private static GunUpgrade item;
        private static Sprite icon;
        internal static bool IsEquipped => item != null && PlayerStats.gunUpgrade == item;
        internal static void Clear()
        {
            item = null;
            if (icon) Object.Destroy(icon);
            icon = null;
        }

        internal static GunUpgrade Create(ItemCatalog catalog)
        {
            GunUpgrade basis = catalog.items.OfType<GunUpgrade>().FirstOrDefault(g => g && g.gunType == GunType.SMG_RIFLE);
            if (!basis)
            {
                Mod.Context.Log.LogError("Cannot register Mosin Obrez: the catalog has no SMG item.");
                return null;
            }
            GunUpgrade revolver = catalog.items.OfType<GunUpgrade>().FirstOrDefault(g => g && g.gunType == GunType.REVOLVER) ?? basis;
            item = Mod.Context.Items.Clone(basis, Id);
            item.name = "Mosin Obrez";
            item.description = "Mosin Obrez\n<color=#7e7e7e>A rifle cut down to the essentials.</color>\n<color=#6fb7b4>Load individual rounds with the bolt open</color>\n<color=#6fb7b4>Push the bolt closed to chamber / 5 rounds</color>";
            item.secondaryDescription = "";
            item.tier = ItemTier.RaceSpec;
            item.cost = Mathf.Max(basis.cost, revolver.cost) * 2;
            item.disabled = false;
            item.startsLocked = false;
            item.damage = Mod.Damage.Value;
            item.recoil = revolver.recoil * Mod.Recoil.Value;
            item.inaccuracy = Mod.Inaccuracy.Value;
            item.fireRate = 1.25f;
            item.capacity = 5;
            item.magazines = Mod.Rounds.Value;
            item.projectilesPerShot = 1;
            item.projectileSpread = 0f;
            item.ammoWord = "round|rounds";
            item.muzzleFlash = revolver.muzzleFlash;
            item.shakeIntensity = revolver.shakeIntensity * 1.2f;
            item.shakeLength = revolver.shakeLength;
            item.shakeSharpness = revolver.shakeSharpness;
            item.forceHide = basis.forceHide != null ? new List<string>(basis.forceHide) : new List<string>();
            item.forceHide.Add("fire rate");
            Texture2D texture = Mod.Context.Sprites.LoadTexture("obrez_icon.png");
            if (texture)
            {
                // The garage calls Image.SetNativeSize, which uses sprite bounds/PPU.
                // The garage gets its own coarse sprite and a stable, readable UI size,
                // independent of cockpit texture detail and the catalog's SMG variant.
                float ppu = PresentationMath.FitPixelDensity(texture.width, texture.height,
                    PresentationMath.GarageWidth, PresentationMath.GarageHeight);
                icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
                icon.name = "obrez_shop_icon";
                item.image = icon;
            }
            return item;
        }
    }
}
