using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using Invector.vCamera;
using UnityEngine;

namespace VAProxy.FirstPersonCamera
{
    [BepInPlugin("vap.firstpersoncamera", "VAProxy First Person Camera", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        ConfigEntry<float> configFov;
        ConfigEntry<float> configForwardOffset;
        ConfigEntry<float> configVerticalOffset;
        ConfigEntry<KeyboardShortcut> configToggle;
        FirstPersonController controller;

        void Awake()
        {
            configFov = Config.Bind("Camera", "Fov", 125f, "First-person field of view");
            configForwardOffset = Config.Bind("Camera", "ForwardOffset", 0.05999985f, "Forward offset from anchor");
            configVerticalOffset = Config.Bind("Camera", "VerticalOffset", -0.02f, "Vertical offset from anchor (positive moves up)");
            configToggle = Config.Bind("Camera", "ToggleShortcut", new KeyboardShortcut(KeyCode.F6), "Toggle first-person camera");
        }

        void Start()
        {
            StartCoroutine(Setup());
        }

        IEnumerator Setup()
        {
            vThirdPersonCamera camera = null;
            while (!camera)
            {
                camera = FindObjectOfType<vThirdPersonCamera>();
                yield return null;
            }
            controller = camera.gameObject.GetComponent<FirstPersonController>();
            if (!controller)
                controller = camera.gameObject.AddComponent<FirstPersonController>();
            controller.Init(camera, configFov, configForwardOffset, configVerticalOffset, Config);
        }

        void Update()
        {
            if (!controller)
                return;
            if (configToggle.Value.IsDown())
                controller.Toggle();
        }
    }

    internal class FirstPersonController : MonoBehaviour
    {
        const string AnchorPath = "S-105.1/ROOT/Hips/Spine/Spine1/Corrupt";
        const float FovStep = 5f;
        const float MinFov = 60f;
        const float MaxFov = 140f;
        const float MinPitch = -85f;
        const float MaxPitch = 85f;
        const float ForwardStep = 0.02f;
        const float VerticalStep = 0.01f;
        const float HoldInitialDelay = 0.25f;
        const float HoldRepeatInterval = 0.07f;
        const string SkipPath = "Director/Skip";
        vThirdPersonCamera originalCamera;
        Camera cam;
        float originalFov;
        GameObject anchor;
        GameObject skipObject;
        bool skipActive;
        bool desiredActive;
        bool active;
        float yaw;
        float pitch;
        ConfigEntry<float> cfgFov;
        ConfigEntry<float> cfgForward;
        ConfigEntry<float> cfgVertical;
        ConfigFile cfgFile;
        float nextFovUp;
        float nextFovDown;
        float nextForwardUp;
        float nextForwardDown;
        float nextVerticalUp;
        float nextVerticalDown;

        public void Init(vThirdPersonCamera camera, ConfigEntry<float> fov, ConfigEntry<float> forward, ConfigEntry<float> vertical, ConfigFile file)
        {
            originalCamera = camera;
            cam = camera.GetComponent<Camera>();
            if (!cam)
                cam = camera.GetComponentInChildren<Camera>();
            if (cam)
                originalFov = cam.fieldOfView;
            cfgFov = fov;
            cfgForward = forward;
            cfgVertical = vertical;
            cfgFile = file;
            yaw = transform.localEulerAngles.y;
            pitch = transform.localEulerAngles.x;
        }

        public void Toggle()
        {
            desiredActive = !desiredActive;
            UpdateActiveState();
        }

        void SetActive(bool value)
        {
            active = value;
            if (originalCamera)
                originalCamera.enabled = !value;
            if (cam)
                cam.fieldOfView = value ? cfgFov.Value : originalFov;
        }

        void UpdateActiveState()
        {
            SetActive(desiredActive && !skipActive);
        }

        void Update()
        {
            UpdateSkipState();
            if (!active)
                return;
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (cam)
            {
                HandleAdjust(KeyCode.I, true, () => AdjustFov(FovStep), ref nextFovUp);
                HandleAdjust(KeyCode.O, true, () => AdjustFov(-FovStep), ref nextFovDown);
                HandleAdjust(KeyCode.UpArrow, alt && !shift, () => AdjustVertical(VerticalStep), ref nextVerticalUp);
                HandleAdjust(KeyCode.DownArrow, alt && !shift, () => AdjustVertical(-VerticalStep), ref nextVerticalDown);
                HandleAdjust(KeyCode.UpArrow, alt && shift, () => AdjustForward(ForwardStep), ref nextForwardUp);
                HandleAdjust(KeyCode.DownArrow, alt && shift, () => AdjustForward(-ForwardStep), ref nextForwardDown);
            }
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
            transform.localEulerAngles = new Vector3(pitch, yaw, 0f);
        }

        void LateUpdate()
        {
            if (!active)
                return;
            if (!anchor)
                anchor = GameObject.Find(AnchorPath);
            if (!anchor)
                return;
            transform.position = anchor.transform.position + transform.forward * cfgForward.Value + Vector3.up * cfgVertical.Value;
        }

        void AdjustFov(float delta)
        {
            cfgFov.Value = Mathf.Clamp(cfgFov.Value + delta, MinFov, MaxFov);
            cam.fieldOfView = cfgFov.Value;
            SaveConfig();
        }

        void AdjustForward(float delta)
        {
            cfgForward.Value += delta;
            SaveConfig();
        }

        void AdjustVertical(float delta)
        {
            cfgVertical.Value += delta;
            SaveConfig();
        }

        void SaveConfig()
        {
            if (cfgFile != null)
                cfgFile.Save();
        }

        void HandleAdjust(KeyCode key, bool condition, System.Action adjust, ref float nextTime)
        {
            if (!condition)
                return;
            float now = Time.unscaledTime;
            if (Input.GetKeyDown(key))
            {
                adjust();
                nextTime = now + HoldInitialDelay;
            }
            else if (Input.GetKey(key) && now >= nextTime)
            {
                adjust();
                nextTime = now + HoldRepeatInterval;
            }
        }

        void UpdateSkipState()
        {
            if (!skipObject)
                skipObject = GameObject.Find(SkipPath);
            bool isSkip = skipObject && skipObject.activeInHierarchy;
            if (isSkip == skipActive)
                return;
            skipActive = isSkip;
            UpdateActiveState();
        }
    }
}
