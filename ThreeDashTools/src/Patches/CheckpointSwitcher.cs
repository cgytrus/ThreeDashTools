﻿using BepInEx.Configuration;

using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;

namespace ThreeDashTools.Patches;

[UsedImplicitly]
public class CheckpointSwitcher : ConfigurablePatch {
    private readonly ConfigEntry<KeyboardShortcut> _prevCheckpoint;
    private readonly ConfigEntry<KeyboardShortcut> _nextCheckpoint;

    public CheckpointSwitcher() : base(Plugin.instance!.Config, nameof(CheckpointSwitcher), "Enabled", false, "") {
        ConfigFile config = Plugin.instance.Config;

        _prevCheckpoint = config.Bind(nameof(CheckpointSwitcher), "PrevCheckpoint",
            new KeyboardShortcut(KeyCode.Comma), "");

        _nextCheckpoint = config.Bind(nameof(CheckpointSwitcher), "NextCheckpoint",
            new KeyboardShortcut(KeyCode.Period), "");
    }

    public override void Apply() {
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
            Checkpoint.current++;
        if(nextCheckpoint)
            Checkpoint.current--;
        if(!prevCheckpoint && !nextCheckpoint)
            return;
        self.Die();
        DeathScript? deathScript = Object.FindObjectOfType<DeathScript>();
        if(deathScript)
            deathScript!.timePassed = float.PositiveInfinity;
    }
}
