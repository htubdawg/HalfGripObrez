using System.Collections;
using HalfGripTools;
using UnityEngine;

namespace HalfGripObrez
{
    // Keep NMagazine.gun, the shop loadout and the base game's aim/grab wiring intact.
    internal sealed class ObrezGun : MonoBehaviour
    {
        private NAutomaticGun gun;
        private TrackManager race;
        private ObrezPresentation presentation;
        private bool fullAuto, lockOnEmpty, releaseOnLock, hadRound, spentCase, initialized;
        private Transform slide3D;
        private AudioClip[] sounds;
        internal bool InShot { get; private set; }

        internal void Initialize(NAutomaticGun source)
        {
            gun = source;
            fullAuto = gun.fullAuto;
            lockOnEmpty = gun.slideLockOnEmpty;
            releaseOnLock = gun.slideReleaseOnMagLock;
            slide3D = gun.slide3D;
            sounds = gun.shootSounds;
            gun.fullAuto = false;
            gun.slideLockOnEmpty = false;
            gun.slideReleaseOnMagLock = false;
            gun.slide3D = null; // A bolt must not animate as a self-loading slide.
            initialized = true;
        }

        private void Start()
        {
            race = Object.FindFirstObjectByType<TrackManager>();
            foreach (Gun candidate in Object.FindObjectsByType<Gun>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.RequiredGunType == GunType.REVOLVER)
                {
                    gun.shootSounds = candidate.shootSounds;
                    break;
                }
            presentation = new ObrezPresentation(gun);
            Mod.Context.Log.LogInfo("Mosin Obrez fitted; pull and release the bolt after every shot.");
        }

        internal bool BeginShot()
        {
            if (!initialized || !isActiveAndEnabled || !gun.isActiveAndEnabled ||
                (race && race.InOpeningCeasefire) ||
                (gun.slide && gun.slide.value > gun.slideForwardThreshold)) return false;
            hadRound = gun.chambered;
            InShot = true;
            return true;
        }

        internal void EndShot(bool completed)
        {
            if (!InShot) return;
            InShot = false; // Also cleared when native Shoot throws.
            if (!completed || !hadRound || gun.chambered) return;
            spentCase = true;
            StartCoroutine(Lower());
        }

        private IEnumerator Lower()
        {
            yield return new WaitForSeconds(0.25f);
            if (gun && gun.grabbed)
            {
                // Exit also restores sprite visibility and unfreezes the body before releasing it.
                ExitAim(gun);
                if (gun.body) gun.body.Release();
            }
        }

        internal void EjectCase()
        {
            if (!spentCase) return;
            spentCase = false;
            if (gun.bulletEjectEffect) gun.bulletEjectEffect.Play();
        }

        private static void ExitAim(Gun target)
        {
            HarmonyLib.AccessTools.Method(typeof(Gun), "ExitAimMode").Invoke(target, null);
        }

        internal void Restore()
        {
            StopAllCoroutines();
            InShot = false;
            if (!initialized) return;
            initialized = false;
            if (gun)
            {
                if (gun.grabbed) ExitAim(gun);
                gun.fullAuto = fullAuto;
                gun.slideLockOnEmpty = lockOnEmpty;
                gun.slideReleaseOnMagLock = releaseOnLock;
                gun.slide3D = slide3D;
                gun.shootSounds = sounds;
            }
            presentation?.Restore();
            presentation = null;
        }

        private void OnDestroy() => Restore();
    }
}
