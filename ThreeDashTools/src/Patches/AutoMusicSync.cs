using System;

using BepInEx.Configuration;

using PathCreation;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;

using Gizmos = Popcron.Gizmos;

namespace ThreeDashTools.Patches;

// ReSharper disable once UnusedType.Global
public class AutoMusicSync : IPatch {
    private enum Mode { None, SyncToMusicTime, SyncToPlayerPosition, SyncToLevelTime }

    private Mode _mode;
    private float _unsyncThreshold;
    private bool _debug;

    private static float _levelStartTime;
    private static float levelTime {
        get => Time.time - _levelStartTime;
        set => _levelStartTime = Time.time - value;
    }

    private static float playerTime => World.DistanceToTime(PathFollower.distanceTravelled);

    private float baseTime => _mode switch {
        Mode.None => 0f,
        Mode.SyncToMusicTime => Player.music && Player.music!.isPlaying ? Player.music.time : 0f,
        Mode.SyncToPlayerPosition => playerTime,
        Mode.SyncToLevelTime => levelTime,
        _ => throw new ArgumentOutOfRangeException()
    };
    private float _prevBaseTime;

    public AutoMusicSync() {
        ConfigFile config = Plugin.instance!.Config;

        ConfigEntry<Mode> mode = config.Bind("AutoMusicSync", "Mode", Mode.None, "");
        _mode = mode.Value;
        mode.SettingChanged += (_, _) => { _mode = mode.Value; };

        ConfigEntry<float> threshold = config.Bind("AutoMusicSync", "UnsyncThreshold", 0.05f, "");
        _unsyncThreshold = threshold.Value;
        threshold.SettingChanged += (_, _) => { _unsyncThreshold = threshold.Value; };

        ConfigEntry<bool> debug = config.Bind("AutoMusicSync", "Debug", false,
            new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
        _debug = debug.Value;
        debug.SettingChanged += (_, _) => { _debug = debug.Value; };
    }

    public void Apply() {
        On.PathFollower.FixedUpdate += (orig, self) => {
            if(_mode == Mode.None || !self.ready || !self.isMoving || !SyncAll(self))
                orig(self);
        };

        On.PauseMenuManager.PauseAllMusic += (orig, self) => {
            ForceSyncAll(null);
            orig(self);
        };

        Player.playerSpawn += _ => {
            _levelStartTime = Time.time;
            ForceSyncAll(null);
        };

        On.PathFollower.Update += (orig, self) => {
            orig(self);
            if(_debug)
                DrawDebug(self.pathCreator.path, self.endOfPathInstruction);
        };
    }

    private static void DrawDebug(VertexPath path, EndOfPathInstruction endOfPath) {
        DrawDebugLine(PathFollower.distanceTravelled, Color.green);
        DrawDebugLine(World.TimeToDistance(levelTime), Color.red);
        if(Player.music)
            DrawDebugLine(World.TimeToDistance(Player.music!.timeSamples / (float)Player.music.clip.frequency),
                Color.blue, !Player.music.isPlaying);

        void DrawDebugLine(float distance, Color color, bool dashed = false) {
            Vector3 point = path.GetPointAtDistance(distance, endOfPath);
            Gizmos.Line(new Vector3(point.x, point.y - 5f, point.z), new Vector3(point.x, point.y + 5f, point.z), color,
                dashed);
        }
    }

    private bool SyncAll(PathFollower pathFollower) {
        bool syncedPlayer = false;
        if(!Player.music || !Player.music!.isPlaying)
            return syncedPlayer;
        float baseTime = this.baseTime;

        if(baseTime != _prevBaseTime) {
            if(baseTime == 0f)
                ForceSyncAll(pathFollower);
            SyncMusicIfNeeded(baseTime);
            if(_mode != Mode.SyncToLevelTime)
                syncedPlayer = SyncPlayerIfNeeded(baseTime, pathFollower);
            SyncLevelTimeIfNeeded(baseTime);
        }
        _prevBaseTime = baseTime;

        if(_mode != Mode.SyncToLevelTime)
            return syncedPlayer;

        SetPlayerTime(pathFollower, baseTime);
        return true;
    }

    private void ForceSyncAll(PathFollower? pathFollower) {
        float baseTime = this.baseTime;
        if(_mode != Mode.SyncToMusicTime && Player.music && Player.music!.isPlaying)
            Player.music.time = baseTime;
        if(_mode != Mode.SyncToPlayerPosition)
            SetPlayerTime(pathFollower, baseTime);
        if(_mode != Mode.SyncToLevelTime)
            levelTime = baseTime;
    }

    private void SyncMusicIfNeeded(float baseTime) {
        if(_mode == Mode.SyncToMusicTime || Player.music == null || !Player.music.isPlaying)
            return;
        float unsync = Mathf.Abs(Player.music.time - baseTime);
        if(unsync >= _unsyncThreshold)
            Player.music.time = baseTime;
    }

    private bool SyncPlayerIfNeeded(float baseTime, PathFollower? pathFollower) {
        if(_mode == Mode.SyncToPlayerPosition)
            return false;
        float unsync = Mathf.Abs(playerTime - baseTime);
        if(unsync < _unsyncThreshold)
            return false;
        SetPlayerTime(pathFollower, baseTime);
        return true;
    }

    private void SyncLevelTimeIfNeeded(float baseTime) {
        if(_mode == Mode.SyncToLevelTime)
            return;
        float unsync = Mathf.Abs(levelTime - baseTime);
        if(unsync >= _unsyncThreshold)
            levelTime = baseTime;
    }

    private static void SetPlayerTime(PathFollower? pathFollower, float time) {
        PathFollower.distanceTravelled = World.TimeToDistance(time);
        if(pathFollower != null)
            pathFollower.UpdatePositionInstant(PathFollower.distanceTravelled);
    }
}
