using System.Collections.Generic;

using JetBrains.Annotations;

using SixDash;
using SixDash.API;
using SixDash.Patches;

using UnityEngine;

namespace ThreeDashTools.Patches.LevelSection;

[UsedImplicitly]
public class PulsingOrbs : ConfigurablePatch {
    private readonly Dictionary<GameObject, List<(Transform, Vector3, SphereCollider?, float)>> _pulsingOrbs = new();

    public PulsingOrbs() : base(Plugin.instance!.Config, "Level", nameof(PulsingOrbs), false, "") { }

    public override void Apply() {
        World.levelLoaded += () => {
            _pulsingOrbs.Clear();
            if(!enabled)
                return;
            RegisterPulsingOrbs();
        };

        World.levelUpdate += (_, _) => {
            foreach(Chunk chunk in World.levelChunks.Values) {
                if(!chunk.active)
                    continue;
                UpdatePulsingOrbs(chunk);
            }
        };
    }

    private void RegisterPulsingOrbs() {
        foreach(ItemScript item in World.levelItems) {
            OrbScript? orb = item.GetComponentInChildren<OrbScript>();
            if(!orb)
                continue;
            List<(Transform, Vector3, SphereCollider?, float)> objects = new();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach(MeshFilter meshFilter in orb.transform.parent.GetComponentsInChildren<MeshFilter>()) {
                Transform trans = meshFilter.transform;
                SphereCollider? collider = trans.GetComponent<SphereCollider>();
                objects.Add((trans, trans.localScale, collider, collider ? collider!.radius : 0f));
            }
            _pulsingOrbs.Add(item.gameObject, objects);
        }
    }

    private void UpdatePulsingOrbs(Chunk chunk) {
        foreach((_, GameObject obj, _, _) in chunk.itemObjects)
            UpdatePulseIfOrb(obj);
    }

    private void UpdatePulseIfOrb(GameObject obj) {
        if(!_pulsingOrbs.TryGetValue(obj, out List<(Transform, Vector3, SphereCollider?, float)> objects))
            return;
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach((Transform, Vector3, SphereCollider?, float) pObj in objects) {
            if(!pObj.Item1)
                break;
            float pulseScale = Mathf.Min(Music.pulse + 0.25f, 1f);
            pObj.Item1.localScale = pObj.Item2 * pulseScale;
            if(pObj.Item3)
                pObj.Item3!.radius = pObj.Item4 / pulseScale;
        }
    }
}
