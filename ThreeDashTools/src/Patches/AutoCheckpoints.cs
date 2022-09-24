using BepInEx.Configuration;

using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;

namespace ThreeDashTools.Patches;

[UsedImplicitly]
public class AutoCheckpoints : ConfigurablePatch {
    private const float BaseFps = 60f;
    private const float PlaceAttemptInterval = 20f / BaseFps;
    private const float TimeoutTime = 0.1f;
    private const float DefaultSpeed = 10.4f;

    private bool _quickCheckpointMode;

    private float _lastCheckpointPlaceAttemptTime = Time.time;

    private CheckpointScript? _pendingCheckpoint;

    private bool _timeout;
    private float _timeoutStart;

    private bool _forceOnGround;

    public AutoCheckpoints() : base(Plugin.instance!.Config, nameof(AutoCheckpoints), "Enabled", false, "") {
        ConfigFile config = Plugin.instance.Config;

        ConfigEntry<bool> quickCheckpointMode = config.Bind(nameof(AutoCheckpoints), "QuickCheckpointMode", false, "");
        _quickCheckpointMode = quickCheckpointMode.Value;
        quickCheckpointMode.SettingChanged += (_, _) => { _quickCheckpointMode = quickCheckpointMode.Value; };
    }

    public override void Apply() {
        Player.spawn += _ => {
            _lastCheckpointPlaceAttemptTime = Time.time;
            _timeout = false;
            _forceOnGround = true;
        };

        Player.death += _ => {
            if(!_timeout)
                return;
            Checkpoint.RemoveLatest();
        };

        On.PlayerScript.SetCubeShape += (orig, self, shapeIndex) => {
            orig(self, shapeIndex);
            if(shapeIndex is 1 or 2 or 4)
                Object.Destroy(_pendingCheckpoint);
        };

        On.PlayerScript.FixedUpdate += PlayerFixedUpdate;
    }

    private void PlayerFixedUpdate(On.PlayerScript.orig_FixedUpdate orig, PlayerScript self) {
        if(!enabled || !PauseMenuManager.inPracticeMode) {
            orig(self);
            return;
        }

        bool isFlying = self.isRocket || self.isUfo || self.isWave;

        // right after spawn we're not on ground so force the game to think we're always on ground until
        // we're actually on ground to prevent an auto checkpoint being set at the very beginning of the level
        bool prevOnGround = _forceOnGround || self.onGround;
        if(_forceOnGround)
            _forceOnGround = !self.onGround;

        orig(self);

        bool landed = self.onGround && self.onGround != prevOnGround;
        bool jumped = !self.onGround && self.onGround != prevOnGround;

        UpdateCheckpointTest(self, isFlying, jumped, landed);
    }

    private void UpdateCheckpointTest(PlayerScript player, bool isFlying, bool jumped, bool landed) {
        if(landed)
            TryPlaceCheckpoint(player, isFlying, jumped, landed);

        if(isFlying && Time.time - _lastCheckpointPlaceAttemptTime >= PlaceAttemptInterval) {
            TryPlaceCheckpoint(player, isFlying, jumped, landed);
            _lastCheckpointPlaceAttemptTime = Time.time;
        }

        if(_timeout && Time.time - _timeoutStart >= TimeoutTime)
            _timeout = false;
    }

    private void TryPlaceCheckpoint(PlayerScript player, bool isFlying, bool jumped, bool landed) {
        if(!isFlying && !jumped && !landed)
            return;

        CheckpointScript? lastCheckpoint = Checkpoint.GetLatest();
        if(lastCheckpoint && !_quickCheckpointMode && !FarEnoughToPlace(player.speed, lastCheckpoint!))
            return;

        if(isFlying)
            PlaceFlyingCheckpoint();
        else
            PlaceNormalCheckpoint();
    }

    private void PlaceNormalCheckpoint() {
        _timeout = true;
        _timeoutStart = Time.time;
        Checkpoint.Mark();
    }

    private void PlaceFlyingCheckpoint() {
        if(_pendingCheckpoint) {
            _pendingCheckpoint!.Store();
            _pendingCheckpoint = null;
        }
        else
            _pendingCheckpoint = Checkpoint.Create();
    }

    private bool FarEnoughToPlace(float speed, CheckpointScript checkpoint) =>
        PathFollower.distanceTravelled - checkpoint.savedX > (_quickCheckpointMode ? 2f : 10f) * speed / DefaultSpeed;
}
