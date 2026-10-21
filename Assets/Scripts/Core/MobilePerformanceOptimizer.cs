using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IdleFishing.Core
{
    /// <summary>
    /// Handles mobile device optimization, dynamic target framerate, battery saver mode,
    /// and periodic memory garbage collection across world/stage transitions.
    /// </summary>
    public class MobilePerformanceOptimizer : MonoBehaviour
    {
        public static MobilePerformanceOptimizer Instance { get; private set; }

        [Header("Framerate & Power")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool batterySaverMode = false;

        [Header("Dynamic Resolution Scaling")]
        [SerializeField] private bool enableDynamicResolution = true;
        [SerializeField] private float minRenderScale = 0.75f;
        [SerializeField] private float maxRenderScale = 1.0f;

        [Header("Debug Display")]
        [SerializeField] private bool showFpsCounter = false;

        private float currentFps = 60f;
        private float frameTimeAccumulator = 0f;
        private int frameCount = 0;
        private float fpsUpdateInterval = 0.5f;
        private float nextFpsUpdateTime = 0f;

        public float CurrentFPS => currentFps;
        public bool IsBatterySaverActive => batterySaverMode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ApplyPerformanceProfile();
        }

        private void Start()
        {
            ApplyPerformanceProfile();
        }

        private void Update()
        {
            // FPS calculation
            frameCount++;
            frameTimeAccumulator += Time.unscaledDeltaTime;

            if (Time.unscaledTime >= nextFpsUpdateTime)
            {
                currentFps = frameCount / frameTimeAccumulator;
                frameCount = 0;
                frameTimeAccumulator = 0f;
                nextFpsUpdateTime = Time.unscaledTime + fpsUpdateInterval;

                if (enableDynamicResolution && !Application.isEditor)
                {
                    EvaluateDynamicRenderScale();
                }
            }
        }

        public void SetBatterySaver(bool enabled)
        {
            batterySaverMode = enabled;
            targetFrameRate = enabled ? 30 : 60;
            ApplyPerformanceProfile();
        }

        public void ToggleBatterySaver()
        {
            SetBatterySaver(!batterySaverMode);
        }

        public void SetShowFps(bool show)
        {
            showFpsCounter = show;
        }

        private void ApplyPerformanceProfile()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = batterySaverMode ? 30 : 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var urpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.renderScale = batterySaverMode ? minRenderScale : maxRenderScale;
            }

            Debug.Log($"[MobilePerformanceOptimizer] Applied profile: TargetFPS={Application.targetFrameRate}, BatterySaver={batterySaverMode}, RenderScale={(urpAsset != null ? urpAsset.renderScale : 1f)}");
        }

        private void EvaluateDynamicRenderScale()
        {
            var urpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null) return;

            // If FPS drops below 40 on 60FPS mode, gently scale down render scale
            if (!batterySaverMode && currentFps < 42f && urpAsset.renderScale > minRenderScale)
            {
                urpAsset.renderScale = Mathf.Max(minRenderScale, urpAsset.renderScale - 0.05f);
            }
            else if (!batterySaverMode && currentFps > 57f && urpAsset.renderScale < maxRenderScale)
            {
                urpAsset.renderScale = Mathf.Min(maxRenderScale, urpAsset.renderScale + 0.05f);
            }
        }

        /// <summary>
        /// Perform lightweight memory reclamation during stage or world transitions
        /// </summary>
        public void CleanMemory()
        {
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
            Debug.Log("[MobilePerformanceOptimizer] Garbage collected and unused assets unloaded.");
        }

        private void OnGUI()
        {
            if (!showFpsCounter) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 26;
            style.normal.textColor = currentFps >= 50 ? Color.green : (currentFps >= 30 ? Color.yellow : Color.red);

            string text = $"FPS: {currentFps:F0} | {(batterySaverMode ? "BATTERY SAVER" : "60 FPS")}";
            GUI.Label(new Rect(30, 100, 350, 40), text, style);
        }
    }
}
