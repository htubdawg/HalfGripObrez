using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HalfGripObrez
{
    internal sealed class ObrezGun : MonoBehaviour
    {
        private static readonly MethodInfo SetChamber = AccessTools.PropertySetter(typeof(NAutomaticGun), "chambered");
        internal readonly BoltAction Action = new();
        internal NAutomaticGun Gun { get; private set; }
        internal ObrezPresentation Visuals { get; private set; }
        internal float WorldWidth { get; private set; }
        internal bool Ready => initialized && Visuals != null && isActiveAndEnabled;
        internal bool CanLoad => Ready && Action.CanLoad && !Ceasefire &&
            Gun.slide && Gun.slide.value >= Gun.slideBackThreshold;
        private TrackManager race;
        private bool fullAuto, lockOnEmpty, releaseOnLock, initialized, inShot, hadRound;
        private Transform slide3D, feedSprite;
        private AudioClip[] sounds;
        private bool boltPhysics, boltFrozen;
        private float boltSpring, boltPhysicsSpring, boltMomentum;
        private System.Func<Grabbable> boltRedirect;
        private bool Ceasefire => race && race.InOpeningCeasefire;
        private bool Endless => global::Gun.infiniteAmmo || global::Gun.godGun;

        internal void Initialize(NAutomaticGun gun)
        {
            Gun = gun;
            WorldWidth = ObrezPresentation.MeasureWidth(gun) * Mod.DockScale.Value;
            fullAuto = gun.fullAuto;
            lockOnEmpty = gun.slideLockOnEmpty;
            releaseOnLock = gun.slideReleaseOnMagLock;
            slide3D = gun.slide3D;
            feedSprite = gun.feedBulletSprite;
            sounds = gun.shootSounds;
            gun.fullAuto = false;
            gun.slideLockOnEmpty = false;
            gun.slideReleaseOnMagLock = false;
            gun.slide3D = null;
            gun.feedBulletSprite = null;
            if (gun.slide)
            {
                boltPhysics = gun.slide.isPhysicsControlled;
                boltFrozen = gun.slide.frozen;
                boltSpring = gun.slide.objectSpringForce;
                boltPhysicsSpring = gun.slide.physicsObjectSpringForce;
                boltMomentum = gun.slide.directSpeedKeptOnRelease;
                boltRedirect = gun.slide.grabRedirect;
                BoltControl.Configure(gun.slide);
                gun.slide.onGrabbed.AddListener(GrabBolt);
            }
            SetChamber.Invoke(gun, new object[] { false });
            initialized = true;
        }
        private void Start()
        {
            race = Object.FindFirstObjectByType<TrackManager>();
            foreach (Gun candidate in Object.FindObjectsByType<Gun>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.RequiredGunType == GunType.REVOLVER)
                { Gun.shootSounds = candidate.shootSounds; break; }
            Visuals = new ObrezPresentation(this);
        }
        private void GrabBolt()
        {
            if (Gun && Gun.slide) Gun.slide.frozen = false;
        }
        // Replaces the SMG's UpdateSlide, including its unconditional return spring.
        internal void UpdateBolt()
        {
            if (!initialized || !Gun.slide) return;
            LinearObject bolt = Gun.slide;
            // Zero springs and zero release momentum hold the last position. Freezing the
            // Grabbable interferes with the native hand's movement/pose update.
            bolt.frozen = false;
            bolt.targetValue = bolt.value;
            BoltEvent change = Action.MoveBolt(bolt.value, Gun.slideBackThreshold, Gun.slideForwardThreshold);
            if (change == BoltEvent.Opened || change == BoltEvent.Ejected)
            {
                SetChamber.Invoke(Gun, new object[] { false });
                if (change == BoltEvent.Ejected && Gun.bulletEjectEffect) Gun.bulletEjectEffect.Play();
                SoundController.PlayRandomSound2D(Gun.slidePullSounds, gameObject, 0.5f);
            }
            else if (change == BoltEvent.Closed)
            {
                SetChamber.Invoke(Gun, new object[] { Action.Chambered });
                SoundController.PlayRandomSound2D(Gun.slideReleaseSounds, gameObject, 0.5f);
            }
        }
        internal bool ReserveRound()
        {
            if (!CanLoad) return false;
            return Action.ReserveRound();
        }
        internal bool FinishRound()
        {
            bool accepted = Action.FinishRound(Ready && !Ceasefire && Gun.slide &&
                Gun.slide.value >= Gun.slideBackThreshold);
            if (accepted)
            {
                LifetimeStats.Add(LifetimeStat.Reloads);
                SoundController.PlayRandomSound2D(Gun.magLockSounds, gameObject, 0.4f);
            }
            return accepted;
        }
        internal void CancelRound() => Action.CancelRound();
        internal bool BeginShot()
        {
            if (!Ready || !Gun.isActiveAndEnabled || Ceasefire || Action.PendingRound ||
                (Gun.slide && Gun.slide.value > Gun.slideForwardThreshold) ||
                (!Endless && (!Action.Chambered || Action.Open))) return false;
            hadRound = Gun.chambered;
            inShot = true;
            return true;
        }
        internal void EndShot(bool completed)
        {
            if (!inShot) return;
            inShot = false;
            if (!completed || !hadRound || Gun.chambered) return;
            if (Action.Fire()) StartCoroutine(Lower());
        }
        private IEnumerator Lower()
        {
            yield return new WaitForSeconds(0.25f);
            if (Gun && Gun.grabbed)
            {
                ExitAim();
                if (Gun.body) Gun.body.Release();
            }
        }
        private void ExitAim() => AccessTools.Method(typeof(Gun), "ExitAimMode").Invoke(Gun, null);
        internal void Restore()
        {
            StopAllCoroutines();
            if (!initialized) return;
            initialized = false;
            if (Gun)
            {
                if (Gun.grabbed) ExitAim();
                Gun.fullAuto = fullAuto;
                Gun.slideLockOnEmpty = lockOnEmpty;
                Gun.slideReleaseOnMagLock = releaseOnLock;
                Gun.slide3D = slide3D;
                Gun.feedBulletSprite = feedSprite;
                Gun.shootSounds = sounds;
                SetChamber.Invoke(Gun, new object[] { false });
                if (Gun.slide)
                {
                    Gun.slide.onGrabbed.RemoveListener(GrabBolt);
                    Gun.slide.grabRedirect = boltRedirect;
                    Gun.slide.isPhysicsControlled = boltPhysics;
                    Gun.slide.objectSpringForce = boltSpring;
                    Gun.slide.physicsObjectSpringForce = boltPhysicsSpring;
                    Gun.slide.directSpeedKeptOnRelease = boltMomentum;
                    Gun.slide.frozen = boltFrozen;
                }
            }
            Visuals?.Restore();
            Visuals = null;
        }
        private void OnDestroy() => Restore();
    }
}
