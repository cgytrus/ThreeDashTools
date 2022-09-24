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

    private GameObject? _pendingCheckpoint;

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
        Player.playerSpawn += _ => {
            _lastCheckpointPlaceAttemptTime = Time.time;
            _timeout = false;
            _forceOnGround = true;
        };

        Player.playerDeath += DeleteCheckpointOnDeath;

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

    private void DeleteCheckpointOnDeath(PlayerScript self) {
        if(!_timeout)
            return;
        PauseMenuManager? pauseMenuManager = Object.FindObjectOfType<PauseMenuManager>();
        if(pauseMenuManager)
            pauseMenuManager.DeleteCheckpoint();
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

        GameObject? checkpointObj = PlayerScript.GetRecentCheckpoint();
        CheckpointScript? lastCheckpoint = checkpointObj ? checkpointObj.GetComponent<CheckpointScript>() : null;
        if(lastCheckpoint && !_quickCheckpointMode && !FarEnoughToPlace(player.speed, lastCheckpoint!))
            return;

        if(isFlying)
            PlaceFlyingCheckpoint(player);
        else
            PlaceNormalCheckpoint(player);
    }

    private void PlaceNormalCheckpoint(PlayerScript player) {
        _timeout = true;
        _timeoutStart = Time.time;
        player.MakeCheckpoint();
    }

    private void PlaceFlyingCheckpoint(PlayerScript player) {
        if(_pendingCheckpoint) {
            _pendingCheckpoint!.SetActive(true);
            _pendingCheckpoint = null;
        }
        else {
            player.MakeCheckpoint();
            _pendingCheckpoint = PlayerScript.GetRecentCheckpoint();
            _pendingCheckpoint.SetActive(false);
        }
    }

    private bool FarEnoughToPlace(float speed, CheckpointScript checkpoint) =>
        PathFollower.distanceTravelled - checkpoint.savedX > (_quickCheckpointMode ? 2f : 10f) * speed / DefaultSpeed;
}
