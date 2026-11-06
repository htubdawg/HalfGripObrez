using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HalfGripObrez
{
    // Attached before the rack clones its ammo. Start runs on each clone so every pack owns its restoration state.
    [HarmonyPatch(typeof(NMagazine), "Awake")]
    internal static class AmmoPatch
    {
        private static void Postfix(NMagazine __instance)
        {
            if (ObrezItem.IsEquipped && __instance.gun && __instance.gun.RequiredGunType == GunType.SMG_RIFLE && !__instance.GetComponent<ObrezAmmo>())
                __instance.gameObject.AddComponent<ObrezAmmo>();
        }
    }

    internal sealed class ObrezAmmo : MonoBehaviour
    {
        private readonly List<(SpriteRenderer renderer, Sprite sprite)> originals = new();
        private void Start()
        {
            NMagazine magazine = GetComponent<NMagazine>();
            Replace(magazine.loadedSprite, "obrez_pack.png");
            Replace(magazine.emptySprite, "obrez_pack_empty.png");
        }

        private void Replace(SpriteRenderer renderer, string file)
        {
            if (!renderer || !renderer.sprite) return;
            Sprite replacement = Mod.Context.Sprites.LoadSprite(file, renderer.sprite,
                20f / renderer.sprite.bounds.size.y);
            if (!replacement) return;
            originals.Add((renderer, renderer.sprite));
            renderer.sprite = replacement;
        }

        internal void Restore()
        {
            foreach (var original in originals) if (original.renderer) original.renderer.sprite = original.sprite;
            originals.Clear();
        }

        private void OnDestroy() => Restore();
    }
}
