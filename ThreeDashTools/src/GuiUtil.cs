using System;
using System.Globalization;

using BepInEx.Configuration;

using UnityEngine;

namespace ThreeDashTools;

internal static class GuiUtil {
    private static readonly CultureInfo culture = CultureInfo.InvariantCulture;
    private static readonly GUILayoutOption[] sliderLayoutOptions = { GUILayout.ExpandWidth(true) };
    private static readonly GUILayoutOption[] sliderInputLayoutOptions = { GUILayout.Width(50f) };

    public static Action<ConfigEntryBase?> SliderDrawer(float min, float max, string format = "F2") => entry => {
        if(entry?.BoxedValue is not float value)
            return;

        float sliderValue = GUILayout.HorizontalSlider(value, min, max, sliderLayoutOptions);
        if(sliderValue != value) {
            entry.BoxedValue = sliderValue;
            value = sliderValue;
        }

        if(float.TryParse(
                GUILayout.TextField(value.ToString(format, culture), sliderInputLayoutOptions),
                NumberStyles.Any, CultureInfo.InvariantCulture, out float inputValue) && inputValue != value)
            entry.BoxedValue = inputValue;
    };
}
