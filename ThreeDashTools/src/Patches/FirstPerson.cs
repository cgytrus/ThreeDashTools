using System.Linq;

using BepInEx.Configuration;

using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;
using UnityEngine.Rendering;

namespace ThreeDashTools.Patches;

[UsedImplicitly]
public class FirstPerson : ConfigurablePatch {
    private Transform? _block;
    private float _sensitivity;
    private readonly float[] _cameraOffsets = new float[5];

    public FirstPerson() : base(Plugin.instance!.Config, nameof(FirstPerson), "Enabled", false, "") {
        ConfigFile config = Plugin.instance.Config;

        int order = -1;

        ConfigEntry<float> sensitivity = config.Bind(nameof(FirstPerson), "Sensitivity", 1f, new ConfigDescription("",
            new AcceptableValueRange<float>(0f, 10f),
            new ConfigurationManagerAttributes {
                Order = order--,
                CustomDrawer = GuiUtil.SliderDrawer(0.05f, 5f)
            }));
        _sensitivity = sensitivity.Value;
        sensitivity.SettingChanged += (_, _) => { _sensitivity = sensitivity.Value; };

        void AddCameraOffsetConfig(string mode, int modeIndex, float defaultValue) {
            ConfigEntry<float> entry = config.Bind(nameof(FirstPerson), $"{mode}CameraOffset", defaultValue,
                new ConfigDescription("", new AcceptableValueRange<float>(-1f, 1f),
                    new ConfigurationManagerAttributes {
                        Order = order--,
                        IsAdvanced = true,
                        CustomDrawer = GuiUtil.SliderDrawer(-1f, 1f)
                    }));
            _cameraOffsets[modeIndex] = entry.Value;
            entry.SettingChanged += (_, _) => { _cameraOffsets[modeIndex] = entry.Value; };
        }

        AddCameraOffsetConfig("Cube", 0, 0.45f);
        AddCameraOffsetConfig("Ship", 1, 0.45f);
        AddCameraOffsetConfig("Wave", 2, 0f);
        AddCameraOffsetConfig("Hedron", 3, 0f);
        AddCameraOffsetConfig("Ufo", 4, 0.45f);
    }

    public override void Apply() {
        On.CameraRaiser.Start += (orig, self) => {
            orig(self);
            if(!enabled) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            Cursor.visible = false;
            _block = self.block;

            CameraSetup(self);
            GraphicsSetup();

            self.enabled = false;
        };

        On.BoomArm.Update += (orig, self) => {
            if(!enabled) {
                orig(self);
                return;
            }

            self.spd = 0.03f * _sensitivity;
            orig(self);

            Transform transform = self.transform;
            transform.position = _block!.position;

            float cameraOffset = _cameraOffsets[Player.scriptInstance!.GetCubeShape()];
            Vector3 gravDir = Player.scriptInstance.gravDirections[Player.scriptInstance.gravDirection];
            transform.GetChild(0).localPosition = new Vector3(cameraOffset * gravDir.z, cameraOffset * gravDir.y,
                cameraOffset * gravDir.x);

            Vector3 cameraRotation = transform.localEulerAngles;
            cameraRotation.z = 0f;
            transform.localRotation = Player.pathFollowerInstance!.gfx.transform.localRotation;
            transform.Rotate(cameraRotation, Space.Self);
        };

        ResetCursor();
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private static void CameraSetup(CameraRaiser self) {
        BoomArm boomArm = self.GetComponentInChildren<BoomArm>();
        boomArm.enabled = true;

        Camera camera = boomArm.transform.GetChild(0).GetComponent<Camera>();
        camera.fieldOfView = 70f;
        camera.nearClipPlane = 0.01f;

        boomArm.GetComponent<Animator>().enabled = false;
        boomArm.myEulerAngles += new Vector3(-25f, 0f, 0f);
    }

    private static void GraphicsSetup() {
        if(!Player.scriptInstance)
            return;

        DisableGraphicsObj(Player.scriptInstance!.shapes[0]); // cube
        DisableGraphicsTrans(Player.scriptInstance.shapes[1].transform.GetChild(1)); // ship
        DisableGraphicsObj(Player.scriptInstance.shapes[2]); // wave
        DisableGraphicsObj(Player.scriptInstance.shapes[3]); // hedron
        DisableGraphicsTrans(Player.scriptInstance.shapes[4].transform.GetChild(0)); // ufo

        Transform ufo = Player.scriptInstance.shapes[4].transform.Find("UFO");
        if(!ufo)
            ufo = Player.scriptInstance.shapes[4].transform;
        if(!ufo)
            return;

        Transform ufoDomeTransform = ufo.Find("Dome");
        if(!ufoDomeTransform)
            ufoDomeTransform = ufo.Find("Sphere.002");
        if(!ufoDomeTransform)
            return;

        // reverse ufo dome triangles so that the dome renders on the inside instead of the outside
        Mesh ufoDome = ufoDomeTransform.GetComponent<MeshFilter>().mesh;
        ufoDome.triangles = ufoDome.triangles.Reverse().ToArray();

        static void DisableGraphicsObj(GameObject obj) {
            foreach(Renderer renderer in obj.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        static void DisableGraphicsTrans(Component obj) {
            foreach(Renderer renderer in obj.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }

    private void ResetCursor() {
        On.PauseMenuManager.Pause += (orig, self) => {
            orig(self);
            if(!enabled)
                return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        };

        On.PauseMenuManager.Resume += (orig, self, resumeMusic) => {
            orig(self, resumeMusic);
            if(!enabled)
                return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        };

        On.TitleCamera.Start += (orig, self) => {
            orig(self);
            if(!enabled)
                return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        };
    }
}
