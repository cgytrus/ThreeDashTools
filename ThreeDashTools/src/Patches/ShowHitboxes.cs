using System;
using System.Collections.Generic;

using HarmonyLib;

using MonoMod.RuntimeDetour;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;

using Gizmos = Popcron.Gizmos;

namespace ThreeDashTools.Patches;

// ReSharper disable once ClassNeverInstantiated.Global
public class ShowHitboxes : ConfigurablePatch {
    public ShowHitboxes() : base(Plugin.instance!.Config, "Player", null, false, "") => instance = this;

    internal static ShowHitboxes? instance { get; private set; }

    private static readonly List<GameObject> toRemove = new();
    private static readonly Dictionary<GameObject, Collider[]> hitboxes = new();

    private enum ColliderType { Hazard, Trigger, Wall, Unknown }

    public override void Apply() {
        On.PlayerScript.Update += (orig, self) => {
            orig(self);
            if(!enabled)
                return;
            RemoveOldObjectsFromList();

            Transform transform = self.transform;
            DrawHitboxes(transform.parent.parent.gameObject, Color.red);
            Vector3 groundCheckPos =
                Mathf.Sign(self.grav) < 0f ? self.groundCheck.position : self.ceilingCheck.position;
            Gizmos.Cube(groundCheckPos, transform.rotation, new Vector3(0.5f, 0.01f, 0.5f) * transform.localScale.y, Color.blue, true);
        };
    }

    public void Update() {
        if(!enabled)
            return;
        foreach(ItemScript item in World.levelItems) {
            if(!item || !item.gameObject.activeInHierarchy)
                continue;
            DrawHitboxes(item.gameObject, Color.red, Color.green, Color.blue, Color.yellow);
        }
    }

    private static void RemoveOldObjectsFromList() {
        toRemove.Clear();
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(GameObject obj in hitboxes.Keys)
            if(!obj)
                toRemove.Add(obj);
        foreach(GameObject obj in toRemove)
            hitboxes.Remove(obj);
    }

    private static void DrawHitboxes(GameObject obj, Color color) {
        foreach(Collider collider in GetHitboxesInObject(obj))
            DrawCollider(collider, color);
    }

    private static void DrawHitboxes(GameObject obj, Color hazardColor, Color triggerColor, Color wallColor,
        Color defaultColor) {
        foreach(Collider collider in GetHitboxesInObject(obj))
            DrawCollider(collider, GetColliderType(collider) switch {
                ColliderType.Hazard => hazardColor,
                ColliderType.Trigger => triggerColor,
                ColliderType.Wall => wallColor,
                _ => defaultColor
            });
    }

    private static void DrawCollider(Collider collider, Color color) {
        if(!collider.enabled || !collider.gameObject.activeInHierarchy)
            color.a *= 0.2f;
        switch(collider) {
            case BoxCollider box:
                DrawCollider(box, color);
                break;
            case SphereCollider sphere:
                DrawCollider(sphere, color);
                break;
            case MeshCollider mesh:
                DrawCollider(mesh, color);
                break;
        }
    }

    private static void DrawCollider(BoxCollider collider, Color color) {
        Transform transform = collider.transform;
        Gizmos.Cube(transform.TransformPoint(collider.center), transform.rotation,
            Vector3.Scale(collider.size, transform.lossyScale), color);
    }

    private static void DrawCollider(SphereCollider collider, Color color) {
        Transform transform = collider.transform;
        Gizmos.Sphere(transform.TransformPoint(collider.center), transform.lossyScale.x * collider.radius, color);
    }

    private static void DrawCollider(MeshCollider collider, Color color) {
        Transform transform = collider.transform;
        Mesh mesh = collider.sharedMesh;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        for(int i = 0; i < triangles.Length; i += 3) {
            Vector3 p0 = transform.TransformPoint(vertices[triangles[i]]);
            Vector3 p1 = transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 p2 = transform.TransformPoint(vertices[triangles[i + 2]]);
            Gizmos.Line(p0, p1, color);
            Gizmos.Line(p1, p2, color);
            Gizmos.Line(p2, p0, color);
        }
    }

    private static ColliderType GetColliderType(Collider collider) =>
        collider.isTrigger ? ColliderType.Trigger : GetColliderType(collider.gameObject);

    private static ColliderType GetColliderType(GameObject obj) {
        while(true) {
            if(obj.CompareTag("Hazard"))
                return ColliderType.Hazard;
            if(obj.CompareTag("Wall"))
                return ColliderType.Wall;
            if(!obj.transform.parent)
                return ColliderType.Unknown;
            obj = obj.transform.parent.gameObject;
        }
    }

    private static IEnumerable<Collider> GetHitboxesInObject(GameObject obj) {
        if(hitboxes.TryGetValue(obj, out Collider[] colliders))
            return colliders;
        colliders = obj.GetComponentsInChildren<Collider>();
        hitboxes[obj] = colliders;
        return colliders;
    }
}
