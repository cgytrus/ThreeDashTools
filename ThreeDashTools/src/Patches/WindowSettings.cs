using System;
using System.Linq;

using BepInEx.Configuration;

using JetBrains.Annotations;

using SixDash.Patches;

using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

namespace ThreeDashTools.Patches;

[PublicAPI]
public class WindowSettings : IPatch {
    [PublicAPI, Serializable]
    private struct EquatableResolution : IEquatable<EquatableResolution>, IEquatable<Resolution> {
#pragma warning disable 0649
        public int width;
        public int height;
        public int refreshRate;
#pragma warning restore 0649

        public EquatableResolution(int width, int height, int refreshRate) {
            this.width = width;
            this.height = height;
            this.refreshRate = refreshRate;
        }

        public bool Equals(EquatableResolution other) =>
            width == other.width && height == other.height && refreshRate == other.refreshRate;

        public bool Equals(Resolution other) =>
            width == other.width && height == other.height && refreshRate == other.refreshRate;

        public override bool Equals(object? obj) => obj is EquatableResolution other && Equals(other) ||
            obj is Resolution res && Equals(res);

        public override int GetHashCode() {
            unchecked {
                int hashCode = width;
                hashCode = (hashCode * 397) ^ height;
                hashCode = (hashCode * 397) ^ refreshRate;
                return hashCode;
            }
        }

        public override string ToString() => $"{width}x{height} @ {refreshRate}";

        public static EquatableResolution Parse(string str) {
            string[] strings = str.Split(new[] { "x", " @ " }, StringSplitOptions.None);
            return new EquatableResolution(int.Parse(strings[0]), int.Parse(strings[1]), int.Parse(strings[2]));
        }

        public Resolution ToResolution() => UnsafeUtility.As<EquatableResolution, Resolution>(ref this);
        public static EquatableResolution FromResolution(Resolution res) =>
            UnsafeUtility.As<Resolution, EquatableResolution>(ref res);
    }

    public WindowSettings() {
        ConfigFile config = Plugin.instance!.Config;

        ConfigEntry<FullScreenMode> fullscreen =
            config.Bind("Window", "FullscreenMode", FullScreenMode.FullScreenWindow, "");
        // not initializing cuz already done with SetResolution below
        fullscreen.SettingChanged += (_, _) => { Screen.fullScreenMode = fullscreen.Value; };

        TomlTypeConverter.AddConverter(typeof(EquatableResolution), new TypeConverter {
            ConvertToString = (Func<object, Type, string>)((obj, _) => obj.ToString()),
            ConvertToObject = (Func<string, Type, object>)((str, _) => EquatableResolution.Parse(str))
        });

        EquatableResolution[] res = Screen.resolutions
            .Select(r => new EquatableResolution(r.width, r.height, r.refreshRate)).ToArray();
        ConfigEntry<EquatableResolution> resolution = config.Bind("Window", "Resolution",
            EquatableResolution.FromResolution(Screen.currentResolution),
            new ConfigDescription("", new AcceptableValueList<EquatableResolution>(res)));
        Screen.SetResolution(resolution.Value.width, resolution.Value.height, fullscreen.Value,
            resolution.Value.refreshRate);
        resolution.SettingChanged += (_, _) => {
            Screen.SetResolution(resolution.Value.width, resolution.Value.height, fullscreen.Value,
            resolution.Value.refreshRate); };
    }

    public void Apply() { }
}
