using System.Collections.Generic;
using HalfGripTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HalfGripObrez
{
    internal sealed class ObrezPresentation
    {
        internal const int Width = 128, Height = 40, BoreRow = 12, PortColumn = 85, PortRow = 11;
        private readonly List<(SpriteRenderer renderer, Sprite original, Sprite replacement)> sprites = new();
        private readonly List<(Renderer renderer, bool enabled)> renderers = new();
        private readonly List<Object> owned = new();
        private readonly ObrezGun owner;
        private SpriteRenderer body;
        private LinearObject bolt;
        private Vector3 boltPosition;
        private Quaternion boltRotation;
        private InteriorSquareCollider boltCollider;
        private float boltAngle, boltMin, boltMax;
        private Camera interiorCamera;
        private InteriorCollider originalBoltCollider;
        private bool originalBoltColliderEnabled;
        private static readonly System.Reflection.FieldInfo CustomKey = HarmonyLib.AccessTools.Field(typeof(SpriteUpgradeChanger), "customKey");

        private static string PartKey(SpriteRenderer renderer)
        {
            SpriteUpgradeChanger changer = renderer.GetComponent<SpriteUpgradeChanger>();
            return changer ? CustomKey.GetValue(changer) as string : null;
        }

        internal static float MeasureWidth(NAutomaticGun gun)
        {
            foreach (SpriteRenderer renderer in gun.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.sprite && (PartKey(renderer) == "smg_body" || renderer.sprite.name == "baza smg_0" || renderer.sprite.name == "obrez_body"))
                    return renderer.sprite.bounds.size.x * renderer.transform.TransformVector(Vector3.right).magnitude;
            return 4f; // Only used if a different car has no recognizable stock sprite.
        }

        internal static Sprite SizedSprite(string file, SpriteRenderer renderer, float worldSize, bool height)
        {
            Texture2D texture = Mod.Context.Sprites.LoadTexture(file);
            if (!texture) return null;
            Vector3 axis = height ? Vector3.up : Vector3.right;
            float scale = renderer.transform.TransformVector(axis).magnitude;
            float ppu = (height ? texture.height : texture.width) * scale / Mathf.Max(0.001f, worldSize);
            // Per-renderer sprites: SpriteLoader caches by filename, which cannot represent differing cockpit scales.
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
            sprite.name = System.IO.Path.GetFileNameWithoutExtension(file);
            return sprite;
        }

        internal ObrezPresentation(ObrezGun obrez)
        {
            owner = obrez;
            Grabber grabber = Object.FindFirstObjectByType<Grabber>();
            interiorCamera = grabber ? grabber.viewCamera : null;
            if (!interiorCamera)
            {
                GameObject named = GameObject.Find("InteriorCamera");
                if (named) interiorCamera = named.GetComponent<Camera>();
            }
            NAutomaticGun gun = owner.Gun;
            SpriteRenderer handle = null;
            foreach (SpriteRenderer renderer in gun.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!renderer.sprite) continue;
                switch (PartKey(renderer) ?? renderer.sprite.name)
                {
                    case "smg_body":
                    case "baza smg_0":
                        body = renderer;
                        Swap(renderer, "obrez_body.png", owner.WorldWidth);
                        break;
                    case "smg_slide":
                    case "smg2_0":
                        handle = renderer;
                        Swap(renderer, "obrez_bolt.png", owner.WorldWidth * 36f / Width);
                        break;
                    case "smg_backside":
                    case "smg_slide_2":
                    case "smg_slide_bullet":
                    case "smg3 backside_0":
                    case "smg4_0":
                    case "fullbullet_0":
                        Swap(renderer, "clear.png", owner.WorldWidth / Width);
                        break;
                }
            }
            if (body && handle && gun.slide)
            {
                bolt = gun.slide;
                boltPosition = bolt.transform.localPosition;
                boltRotation = bolt.transform.localRotation;
                boltAngle = bolt.anchorAngle;
                boltMin = bolt.minLimit;
                boltMax = bolt.maxLimit;
                Vector3 target = PixelPoint(96, 18);
                Vector3 delta = Vector3.ProjectOnPlane(target - handle.bounds.center, body.transform.forward);
                Vector3 local = bolt.transform.parent.InverseTransformPoint(bolt.transform.position + delta);
                // Pull toward the butt, not along the old SMG's unscaled slide axis/travel.
                Vector3 rearward = body.transform.TransformDirection(body.flipX ? Vector3.left : Vector3.right);
                Vector3 localTravel = bolt.transform.parent.InverseTransformVector(rearward * owner.WorldWidth * 0.14f);
                InteriorMath.BuildPlaneBasis(bolt.movementPlane.normalized, out Vector3 tangent, out Vector3 bitangent);
                float x = Vector3.Dot(localTravel, tangent), y = Vector3.Dot(localTravel, bitangent);
                bolt.anchorAngle = Mathf.Atan2(y, x);
                bolt.minLimit = 0f;
                bolt.maxLimit = Mathf.Sqrt(x * x + y * y);
                bolt.ResetRestPose(local, boltRotation);
                // The stock collider is centred on the Slide object, while its art is offset
                // on a child. Put the grab area on the visible handle itself.
                originalBoltCollider = bolt.attachedCollider;
                originalBoltColliderEnabled = originalBoltCollider && originalBoltCollider.enabled;
                if (originalBoltCollider) originalBoltCollider.enabled = false;
                boltCollider = handle.gameObject.AddComponent<InteriorSquareCollider>();
                owned.Add(boltCollider);
                boltCollider.width = handle.sprite.bounds.size.x;
                boltCollider.height = handle.sprite.bounds.size.y;
                boltCollider.hitBoxExpand = handle.sprite.bounds.size.y * 0.35f;
                boltCollider.sortingLayer = originalBoltCollider ? originalBoltCollider.sortingLayer + 2 : 3;
                bolt.attachedCollider = boltCollider;
            }
            if (!body || !handle || !boltCollider || !interiorCamera)
                Mod.Context.Log.LogError("Obrez cockpit setup incomplete: body=" + (bool)body + " handle=" + (bool)handle + " collider=" + (bool)boltCollider + " camera=" + (bool)interiorCamera);
            else
                Mod.Context.Log.LogInfo($"Obrez cockpit ready: gun width={owner.WorldWidth:0.###}, bolt travel={bolt.maxLimit:0.###}, camera={interiorCamera.name}");
            BuildHeld(gun);
        }

        private Vector3 PixelPoint(float column, float row)
        {
            float ppu = body.sprite.pixelsPerUnit;
            return body.transform.TransformPoint(new Vector3((column - Width * 0.5f) / ppu * (body.flipX ? -1 : 1),
                (Height * 0.5f - row) / ppu * (body.flipY ? -1 : 1), 0));
        }
        internal Vector3 Receiver => body ? PixelPoint(PortColumn, PortRow) : owner.transform.position;
        internal Vector3 Normal => body ? body.transform.forward : Vector3.forward;
        internal Vector3 AlongBarrel => body ? body.transform.TransformDirection(body.flipX ? Vector3.right : Vector3.left) : Vector3.left;
        internal bool NearReceiver(Vector3 point)
        {
            if (!body || !interiorCamera) return false;
            Vector3 round = interiorCamera.WorldToScreenPoint(point);
            Vector3 port = interiorCamera.WorldToScreenPoint(Receiver);
            Vector3 left = interiorCamera.WorldToScreenPoint(PixelPoint(0, BoreRow));
            Vector3 right = interiorCamera.WorldToScreenPoint(PixelPoint(Width, BoreRow));
            float width = Vector2.Distance(left, right);
            return PresentationMath.NearReceiver(round.x, round.y, round.z, port.x, port.y, port.z, width);
        }
        internal bool LoadingPosition(Grabbable round, out Vector3 position)
        {
            position = default;
            if (!body || !interiorCamera) return false;
            Vector3 screen = interiorCamera.WorldToScreenPoint(Receiver);
            if (screen.z <= 0 || !round.RaycastMovementPlane(interiorCamera.ScreenPointToRay(screen), out Vector2 plane)) return false;
            position = round.PlaneToWorld(plane);
            return true;
        }

        private void Swap(SpriteRenderer renderer, string file, float worldWidth)
        {
            Sprite replacement = SizedSprite(file, renderer, worldWidth, false);
            if (!replacement) return;
            sprites.Add((renderer, renderer.sprite, replacement));
            owned.Add(replacement);
            renderer.sprite = replacement;
        }
        internal void Reapply(SpriteRenderer renderer)
        {
            foreach (var entry in sprites)
                if (entry.renderer == renderer && entry.replacement) renderer.sprite = entry.replacement;
        }
        private void BuildHeld(NAutomaticGun gun)
        {
            if (!gun.aimModel || !gun.firePoint) return;
            float front = float.MinValue, rear = float.MaxValue;
            Material basis = null;
            int layer = gun.aimModel.gameObject.layer;
            foreach (MeshFilter filter in gun.aimModel.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (!filter.sharedMesh || !renderer) continue;
                layer = renderer.gameObject.layer;
                if (!basis && renderer.sharedMaterial) basis = renderer.sharedMaterial;
                Bounds b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    float z = gun.aimModel.InverseTransformPoint(filter.transform.TransformPoint(corner)).z;
                    front = Mathf.Max(front, z);
                    rear = Mathf.Min(rear, z);
                }
                renderers.Add((renderer, renderer.enabled));
            }
            Texture2D texture = Mod.Context.Sprites.LoadTexture("obrez_held.png");
            if (!texture || !basis || front <= rear) return;
            float length = (front - rear) * 0.9f;
            Mesh mesh = VoxelMesh.Build(texture, length / Width, length * 0.045f, BoreRow, "Obrez held mesh");
            owned.Add(mesh);
            GameObject model = VoxelMesh.MakeObject("Obrez.Held", mesh, texture, basis, gun.aimModel, layer);
            owned.Add(model);
            Material material = model.GetComponent<MeshRenderer>().sharedMaterial;
            if (material) owned.Add(material);
            model.transform.localPosition = gun.aimModel.InverseTransformPoint(gun.firePoint.position);
            foreach (var original in renderers) original.renderer.enabled = false;
        }
        internal void Restore()
        {
            foreach (var original in sprites) if (original.renderer) original.renderer.sprite = original.original;
            foreach (var original in renderers) if (original.renderer) original.renderer.enabled = original.enabled;
            if (bolt)
            {
                bolt.anchorAngle = boltAngle;
                bolt.minLimit = boltMin;
                bolt.maxLimit = boltMax;
                bolt.ResetRestPose(boltPosition, boltRotation);
            }
            if (bolt) bolt.attachedCollider = originalBoltCollider;
            if (originalBoltCollider) originalBoltCollider.enabled = originalBoltColliderEnabled;
            foreach (Object resource in owned) if (resource) Object.Destroy(resource);
            sprites.Clear(); renderers.Clear(); owned.Clear();
        }
    }
}
