using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BepInEx.Configuration;

using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;

using Object = UnityEngine.Object;

namespace ThreeDashTools.Patches;

[UsedImplicitly]
public class Icon : ConfigurablePatch {
    private static readonly string iconsPath = Path.Combine(Application.streamingAssetsPath, "Icons");
    private static readonly Dictionary<string, GameObject> iconPrefabs = new();

    private bool _extended;

    private readonly HashSet<Material> _primaryMaterials = new();
    private readonly HashSet<Material> _secondaryMaterials = new();
    private readonly HashSet<Material> _eyelidMaterials = new();
    private readonly HashSet<Material> _outlineMaterials = new();
    private readonly HashSet<Outline> _outlineOutlines = new();
    private readonly HashSet<Material> _shipParticlesMaterials = new();
    private readonly HashSet<Material> _shipTrailMaterials = new();
    private readonly HashSet<Material> _ufoParticlesMaterials = new();
    private readonly HashSet<Material> _ufoTrailMaterials = new();
    private readonly HashSet<Outline> _hedronOutlines = new();

    private readonly ConfigEntry<Color> _primaryColor;
    private readonly ConfigEntry<Color> _secondaryColor;
    private readonly ConfigEntry<Color> _eyelidColor;
    private readonly ConfigEntry<Color> _outlineColor;
    private readonly ConfigEntry<Color> _shipParticlesColor;
    private readonly ConfigEntry<Color> _shipTrailColor;
    private readonly ConfigEntry<Color> _ufoParticlesColor;
    private readonly ConfigEntry<Color> _ufoTrailColor;
    private readonly ConfigEntry<Color> _hedronOutlineColor;

    private readonly ConfigEntry<string> _cubeIcon;
    private readonly ConfigEntry<string> _shipIcon;
    private readonly ConfigEntry<string> _waveIcon;
    private readonly ConfigEntry<string> _hedronIcon;
    private readonly ConfigEntry<string> _ufoIcon;

    public Icon() : base(Plugin.instance!.Config, nameof(Icon), "EnableCustomization", false, "Requires respawn") {
        ConfigFile config = Plugin.instance.Config;

        int order = -1;

        const string extKey = "ExtendedCustomization";
        ConfigEntry<bool> ext = config.Bind(nameof(Icon), extKey, false,
            new ConfigDescription("", null, new ConfigurationManagerAttributes {
                Order = order--,
                IsAdvanced = true
            }));
        _extended = ext.Value;
        ext.SettingChanged += (_, _) => _extended = ext.Value;

        ConfigEntry<Color> DefineMatColorConfig(string key, Color defaultValue, HashSet<Material> materials,
            bool advanced, bool extended) => DefineColorConfig(key, defaultValue, advanced, extended,
            entry => SetMaterialsColor(materials, entry.Value));

        ConfigEntry<Color> DefineOutColorConfig(string key, Color defaultValue, HashSet<Outline> outlines,
            bool advanced, bool extended) => DefineColorConfig(key, defaultValue, advanced, extended,
            entry => SetOutlinesColor(outlines, entry.Value));

        ConfigEntry<Color> DefineColorConfig(string key, Color defaultValue, bool advanced, bool extended,
            Action<ConfigEntry<Color>> update) {
            ConfigEntry<Color> entry = config.Bind(nameof(Icon), key, defaultValue,
                new ConfigDescription(extended ? $"Requires {extKey} on" : "", null,
                    new ConfigurationManagerAttributes {
                        Order = order--,
                        IsAdvanced = advanced
                    }));
            entry.SettingChanged += (_, _) => update(entry);
            return entry;
        }

        AcceptableValueList<string> acceptableIcons = new(iconPrefabs.Keys.Prepend(string.Empty).ToArray());
        ConfigEntry<string> DefineIconConfig(string key) {
            ConfigEntry<string> entry = config.Bind(nameof(Icon), key, string.Empty,
                new ConfigDescription("Requires respawn", acceptableIcons, new ConfigurationManagerAttributes {
                    Order = order--
                }));
            return entry;
        }

        _primaryColor =
            DefineMatColorConfig("PrimaryColor", new Color(1f, 0.7129f, 0f), _primaryMaterials, false, false);
        _secondaryColor = DefineMatColorConfig("SecondaryColor", Color.cyan, _secondaryMaterials, false, false);
        _eyelidColor =
            DefineMatColorConfig("EyelidColor", new Color(0f, 0.7949f, 0.8019f), _eyelidMaterials, true, true);
        _outlineColor = DefineColorConfig("OutlineColor", Color.black, false, false, entry => {
            SetMaterialsColor(_outlineMaterials, entry.Value);
            SetOutlinesColor(_outlineOutlines, entry.Value);
        });

        _shipParticlesColor = DefineMatColorConfig("ShipParticlesColor", new Color(1f, 0.4847f, 0f),
            _shipParticlesMaterials, true, false);
        _shipTrailColor = DefineMatColorConfig("ShipTrailColor", new Color(1f, 1f, 1f, 0.2824f),
            _shipTrailMaterials, true, false);

        _ufoParticlesColor =
            DefineMatColorConfig("UfoParticlesColor", Color.white, _ufoParticlesMaterials, true, false);
        _ufoTrailColor = DefineMatColorConfig("UfoTrailColor", new Color(0.5991f, 0.9733f, 1f, 1f / 3f),
            _ufoTrailMaterials, true, false);

        _hedronOutlineColor =
            DefineOutColorConfig("HedronOutlineColor", new Color(0f, 0.9164f, 1f), _hedronOutlines, true, true);

        _cubeIcon = DefineIconConfig("CubeIcon");
        _shipIcon = DefineIconConfig("ShipIcon");
        _waveIcon = DefineIconConfig("WaveIcon");
        _hedronIcon = DefineIconConfig("HedronIcon");
        _ufoIcon = DefineIconConfig("UfoIcon");
    }

    public static void LoadAssets() {
        foreach(string iconAssetBundle in Directory.EnumerateFiles(iconsPath)) {
            AssetBundle bundle = AssetBundle.LoadFromFile(iconAssetBundle);
            GameObject[] objects = bundle.LoadAllAssets<GameObject>();
            foreach(GameObject obj in objects)
                if(!obj.name.StartsWith("_", StringComparison.Ordinal))
                    iconPrefabs.Add(obj.name, obj);
        }
    }

    public override void Apply() {
        Player.playerSpawn += self => {
            if(!enabled)
                return;
            _outlineOutlines.Clear();
            ReplaceCubeIcon(self);
            ReplaceShipIcon(self);
            ReplaceWaveIcon(self);
            ReplaceHedronIcon(self);
            ReplaceUfoIcon(self);
            UpdateMaterials(self);
        };
    }

    private void ReplaceCubeIcon(PlayerScript self) {
        if(!iconPrefabs.TryGetValue(_cubeIcon.Value, out GameObject prefab))
            return;

        Transform cube = self.shapes[0].transform;
        Transform cubeInShip = self.shapes[1].transform.GetChild(1);
        Transform cubeInUfo = self.shapes[4].transform.GetChild(0);

        ReplaceIcon(cube.GetChild(1), prefab);
        ReplaceIcon(cubeInShip.GetChild(1), prefab);
        ReplaceIcon(cubeInUfo.GetChild(1), prefab);

        cube.GetChild(0).gameObject.SetActive(false);
        cubeInShip.GetChild(0).gameObject.SetActive(false);
        cubeInUfo.GetChild(0).gameObject.SetActive(false);
    }

    private void ReplaceShipIcon(PlayerScript self) {
        if(!iconPrefabs.TryGetValue(_shipIcon.Value, out GameObject prefab))
            return;
        ReplaceIcon(self.shapes[1].transform.GetChild(0), prefab);
    }

    private void ReplaceWaveIcon(PlayerScript self) {
        if(!iconPrefabs.TryGetValue(_waveIcon.Value, out GameObject prefab))
            return;
        ReplaceIcon(self.shapes[2].transform.GetChild(0), prefab);
    }

    private void ReplaceHedronIcon(PlayerScript self) {
        if(!iconPrefabs.TryGetValue(_hedronIcon.Value, out GameObject prefab))
            return;
        self.shapes[3] = ReplaceIcon(self.shapes[3].transform, prefab);
    }

    private void ReplaceUfoIcon(PlayerScript self) {
        if(!iconPrefabs.TryGetValue(_ufoIcon.Value, out GameObject prefab))
            return;
        ReplaceIcon(self.shapes[4].transform.GetChild(2), prefab);
    }

    private static GameObject ReplaceIcon(Transform transform, GameObject prefab) {
        GameObject icon = Object.Instantiate(prefab, transform.position, transform.rotation, transform.parent);
        Transform iconTrans = icon.transform;
        iconTrans.localScale = transform.localScale;
        iconTrans.SetSiblingIndex(transform.GetSiblingIndex());
        icon.SetActive(transform.gameObject.activeSelf);
        Object.Destroy(transform.gameObject);
        return icon;
    }

    private void UpdateMaterials(PlayerScript self) {
        _primaryMaterials.Clear();
        _secondaryMaterials.Clear();
        _eyelidMaterials.Clear();
        _outlineMaterials.Clear();
        _shipParticlesMaterials.Clear();
        _shipTrailMaterials.Clear();
        _ufoParticlesMaterials.Clear();
        _ufoTrailMaterials.Clear();
        _hedronOutlines.Clear();

        foreach(GameObject obj in self.shapes)
            AssignMaterials(obj);
        AssignMaterials(self.runParticles);
        AssignMaterials(self.rocketTrail);
        AssignMaterials(self.waveTrail);
        AssignMaterials(self.hedronTrail);
        AssignMaterials(self.ufoTrail);
        foreach(GameObject shape in self.shapes)
            foreach(Outline outline in shape.GetComponentsInChildren<Outline>()
                .Where(outline => outline.OutlineColor == Color.black))
                _outlineOutlines.Add(outline);
        _hedronOutlines.Add(self.shapes[3].GetComponentInChildren<Outline>());

        SetMaterialsColor(_primaryMaterials, _primaryColor.Value);
        SetMaterialsColor(_secondaryMaterials, _secondaryColor.Value);
        // why is this not public in the first place??
#pragma warning disable Publicizer001
        SetMaterialsColor(_eyelidMaterials, _extended ? _eyelidColor.Value : _secondaryColor.Value.RGBMultiplied(0.8f));
#pragma warning restore Publicizer001
        SetMaterialsColor(_outlineMaterials, _outlineColor.Value);
        SetOutlinesColor(_outlineOutlines, _outlineColor.Value);
        SetMaterialsColor(_shipParticlesMaterials, _shipParticlesColor.Value);
        SetMaterialsColor(_shipTrailMaterials, _shipTrailColor.Value);
        SetMaterialsColor(_ufoParticlesMaterials, _ufoParticlesColor.Value);
        SetMaterialsColor(_ufoTrailMaterials, _ufoTrailColor.Value);
        SetOutlinesColor(_hedronOutlines, _extended ? _hedronOutlineColor.Value : _secondaryColor.Value);
    }

    private void AssignMaterials(GameObject obj) {
        foreach(Renderer renderer in obj.GetComponentsInChildren<Renderer>())
            AssignMaterials(renderer);
    }

    private void AssignMaterials(Renderer renderer) {
#pragma warning disable Publicizer001
        for(int i = 0; i < renderer.GetMaterialCount(); i++)
#pragma warning restore Publicizer001
            AssignMaterial(renderer, i);
    }

    private void AssignMaterial(Renderer renderer, int index) {
        const StringComparison comp = StringComparison.Ordinal;

        Material sharedMaterial = renderer.sharedMaterials[index];

        if(sharedMaterial.name.StartsWith("SHELLMAT", comp))
            _primaryMaterials.Add(sharedMaterial);
        else if(sharedMaterial.name.StartsWith("INSIDEMAT", comp))
            _secondaryMaterials.Add(sharedMaterial);
        else if(sharedMaterial.name.StartsWith("EYELIDMAT", comp))
            _eyelidMaterials.Add(sharedMaterial);
        else if(sharedMaterial.name.StartsWith("PlayerOutline", comp))
            _outlineMaterials.Add(sharedMaterial);
        else if(sharedMaterial.name.StartsWith("Black", comp))
            _outlineMaterials.Add(renderer.materials[index]);
        else if(sharedMaterial.name.StartsWith("ItemOrange", comp))
            _shipParticlesMaterials.Add(renderer.materials[index]);
        else if(sharedMaterial.name.StartsWith("TranslucentWhite", comp))
            _shipTrailMaterials.Add(renderer.materials[index]);
        else if(sharedMaterial.name.StartsWith("ItemWhite", comp))
            _ufoParticlesMaterials.Add(renderer.materials[index]);
        else if(sharedMaterial.name.StartsWith("ItemCyanTranslucent", comp))
            _ufoTrailMaterials.Add(renderer.materials[index]);
    }

    private static void SetMaterialsColor(IEnumerable<Material> materials, Color color) {
        foreach(Material material in materials)
            if(material)
                material.color = color;
    }

    private static void SetOutlinesColor(IEnumerable<Outline> outlines, Color color) {
        foreach(Outline outline in outlines)
            if(outline)
                outline.OutlineColor = color;
    }
}
