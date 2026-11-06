using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using HalfGripTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HalfGripObrez
{
    internal static class ObrezAmmo
    {
        internal static void ConvertRack(MagazineManager rack, bool initial = false)
        {
            bool converted = false;
            foreach (NMagazine magazine in rack.GetComponentsInChildren<NMagazine>(true))
                if (magazine.gun && magazine.gun.RequiredGunType == GunType.SMG_RIFLE)
                { Convert(magazine); converted = true; }
            if (converted && !rack.GetComponent<ObrezRack>())
            {
                ObrezRack saved = rack.gameObject.AddComponent<ObrezRack>();
                saved.OriginalEntities = rack.entitiesPerMagazine;
                rack.entitiesPerMagazine = 1;
            }
            if (converted && initial) BuildStartingSupply(rack);
        }

        private static void BuildStartingSupply(MagazineManager rack)
        {
            // Build actual loose-round entities before the native rack records its slots.
            // The SMG rack can be a sibling of the gun, so its Owner()/FitAuthoredCount
            // cannot reliably apply the item's magazine count for us.
            var rounds = new List<ObrezRound>(rack.GetComponentsInChildren<ObrezRound>(true));
            if (rounds.Count == 0) return;
            int count = Mod.Rounds.Value;
            Vector3 start = rounds[0].transform.position;
            Vector3 end = rounds[rounds.Count - 1].transform.position;
            Vector3 direction = end - start;
            Vector3 centre = (start + end) * 0.5f;
            float width = ObrezPresentation.MeasureWidth(rounds[0].SourceGun) * Mod.DockScale.Value;
            float span = Mathf.Max(direction.magnitude, width * 0.10f * (count - 1));
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : rack.transform.right;
            while (rounds.Count > count)
            {
                ObrezRound extra = rounds[rounds.Count - 1];
                rounds.RemoveAt(rounds.Count - 1);
                Object.DestroyImmediate(extra.gameObject);
            }
            while (rounds.Count < count)
            {
                GameObject made = Object.Instantiate(rounds[0].gameObject, rounds[0].transform.parent);
                rounds.Add(made.GetComponent<ObrezRound>());
            }
            for (int i = 0; i < rounds.Count; i++)
            {
                Transform t = rounds[i].transform;
                t.position = centre + direction * (span * ((float)i / (count - 1) - 0.5f));
                rounds[i].ResetRestPose(t.localPosition, t.localRotation);
            }
            Mod.Context.Log.LogInfo("Obrez starting supply: " + rounds.Count + " individual rounds (no extra magazines).");
        }

        internal static void Convert(NMagazine magazine)
        {
            if (magazine.seated && magazine.gun) magazine.gun.OnMagazineRemoved(magazine);
            magazine.Release();
            ObrezRound round = magazine.gameObject.AddComponent<ObrezRound>();
            CopyBase(magazine, round);
            round.SourceGun = magazine.gun;
            round.Loaded = magazine.loadedSprite;
            round.Empty = magazine.emptySprite;
            round.OriginalLoaded = round.Loaded ? round.Loaded.sprite : null;
            round.OriginalEmpty = round.Empty ? round.Empty.sprite : null;
            round.InsertPoint = magazine.insertPoint;
            round.AlignPoint = magazine.alignPoint;
            round.InsertSounds = magazine.insertSounds;
            round.AlignDuration = magazine.alignDuration;
            round.InsertDuration = magazine.insertDuration;
            round.OriginalCapacity = magazine.capacity;
            round.OriginalBullets = magazine.bullets;
            if (round.Loaded) round.OriginalArtPosition = round.Loaded.transform.localPosition;
            if (round.attachedCollider) round.OriginalHitExpand = round.attachedCollider.hitBoxExpand;
            if (round.attachedCollider is InteriorSquareCollider box)
            { round.OriginalWidth = box.width; round.OriginalHeight = box.height; }
            else if (round.attachedCollider is InteriorCircleCollider circle) round.OriginalRadius = circle.radius;
            else if (round.attachedCollider is InteriorCapsuleCollider capsule)
            { round.OriginalRadius = capsule.radius; round.OriginalOtherPoint = capsule.otherPoint; }
            // Refit conversion may happen after a rack has already adopted its magazines.
            ReplaceRackReferences(magazine, round);
            Object.DestroyImmediate(magazine);
        }

        internal static void CopyBase(object from, object to)
        {
            GunConversion.CopyFields(typeof(Grabbable), from, to);
            GunConversion.CopyFields(typeof(FallingObject), from, to);
            GunConversion.CopyFields(typeof(NAmmo), from, to);
        }

        private static void ReplaceRackReferences(NAmmo before, NAmmo after)
        {
            foreach (MagazineManager rack in Object.FindObjectsByType<MagazineManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (AccessTools.Field(typeof(MagazineManager), "items").GetValue(rack) is IList items)
                    for (int i = 0; i < items.Count; i++) if (ReferenceEquals(items[i], before)) items[i] = after;
                foreach (string listName in new[] { "slots", "returning" })
                    if (AccessTools.Field(typeof(MagazineManager), listName).GetValue(rack) is IList records)
                        foreach (object record in records)
                        {
                            var field = AccessTools.Field(record.GetType(), listName == "slots" ? "occupant" : "item");
                            if (ReferenceEquals(field.GetValue(record), before)) field.SetValue(record, after);
                        }
            }
        }

        internal static void RestoreAll()
        {
            foreach (ObrezRound round in Object.FindObjectsByType<ObrezRound>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                round.RestoreArt();
                NMagazine magazine = round.gameObject.AddComponent<NMagazine>();
                CopyBase(round, magazine);
                magazine.gun = round.SourceGun;
                magazine.loadedSprite = round.Loaded;
                magazine.emptySprite = round.Empty;
                magazine.insertPoint = round.InsertPoint;
                magazine.alignPoint = round.AlignPoint;
                magazine.insertSounds = round.InsertSounds;
                magazine.alignDuration = round.AlignDuration;
                magazine.insertDuration = round.InsertDuration;
                magazine.capacity = round.OriginalCapacity;
                magazine.bullets = round.OriginalBullets;
                magazine.frozen = false;
                magazine.ResetRestPose(round.transform.localPosition, round.transform.localRotation);
                ReplaceRackReferences(round, magazine);
                Object.DestroyImmediate(round);
            }
            foreach (ObrezRack saved in Object.FindObjectsByType<ObrezRack>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                saved.GetComponent<MagazineManager>().entitiesPerMagazine = saved.OriginalEntities;
                Object.DestroyImmediate(saved);
            }
        }
    }

    internal sealed class ObrezRack : MonoBehaviour
    {
        public int OriginalEntities;
    }

    internal sealed class ObrezRound : NAmmo
    {
        // Serialized so the native rack's inactive runtime template and future grants carry the restoration data.
        public NAutomaticGun SourceGun;
        public SpriteRenderer Loaded, Empty;
        public Sprite OriginalLoaded, OriginalEmpty;
        public Transform InsertPoint, AlignPoint;
        public AudioClip[] InsertSounds;
        public float AlignDuration, InsertDuration, OriginalWidth, OriginalHeight, OriginalRadius, OriginalHitExpand;
        public int OriginalCapacity, OriginalBullets;
        public Vector3 OriginalArtPosition;
        public Vector2 OriginalOtherPoint;
        private Sprite art;
        private ObrezGun target;
        private bool inserting, consumed;
        private float elapsed;
        private Vector3 fromPosition;
        private Quaternion fromRotation;
        public override bool busy => inserting || consumed;
        public override bool spent => consumed;
        protected override void Awake() { } // Setup/clone fields must be in place before plane initialization.
        private void Start()
        {
            base.Awake();
            if (!SourceGun || !Loaded) return;
            ObrezGun gun = SourceGun.GetComponent<ObrezGun>();
            float width = gun ? gun.WorldWidth : ObrezPresentation.MeasureWidth(SourceGun) * Mod.DockScale.Value;
            // The reference Obrez is short: a rifle cartridge is substantial beside it.
            // 0.17 also keeps individual rounds readable in the cockpit (42% taller than 0.2.1).
            float height = width * 0.17f;
            Loaded.transform.localPosition = Vector3.zero;
            art = ObrezPresentation.SizedSprite("obrez_round.png", Loaded, height, true);
            if (art) Loaded.sprite = art;
            Loaded.enabled = true;
            if (Empty) Empty.enabled = false;
            if (attachedCollider)
                attachedCollider.hitBoxExpand = height * 0.08f / attachedCollider.transform.TransformVector(Vector3.up).magnitude;
            if (attachedCollider is InteriorSquareCollider box)
            {
                box.height = height / box.transform.TransformVector(Vector3.up).magnitude;
                box.width = height * 0.32f / box.transform.TransformVector(Vector3.right).magnitude;
            }
            else if (attachedCollider is InteriorCircleCollider circle)
                circle.radius = height * 0.35f / circle.transform.TransformVector(Vector3.up).magnitude;
            else if (attachedCollider is InteriorCapsuleCollider capsule)
            {
                capsule.radius = height * 0.45f / capsule.transform.TransformVector(Vector3.up).magnitude;
                capsule.otherPoint = Vector2.zero;
            }
            ResetRestPose(transform.localPosition, transform.localRotation);
        }
        protected override void Update()
        {
            if (consumed) return;
            if (inserting) { StepInsert(); return; }
            base.Update();
            if (FellOutOfTheWorld() || !SourceGun || !IsGrabbed || stuck || !Loaded) return;
            ObrezGun gun = SourceGun.GetComponent<ObrezGun>();
            if (!gun || !gun.Ready || !gun.Visuals.NearReceiver(Loaded.bounds.center) || !gun.ReserveRound()) return;
            target = gun;
            inserting = true;
            elapsed = 0;
            fromPosition = transform.position;
            fromRotation = transform.rotation;
            Release();
            frozen = true;
        }
        private void StepInsert()
        {
            if (!target || !target.Ready) { Cancel(); return; }
            elapsed += InteriorMath.DeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.18f));
            if (!target.Visuals.LoadingPosition(this, out Vector3 receiver)) { Cancel(); return; }
            Quaternion rotation = Quaternion.FromToRotation(fromRotation * Vector3.up, target.Visuals.AlongBarrel) * fromRotation;
            transform.position = Vector3.Lerp(fromPosition, receiver, t);
            transform.rotation = Quaternion.Slerp(fromRotation, rotation, t);
            if (t < 1f) return;
            if (!target.FinishRound()) { Cancel(); return; }
            target = null;
            consumed = true;
            canBeGrabbed = false;
            Loaded.enabled = false;
            MagazineManager rack = GetComponentInParent<MagazineManager>();
            if (rack) rack.Take(this);
            else Object.Destroy(gameObject);
        }
        private void Cancel()
        {
            if (target) target.CancelRound();
            target = null;
            inserting = false;
            frozen = false;
            AdoptPoseFromTransform();
        }
        internal void Reapply(SpriteRenderer renderer)
        {
            if (renderer == Loaded && art) renderer.sprite = art;
            if (renderer == Empty) renderer.enabled = false;
        }
        internal void RestoreArt()
        {
            Cancel();
            if (Loaded) { Loaded.sprite = OriginalLoaded; Loaded.transform.localPosition = OriginalArtPosition; Loaded.enabled = true; }
            if (Empty) { Empty.sprite = OriginalEmpty; Empty.enabled = false; }
            if (attachedCollider) attachedCollider.hitBoxExpand = OriginalHitExpand;
            if (attachedCollider is InteriorSquareCollider box) { box.width = OriginalWidth; box.height = OriginalHeight; }
            else if (attachedCollider is InteriorCircleCollider circle) circle.radius = OriginalRadius;
            else if (attachedCollider is InteriorCapsuleCollider capsule)
            { capsule.radius = OriginalRadius; capsule.otherPoint = OriginalOtherPoint; }
            if (art) Object.Destroy(art);
            art = null;
        }
        private void OnDestroy()
        {
            if (target) target.CancelRound();
            if (art) Object.Destroy(art);
        }
    }
}
