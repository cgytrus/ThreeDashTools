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

    private static readonly List<CheckpointScript> checkpoints = new();

    private int currentCheckpoint {
        get => _currentCheckpoint;
        set => _currentCheckpoint = Mathf.Clamp(value, 0, checkpoints.Count);
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
            checkpoints.Clear();
        };

        Player.checkpointPlace += checkpoint => {
            checkpoints.Add(checkpoint);
        };

        Player.checkpointRemove += () => {
            for(int i = checkpoints.Count - 1; i >= 0; i--)
                if(!checkpoints[i])
                    checkpoints.RemoveAt(i);
        };

        On.PlayerScript.GetRecentCheckpoint += _ => {
            for(int i = checkpoints.Count - 1 - (enabled ? currentCheckpoint : 0); ; i--) {
                if(i < 0)
                    break;
                if(!checkpoints[i] || !checkpoints[i].gameObject.activeInHierarchy)
                    continue;
                if(i < checkpoints.Count)
                    return checkpoints[i].gameObject;
            }
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
