﻿using BepInEx;

using SixDash;

using ThreeDashTools.Patches;

namespace ThreeDashTools;

[BepInPlugin("mod.cgytrus.plugins.3dashtools", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("mod.cgytrus.plugins.sixdash", "0.1.0")]
public class Plugin : BaseUnityPlugin {
    public static Plugin? instance { get; private set; }

    public Plugin() => instance = this;

    private void Awake() => Util.ApplyAllPatches();

    private void Update() => ShowHitboxes.instance?.Update();
}
