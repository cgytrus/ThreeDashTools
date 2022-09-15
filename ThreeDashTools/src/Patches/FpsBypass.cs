using BepInEx.Configuration;

using SixDash.Patches;

using UnityEngine;

namespace ThreeDashTools.Patches;

// ReSharper disable once UnusedType.Global
public class FpsBypass : IPatch {
    public FpsBypass() {
        ConfigFile config = Plugin.instance!.Config;

        ConfigEntry<bool> vsync = config.Bind("FpsBypass", "Vsync", true, "");
        QualitySettings.vSyncCount = vsync.Value ? 1 : 0;
        vsync.SettingChanged += (_, _) => { QualitySettings.vSyncCount = vsync.Value ? 1 : 0; };

        ConfigEntry<int> fps = config.Bind("FpsBypass", "Fps", -1, "");
        Application.targetFrameRate = fps.Value;
        fps.SettingChanged += (_, _) => { Application.targetFrameRate = fps.Value; };
    }

    public void Apply() { }
}
