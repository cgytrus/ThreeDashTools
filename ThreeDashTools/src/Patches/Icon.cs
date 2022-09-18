using System;
using System.Collections.Generic;
using System.Linq;

using BepInEx.Configuration;

using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;

namespace ThreeDashTools.Patches;

[UsedImplicitly]
public class Icon : IPatch {
    private readonly HashSet<Material> _primaryMaterials = new();
    private readonly HashSet<Material> _insideMaterials = new();
    private readonly HashSet<Material> _eyelidMaterials = new();
    private readonly HashSet<Material> _outlineMaterials = new();
    private readonly HashSet<Outline> _outlineOutlines = new();
    private readonly HashSet<Material> _shipParticlesMaterials = new();
    private readonly HashSet<Material> _shipTrailMaterials = new();
    private readonly HashSet<Material> _ufoParticlesMaterials = new();
    private readonly HashSet<Material> _ufoTrailMaterials = new();
    private readonly HashSet<Outline> _hedronOutlines = new();

    private readonly ConfigEntry<Color> _primaryColor;
    private readonly ConfigEntry<Color> _insideColor;
    private readonly ConfigEntry<Color> _eyelidColor;
    private readonly ConfigEntry<Color> _outlineColor;
    private readonly ConfigEntry<Color> _shipParticlesColor;
    private readonly ConfigEntry<Color> _shipTrailColor;
    private readonly ConfigEntry<Color> _ufoParticlesColor;
    private readonly ConfigEntry<Color> _ufoTrailColor;
    private readonly ConfigEntry<Color> _hedronOutlineColor;

    public Icon() {
        ConfigFile config = Plugin.instance!.Config;

        int order = 0;

        ConfigEntry<Color> DefineMatColorConfig(string key, Color defaultValue, HashSet<Material> materials,
            bool advanced) => DefineColorConfig(key, defaultValue, advanced,
            entry => SetMaterialsColor(materials, entry.Value));

        ConfigEntry<Color> DefineOutColorConfig(string key, Color defaultValue, HashSet<Outline> outlines,
            bool advanced) => DefineColorConfig(key, defaultValue, advanced,
            entry => SetOutlinesColor(outlines, entry.Value));

        ConfigEntry<Color> DefineColorConfig(string key, Color defaultValue, bool advanced, Action<ConfigEntry<Color>> update) {
            ConfigEntry<Color> entry = config.Bind(nameof(Icon), key, defaultValue,
                new ConfigDescription("", null, new ConfigurationManagerAttributes {
                    Order = order--,
                    IsAdvanced = advanced
                }));
            entry.SettingChanged += (_, _) => update(entry);
            return entry;
        }

        _primaryColor = DefineMatColorConfig("PrimaryColor", new Color(1f, 0.7129f, 0f), _primaryMaterials, false);
        _insideColor = DefineMatColorConfig("InsideColor", Color.cyan, _insideMaterials, false);
        _eyelidColor = DefineMatColorConfig("EyelidColor", new Color(0f, 0.7949f, 0.8019f), _eyelidMaterials, false);
        _outlineColor = DefineColorConfig("OutlineColor", Color.black, false, entry => {
            SetMaterialsColor(_outlineMaterials, entry.Value);
            SetOutlinesColor(_outlineOutlines, entry.Value);
        });

        _shipParticlesColor = DefineMatColorConfig("ShipParticlesColor", new Color(1f, 0.4847f, 0f),
            _shipParticlesMaterials, true);
        _shipTrailColor = DefineMatColorConfig("ShipTrailColor", new Color(1f, 1f, 1f, 0.2824f),
            _shipTrailMaterials, true);

        _ufoParticlesColor = DefineMatColorConfig("UfoParticlesColor", Color.white, _ufoParticlesMaterials, true);
        _ufoTrailColor = DefineMatColorConfig("UfoTrailColor", new Color(0.5991f, 0.9733f, 1f, 1f / 3f),
            _ufoTrailMaterials, true);

        _hedronOutlineColor =
            DefineOutColorConfig("HedronOutlineColor", new Color(0f, 0.9164f, 1f), _hedronOutlines, false);
    }

    public void Apply() {
        Player.playerSpawn += self => {
            _primaryMaterials.Clear();
            _insideMaterials.Clear();
            _eyelidMaterials.Clear();
            _outlineMaterials.Clear();
            _outlineOutlines.Clear();
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
            foreach(Outline outline in self.shapes[4].GetComponentsInChildren<Outline>()
                .Where(outline => outline.OutlineColor == Color.black))
                _outlineOutlines.Add(outline);
            _hedronOutlines.Add(self.shapes[3].GetComponentInChildren<Outline>());

            SetMaterialsColor(_primaryMaterials, _primaryColor.Value);
            SetMaterialsColor(_insideMaterials, _insideColor.Value);
            SetMaterialsColor(_eyelidMaterials, _eyelidColor.Value);
            SetMaterialsColor(_outlineMaterials, _outlineColor.Value);
            SetOutlinesColor(_outlineOutlines, _outlineColor.Value);
            SetMaterialsColor(_shipParticlesMaterials, _shipParticlesColor.Value);
            SetMaterialsColor(_shipTrailMaterials, _shipTrailColor.Value);
            SetMaterialsColor(_ufoParticlesMaterials, _ufoParticlesColor.Value);
            SetMaterialsColor(_ufoTrailMaterials, _ufoTrailColor.Value);
            SetOutlinesColor(_hedronOutlines, _hedronOutlineColor.Value);
        };
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
            _insideMaterials.Add(sharedMaterial);
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
