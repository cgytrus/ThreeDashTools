using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using BepInEx.Configuration;

using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace ThreeDashTools.Patches;

[UsedImplicitly]
public class Speedhack : IPatch {
    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<float> _speed;
    private readonly ConfigEntry<bool> _affectPhysics;
    private readonly Dictionary<AudioSource?, float> _audioSources = new();
    private GameObject? _deathFx;
    private readonly float _initialFixedDeltaTime;

    private static readonly GUILayoutOption[] sliderLayoutOptions = { GUILayout.ExpandWidth(true) };

    public Speedhack() {
        ConfigFile config = Plugin.instance!.Config;

        _enabled = config.Bind(nameof(Speedhack), "Enabled", false, "");
        _enabled.SettingChanged += (_, _) => { UpdateSpeed(); };

        _speed = config.Bind(nameof(Speedhack), "Speed", 1f,
            new ConfigDescription("", new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes {
                CustomDrawer = SliderDrawer
            }));
        _speed.SettingChanged += (_, _) => { UpdateSpeed(); };

        _affectPhysics = config.Bind(nameof(Speedhack), "AffectPhysics", false,
            new ConfigDescription("", null, new ConfigurationManagerAttributes {
            IsAdvanced = true
        }));
        _affectPhysics.SettingChanged += (_, _) => { UpdateSpeed(); };

        _initialFixedDeltaTime = Time.fixedDeltaTime;

        void SliderDrawer(ConfigEntryBase _) {
            if(_speed is null)
                return;

            float sliderValue = GUILayout.HorizontalSlider(_speed.Value, 0f, 3f, sliderLayoutOptions);
            if(sliderValue != _speed.Value)
                _speed.Value = sliderValue;

            if(float.TryParse(GUILayout.TextField(_speed.Value.ToString("F2", CultureInfo.InvariantCulture)),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out float value) && value != _speed.Value)
                _speed.Value = value;
        }
    }

    public void Apply() {
        _audioSources.Clear();
        AddAudioSources(Object.FindObjectsOfType<AudioSource?>(true));
        UpdateSpeed();

        On.PauseMenuManager.Resume += (orig, self, music) => {
            orig(self, music);
            UpdateSpeed();
        };

        On.CameraAnimator.Start += (orig, self) => {
            orig(self);
            UpdateSpeed();
        };

        On.MenuButtonScript.Start += (orig, self) => {
            orig(self);
            UpdateSpeed();
        };

        SceneManager.sceneLoaded += (scene, _) => {
            foreach(GameObject obj in scene.GetRootGameObjects())
                AddAudioSources(obj.GetComponentsInChildren<AudioSource?>(true));
            UpdateSpeed();
        };

        Player.spawn += self => {
            if(!_deathFx) {
                _deathFx = self.DeathFX;
                AddAudioSources(_deathFx.GetComponentsInChildren<AudioSource?>(true));
            }
            UpdateSpeed();
        };

        On.WinScript.Start += (orig, self) => {
            orig(self);
            AddAudioSources(self.gameObject.GetComponentsInChildren<AudioSource?>(true));
            UpdateSpeed();
        };
    }

    private void UpdateSpeed() {
        float speed = _enabled.Value ? _speed.Value : 1f;
        if(!PauseMenuManager.paused) {
            Time.timeScale = speed;
            Time.fixedDeltaTime = _initialFixedDeltaTime * (_affectPhysics.Value ? 1f : speed);
        }
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(AudioSource? source in _audioSources.Keys.ToList())
            if(!source)
                _audioSources.Remove(source);
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(KeyValuePair<AudioSource?, float> pair in _audioSources)
            if(pair.Key)
                pair.Key!.pitch = pair.Value * speed;
    }

    private void AddAudioSources(IEnumerable<AudioSource?> enumerable) {
        foreach(AudioSource? audio in enumerable) {
            if(_audioSources.ContainsKey(audio) || !audio)
                continue;
            _audioSources.Add(audio, audio!.pitch);
        }
    }
}
