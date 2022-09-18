using System.Collections.Generic;

using BepInEx.Configuration;

using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;

namespace ThreeDashTools.Patches;

[UsedImplicitly]
public class CheckpointSwitcher : ConfigurablePatch {
    private readonly ConfigEntry<KeyboardShortcut> _prevCheckpoint;
    private readonly ConfigEntry<KeyboardShortcut> _nextCheckpoint;

    private int currentCheckpoint {
        get => _currentCheckpoint;
        set {
            GameObject[] checkpoints = GameObject.FindGameObjectsWithTag("Checkpoint");
            _currentCheckpoint = Mathf.Clamp(value, 0, checkpoints.Length);
        }
    }

    private int _currentCheckpoint;

    public CheckpointSwitcher() : base(Plugin.instance!.Config, nameof(CheckpointSwitcher), "Enabled", false, "") {
        ConfigFile config = Plugin.instance.Config;

        _prevCheckpoint = config.Bind(nameof(CheckpointSwitcher), "PrevCheckpoint",
            new KeyboardShortcut(KeyCode.Comma), "");

        _nextCheckpoint = config.Bind(nameof(CheckpointSwitcher), "NextCheckpoint",
            new KeyboardShortcut(KeyCode.Period), "");
    }

    public override void Apply() {
        World.levelLoading += () => {
            _currentCheckpoint = 0;
        };

        On.PlayerScript.GetRecentCheckpoint += orig => {
            if(!enabled)
                return orig();
            GameObject[] checkpoints = GameObject.FindGameObjectsWithTag("Checkpoint");
            int index = checkpoints.Length - 1 - currentCheckpoint;
            if(index >= 0 && index < checkpoints.Length)
                return checkpoints[index];
            return null;
        };

        On.PlayerScript.Update += (orig, self) => {
            orig(self);
            if(enabled)
                SwitchCheckpoint(self);
        };
    }

    private void SwitchCheckpoint(PlayerScript self) {
        bool prevCheckpoint = _prevCheckpoint.Value.IsDown();
        bool nextCheckpoint = _nextCheckpoint.Value.IsDown();
        if(prevCheckpoint)
            currentCheckpoint++;
        if(nextCheckpoint)
            currentCheckpoint--;
        if(!prevCheckpoint && !nextCheckpoint)
            return;
        self.Die();
        DeathScript? deathScript = Object.FindObjectOfType<DeathScript>();
        if(deathScript)
            deathScript!.timePassed = float.PositiveInfinity;
    }
}
