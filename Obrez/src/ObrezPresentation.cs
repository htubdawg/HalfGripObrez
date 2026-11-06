using System.Collections.Generic;
using HalfGripTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HalfGripObrez
{
    internal sealed class ObrezPresentation
    {
        private readonly List<(SpriteRenderer renderer, Sprite sprite)> sprites = new();
        private readonly List<(Renderer renderer, bool enabled)> renderers = new();
        private readonly List<Object> owned = new();
        private LinearObject bolt;
        private Vector3 boltPosition;
        private Quaternion boltRotation;
        private InteriorSquareCollider boltCollider;
        private float boltWidth, boltHeight;

        internal ObrezPresentation(NAutomaticGun gun)
        {
            SpriteRenderer body = null, handle = null;
            foreach (SpriteRenderer renderer in gun.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!renderer.sprite) continue;
                switch (renderer.sprite.name)
                {
                    case "baza smg_0":
                        body = renderer;
                        Swap(renderer, "obrez_body.png", 96f / (renderer.sprite.bounds.size.x * 0.85f));
                        break;
                    case "smg2_0":
                        handle = renderer;
                        Swap(renderer, "obrez_bolt.png", 0);
                        break;
                    case "smg3 backside_0":
                    case "smg4_0":
                    case "fullbullet_0":
                        Swap(renderer, "clear.png", 0);
                        break;
                }
            }
            if (body && handle && gun.slide)
            {
                bolt = gun.slide;
                boltPosition = bolt.transform.localPosition;
                boltRotation = bolt.transform.localRotation;
                // Body artwork: bolt handle at column 65, row 11, measured from top left.
                float ppu = body.sprite.pixelsPerUnit;
                Vector3 receiver = body.transform.TransformPoint(new Vector3(
                    (65 - 48) / ppu * (body.flipX ? -1 : 1),
                    (16 - 11) / ppu * (body.flipY ? -1 : 1), 0));
                Vector3 delta = Vector3.ProjectOnPlane(receiver - handle.bounds.center, body.transform.forward);
                Vector3 local = bolt.transform.parent.InverseTransformPoint(bolt.transform.position + delta);
                bolt.ResetRestPose(local, boltRotation);
                boltCollider = bolt.GetComponent<InteriorSquareCollider>();
                if (boltCollider)
                {
                    boltWidth = boltCollider.width;
                    boltHeight = boltCollider.height;
                    boltCollider.width = Mathf.Max(1f, handle.sprite.bounds.size.x);
                    boltCollider.height = Mathf.Max(1f, handle.sprite.bounds.size.y);
                }
            }
            BuildHeld(gun);
        }

        private void Swap(SpriteRenderer renderer, string file, float ppu)
        {
            Sprite replacement = Mod.Context.Sprites.LoadSprite(file, renderer.sprite, ppu);
            if (!replacement) return;
            sprites.Add((renderer, renderer.sprite));
            renderer.sprite = replacement;
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
            if (!texture || !basis || front <= rear)
            {
                Mod.Context.Log.LogWarning("No compatible held-model geometry/material; retaining the stock held model.");
                return;
            }
            Mesh mesh = VoxelMesh.Build(texture, (front - rear) * 0.7f / 96f, (front - rear) * 0.06f, 10f, "Obrez held mesh");
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
            foreach (var original in sprites) if (original.renderer) original.renderer.sprite = original.sprite;
            foreach (var original in renderers) if (original.renderer) original.renderer.enabled = original.enabled;
            if (bolt) bolt.ResetRestPose(boltPosition, boltRotation);
            if (boltCollider) { boltCollider.width = boltWidth; boltCollider.height = boltHeight; }
            foreach (Object resource in owned) if (resource) Object.Destroy(resource);
            sprites.Clear();
            renderers.Clear();
            owned.Clear();
        }
    }
}
