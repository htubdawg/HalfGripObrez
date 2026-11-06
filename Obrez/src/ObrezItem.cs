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
            // Use each distinct built-in gun sprite once; upgrade tiers that share an
            // image must not skew the result. Measure the exact rect/PPU ratio that
            // Unity UI uses, rather than mesh bounds or assumed screen/canvas pixels.
            Sprite[] nativeIcons = catalog.items.OfType<GunUpgrade>()
                .Where(g => g && g.id != Id && g.image && (g.hideFlags & HideFlags.DontSave) == 0)
                .Select(g => g.image).Distinct().ToArray();
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
                float[] nativeWidths = nativeIcons.Select(s => s.rect.width / s.pixelsPerUnit).ToArray();
                float[] nativeHeights = nativeIcons.Select(s => s.rect.height / s.pixelsPerUnit).ToArray();
                float ppu = PresentationMath.NativeIconPixelDensity(texture.width, texture.height, nativeWidths, nativeHeights);
                if (ppu > 0f)
                {
                    icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
                    icon.name = "obrez_shop_icon";
                    item.image = icon;
                    Mod.Context.Log.LogInfo($"Obrez garage icon: {nativeIcons.Length} distinct native gun references, " +
                        $"target rect/PPU={PresentationMath.MedianPositive(nativeWidths):0.####}x{PresentationMath.MedianPositive(nativeHeights):0.####}, " +
                        $"actual={texture.width / ppu:0.####}x{texture.height / ppu:0.####}");
                }
                else Mod.Context.Log.LogError("Obrez icon has no valid native sizing references; keeping the stock gun icon.");
            }
            return item;
        }
    }
}
