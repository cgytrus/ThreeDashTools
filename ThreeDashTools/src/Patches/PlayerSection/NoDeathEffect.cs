using System.Collections.Generic;

using BepInEx.Configuration;

using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;

namespace ThreeDashTools.Patches.PlayerSection;

[UsedImplicitly]
public class NoDeathEffect : IPatch {
    private readonly ConfigEntry<bool> _entry;
    private readonly List<GameObject> _savedObjects = new();
    private PlayerScript? _player;

    public NoDeathEffect() {
        ConfigFile config = Plugin.instance!.Config;
        _entry = config.Bind("Player", nameof(NoDeathEffect), false, "");
        _entry.SettingChanged += (_, _) => { ToggleDeathFxParticles(!_entry.Value); };
    }

    public void Apply() {
        Player.playerSpawn += self => {
            _player = self;
            ToggleDeathFxParticles(!_entry.Value);
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach(GameObject obj in _savedObjects)
                if(obj)
                    Object.Destroy(obj);
            _savedObjects.Clear();
        };

        Player.playerDeath += self => {
            if(!_entry.Value)
                return;

            _savedObjects.Add(self.gfx);
            _savedObjects.Add(self.particles);

            self.gfx.transform.SetParent(null);
            self.particles.transform.SetParent(null);
        };
    }

    private void ToggleDeathFxParticles(bool enabled) {
        if(!_player)
            return;
        foreach(ParticleSystemRenderer renderer in _player!.DeathFX.GetComponentsInChildren<ParticleSystemRenderer>())
            renderer.enabled = enabled;
    }
}
