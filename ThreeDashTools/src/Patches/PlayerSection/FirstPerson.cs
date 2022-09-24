using System.Linq;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;
using UnityEngine.Rendering;

namespace ThreeDashTools.Patches.PlayerSection;

// ReSharper disable once UnusedType.Global
public class FirstPerson : ConfigurablePatch {
    private Transform? _block;
    private PathFollower? _pathFollower;

    public FirstPerson() : base(Plugin.instance!.Config, "Player", nameof(FirstPerson), false, "") { }

    public override void Apply() {
        On.CameraRaiser.Start += (orig, self) => {
            orig(self);
            if(!enabled) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            self.GetComponentInChildren<BoomArm>().enabled = true;
            _block = self.block;
            self.enabled = false;
        };

        On.BoomArm.Start += (orig, self) => {
            orig(self);
            if(!enabled)
                return;
            Cursor.visible = false;
            self.GetComponent<Animator>().enabled = false;
            self.spd = 0.03f;
            self.myEulerAngles += new Vector3(-25f, 0f, 0f);

            Camera camera = self.transform.GetChild(0).GetComponent<Camera>();
            camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.01f;

            if(!Player.scriptInstance)
                return;
            DisableGraphicsObj(Player.scriptInstance!.shapes[0]); // cube
            DisableGraphicsTrans(Player.scriptInstance.shapes[1].transform.GetChild(1)); // ship
            DisableGraphicsObj(Player.scriptInstance.shapes[2]); // wave
            DisableGraphicsObj(Player.scriptInstance.shapes[3]); // hedron
            DisableGraphicsTrans(Player.scriptInstance.shapes[4].transform.GetChild(0)); // ufo

            // reverse ufo dome triangles so that the dome renders on the inside instead of the outside
            Mesh ufoDome = Player.scriptInstance.shapes[4].transform.Find("UFO").Find("Sphere.002")
                .GetComponent<MeshFilter>().mesh;
            ufoDome.triangles = ufoDome.triangles.Reverse().ToArray();

            static void DisableGraphicsObj(GameObject obj) {
                foreach(Renderer renderer in obj.GetComponentsInChildren<Renderer>())
                    renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            static void DisableGraphicsTrans(Component obj) {
                foreach(Renderer renderer in obj.GetComponentsInChildren<Renderer>())
                    renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        };

        On.BoomArm.Update += (orig, self) => {
            orig(self);
            if(!enabled)
                return;
            Transform transform = self.transform;
            transform.position = _block!.position;
            Vector3 cameraOffset = new(0f, 0.45f, 0f);
            if(Player.scriptInstance!.gravDirections[Player.scriptInstance.gravDirection].y < 0f)
                cameraOffset.y = -cameraOffset.y;
            transform.GetChild(0).localPosition = cameraOffset;

            Vector3 cameraRotation = transform.localEulerAngles;
            cameraRotation.z = 0f;
            transform.localRotation = _pathFollower!.gfx.transform.localRotation;
            transform.Rotate(cameraRotation, Space.Self);
        };

        On.PathFollower.Awake += (orig, self) => {
            orig(self);
            _pathFollower = self;
        };

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
