using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.VFX;
using IdleFishing.Audio;

namespace IdleFishing.World
{
    /// <summary>
    /// Controls the Stylized Toon Water Surface & Water Flow system:
    /// - Subdivided water mesh for 3D vertex wave displacement
    /// - Sloped ocean seabed for physical depth gradient & crystal-clear reef visibility
    /// - True 3D object contour foam wrapping objects based on Scene Depth (zero fake circles)
    /// - Directional water current flow streaks
    /// - Interactive multi-ripple shader rings on player taps and fish/bobber splashes
    /// - Mathematical wave height, normal, and flow velocity queries for buoyancy
    /// </summary>
    [ExecuteAlways]
    public class OceanWaterFlowController : MonoBehaviour
    {
        public static OceanWaterFlowController Instance { get; private set; }

        [Header("Water Material Asset")]
        [SerializeField] private Material waterMaterial;

        [Header("=== 1. ĐƯỜNG BAO VIỀN BỌT NƯỚC (CONTOUR FOAM) ===")]
        [Tooltip("Màu sắc và độ mờ đục của bọt nước (Alpha điều chỉnh độ đậm nhạt)")]
        [SerializeField] private Color foamColor = new Color(0.97f, 0.99f, 1.0f, 0.96f);

        [Tooltip("Tổng độ dày vùng viền bọt ôm quanh khối đa giác 3D (meters)")]
        [Range(0.01f, 1.0f)]
        [SerializeField] private float contourFoamThickness = 0.22f;

        [Tooltip("Độ rộng dải viền bọt trong cùng bám sát mép lưới vật thể 3D")]
        [Range(0.005f, 0.4f)]
        [SerializeField] private float contourInnerLine = 0.075f;

        [Tooltip("Khoảng cách viền bọt phụ bên ngoài so với mép vật thể")]
        [Range(0.02f, 0.6f)]
        [SerializeField] private float contourOuterLine = 0.17f;

        [Tooltip("Độ mảnh/dày của dải viền bọt phụ bên ngoài")]
        [Range(0.005f, 0.2f)]
        [SerializeField] private float contourOuterWidth = 0.045f;

        [Tooltip("Độ gợn mép hữu cơ tự nhiên của đường viền bọt")]
        [Range(0.5f, 10.0f)]
        [SerializeField] private float contourNoiseScale = 2.4f;

        [Tooltip("Biên độ uốn lượn lồi lõm của viền bọt")]
        [Range(0.0f, 0.1f)]
        [SerializeField] private float contourNoiseStrength = 0.028f;

        [Tooltip("Tốc độ xung nhịp gợn sóng của viền bọt phụ")]
        [Range(0.0f, 8.0f)]
        [SerializeField] private float contourPulseSpeed = 2.8f;

        [Tooltip("Ngưỡng độ sâu tối thiểu Y để sinh bọt (vật thể sâu hơn mức này sẽ không có bọt)")]
        [Range(-3.0f, 0.0f)]
        [SerializeField] private float contourCutoffDepth = -0.75f;

        [Header("=== 2. MÀU SẮC & CHUYỂN MÀU ĐỘ SÂU (WATER COLORS & DEPTH) ===")]
        [Tooltip("Màu nước nông gần bờ / rạn san hô")]
        [SerializeField] private Color shallowColor = new Color(0.14f, 0.85f, 0.88f, 0.74f);

        [Tooltip("Màu nước biển trung tầng")]
        [SerializeField] private Color baseColor = new Color(0.08f, 0.56f, 0.85f, 0.88f);

        [Tooltip("Màu nước biển sâu xa khơi")]
        [SerializeField] private Color deepColor = new Color(0.04f, 0.26f, 0.56f, 0.96f);

        [Tooltip("Sắc độ đường chân trời / góc xiên viền mặt nước (Fresnel)")]
        [SerializeField] private Color horizonColor = new Color(0.18f, 0.65f, 0.92f, 1.0f);

        [Tooltip("Khoảng cách chuyển màu độ sâu (càng nhỏ chuyển màu sang nước sâu càng nhanh)")]
        [Range(0.5f, 10.0f)]
        [SerializeField] private float depthMaxDistance = 2.4f;

        [Header("=== 3. SÓNG NƯỚC & VERTEX NHẤP NHÔ (WAVE MOTION) ===")]
        [Tooltip("Độ cao chuẩn của mặt biển (World Y)")]
        [SerializeField] private float baseWaterLevelY = 0.05f;

        [Tooltip("Biên độ nhấp nhô của sóng (độ cao đỉnh sóng)")]
        [Range(0.0f, 0.3f)]
        [SerializeField] private float waveAmplitude = 0.055f;

        [Tooltip("Tần số sóng (mật độ cuộn sóng)")]
        [Range(0.1f, 4.0f)]
        [SerializeField] private float waveFrequency = 0.85f;

        [Tooltip("Tốc độ sóng cuộn trôi")]
        [Range(0.1f, 6.0f)]
        [SerializeField] private float waveSpeed = 1.65f;

        [Tooltip("Tỷ lệ sóng thứ cấp tạo dao động tự nhiên")]
        [Range(0.1f, 4.0f)]
        [SerializeField] private float secondaryWaveScale = 1.45f;

        [Header("=== 4. DÒNG NƯỚC CHẢY (FLOW CURRENT) ===")]
        [Tooltip("Hướng chảy chính của dòng nước (X, Z)")]
        [SerializeField] private Vector2 flowDirection = new Vector2(0.97f, 0.23f);

        [Tooltip("Tốc độ dòng chảy")]
        [Range(0.0f, 3.0f)]
        [SerializeField] private float flowSpeed = 0.55f;

        [Tooltip("Cường độ vệt nước chảy bọt trắng")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float flowStreakIntensity = 0.52f;

        [Tooltip("Kích thước vệt sợi dòng chảy")]
        [Range(0.05f, 2.0f)]
        [SerializeField] private float flowStreakScale = 0.35f;

        [Tooltip("Tọa độ Z tâm luồng chảy xiết")]
        [SerializeField] private float flowBandCenterZ = 17.5f;

        [Tooltip("Độ rộng luồng chảy xiết")]
        [SerializeField] private float flowBandWidth = 9.0f;

        [Header("=== 5. KHÚC XẠ ÁNH SÁNG ĐÁY NƯỚC (VORONOI CAUSTICS) ===")]
        [Tooltip("Màu sắc và độ sáng của mạng lưới quang học đáy nước")]
        [SerializeField] private Color causticColor = new Color(0.58f, 0.96f, 1.0f, 0.32f);

        [Tooltip("Kích thước ô lưới khúc xạ Voronoi")]
        [Range(0.1f, 3.0f)]
        [SerializeField] private float causticScale = 0.65f;

        [Tooltip("Tốc độ biến thiên tia khúc xạ")]
        [Range(0.1f, 3.0f)]
        [SerializeField] private float causticSpeed = 0.85f;

        [Tooltip("Độ mảnh/sắc nét của viền tế bào quang học")]
        [Range(0.01f, 0.25f)]
        [SerializeField] private float causticThreshold = 0.055f;

        [Tooltip("Độ mềm mại mép tia sáng")]
        [Range(0.005f, 0.15f)]
        [SerializeField] private float causticSoftness = 0.025f;

        [Header("=== 6. LẤP LÁNH ÁNH NẮNG & SÓNG BỜ (SPARKLES & SHORE) ===")]
        [Tooltip("Màu phản chiếu tia nắng mặt trời (Specular)")]
        [SerializeField] private Color specularColor = new Color(1.0f, 0.98f, 0.88f, 1.0f);

        [Tooltip("Độ tụ đốm sáng mặt trời")]
        [Range(0.8f, 0.999f)]
        [SerializeField] private float specularSize = 0.965f;

        [Tooltip("Cường độ lấp lánh")]
        [Range(0.0f, 2.0f)]
        [SerializeField] private float sparkleIntensity = 0.85f;

        [Tooltip("Tọa độ Z mép bờ cảng")]
        [SerializeField] private float shorelineZ = 10.15f;

        [Tooltip("Độ dày vùng sóng vỗ bến cảng")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float shoreFoamWidth = 0.65f;

        [Header("=== 7. ĐỘ PHÂN GIẢI LƯỚI NƯỚC (MESH RESOLUTION) ===")]
        [Tooltip("Số ô lưới chia theo trục X")]
        [SerializeField] private int gridResolutionX = 72;

        [Tooltip("Số ô lưới chia theo trục Z")]
        [SerializeField] private int gridResolutionZ = 52;

        private struct ActiveRipple
        {
            public Vector3 position;
            public float startTime;
            public float duration;
            public float maxRadius;
            public float intensity;
            public float ringFrequency;
        }

        private const int MaxRipples = 16;
        private readonly List<ActiveRipple> activeRipples = new List<ActiveRipple>();
        private readonly Vector4[] shaderRippleData = new Vector4[MaxRipples];
        private readonly Vector4[] shaderRippleParams = new Vector4[MaxRipples];

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private Mesh generatedWaterMesh;

        private Color worldTintMultiplier = Color.white;

        public Vector2 FlowDirectionNormalized => flowDirection.sqrMagnitude > 0.0001f ? flowDirection.normalized : Vector2.right;
        public float FlowSpeed => flowSpeed;
        public float BaseWaterLevelY => baseWaterLevelY;
        public Material WaterMaterial => waterMaterial;

        private void Awake()
        {
            Instance = this;
            InitializeWaterSystem();
        }

        private void OnEnable()
        {
            Instance = this;
            InitializeWaterSystem();
            GameEvents.WorldChanged += OnWorldChanged;
        }

        private void OnDisable()
        {
            GameEvents.WorldChanged -= OnWorldChanged;
        }

        public void InitializeWaterSystem()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();

            EnsureCameraDepthTexture();
            EnsureWaterMaterial();
            EnsureSubdividedWaterMesh();
            EnsureOceanSeabed();
            RemoveLegacyVFXArtifacts();
            EnsureFloatingBuoys();
            SyncShaderParameters();
            UpdateShaderRipples(GetCurrentTime());
        }

        private float GetCurrentTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return (float)UnityEditor.EditorApplication.timeSinceStartup;
            }
#endif
            return Time.time;
        }

        private void Update()
        {
            float now = GetCurrentTime();
            if (Application.isPlaying)
            {
                UpdateDayNightWaterMood();
            }
            UpdateShaderRipples(now);
        }

        // =========================================================
        // 1. WAVE HEIGHT, NORMAL & FLOW QUERIES
        // =========================================================
        public float GetWaveHeightAtPosition(Vector3 worldPos)
        {
            EvaluateWavesCPU(new Vector2(worldPos.x, worldPos.z), GetCurrentTime(), out float h, out _, out _);
            return baseWaterLevelY + h;
        }

        public Vector3 GetWaveNormalAtPosition(Vector3 worldPos)
        {
            EvaluateWavesCPU(new Vector2(worldPos.x, worldPos.z), GetCurrentTime(), out _, out Vector2 grad, out _);
            return new Vector3(-grad.x * 2.2f, 1.0f, -grad.y * 2.2f).normalized;
        }

        public Vector3 GetFlowVelocityAtPosition(Vector3 worldPos)
        {
            Vector2 dir = FlowDirectionNormalized;
            float channelFactor = Mathf.Exp(-Mathf.Pow((worldPos.z - flowBandCenterZ) / Mathf.Max(1.0f, flowBandWidth), 2.0f));
            float speed = flowSpeed * Mathf.Lerp(0.45f, 1.15f, channelFactor);
            return new Vector3(dir.x * speed, 0f, dir.y * speed);
        }

        private void EvaluateWavesCPU(Vector2 worldXZ, float t, out float heightOffset, out Vector2 gradXZ, out float crestFactor)
        {
            Vector2 fDir = FlowDirectionNormalized;
            Vector2 crossDir = new Vector2(-fDir.y, fDir.x);

            Vector2 dir1 = (fDir * 0.75f + new Vector2(0f, -0.65f)).normalized;
            float phase1 = Vector2.Dot(worldXZ, dir1) * waveFrequency + t * waveSpeed;
            float w1 = Mathf.Sin(phase1);
            float dw1 = Mathf.Cos(phase1) * waveFrequency;

            Vector2 dir2 = (crossDir * 0.65f + new Vector2(0.25f, 0.72f)).normalized;
            float freq2 = waveFrequency * secondaryWaveScale;
            float phase2 = Vector2.Dot(worldXZ, dir2) * freq2 - t * (waveSpeed * 1.18f);
            float w2 = Mathf.Sin(phase2) * 0.65f;
            float dw2 = Mathf.Cos(phase2) * freq2 * 0.65f;

            Vector2 dir3 = (fDir - crossDir * 0.4f).normalized;
            float freq3 = waveFrequency * 2.3f;
            float phase3 = Vector2.Dot(worldXZ, dir3) * freq3 + t * (waveSpeed * 1.55f);
            float w3 = Mathf.Cos(phase3) * 0.30f;
            float dw3 = -Mathf.Sin(phase3) * freq3 * 0.30f;

            float combined = (w1 + w2 + w3) / 1.95f;
            heightOffset = combined * waveAmplitude;
            gradXZ = (dir1 * dw1 + dir2 * dw2 + dir3 * dw3) * (waveAmplitude / 1.95f);
            crestFactor = Mathf.Clamp01(combined * 0.5f + 0.5f);
        }

        // =========================================================
        // 2. INTERACTIVE RIPPLES (Only on splash, bite, or player click)
        // =========================================================
        public void RegisterRipple(Vector3 worldPos, float maxRadius = 1.8f, float intensity = 0.85f, float ringFreq = 12.0f, float duration = 1.6f)
        {
            float now = GetCurrentTime();
            activeRipples.RemoveAll(r => (now - r.startTime) >= r.duration);

            if (activeRipples.Count >= MaxRipples)
            {
                activeRipples.RemoveAt(0);
            }

            activeRipples.Add(new ActiveRipple
            {
                position = new Vector3(worldPos.x, baseWaterLevelY, worldPos.z),
                startTime = now,
                duration = Mathf.Max(0.3f, duration),
                maxRadius = Mathf.Max(0.3f, maxRadius),
                intensity = Mathf.Clamp(intensity, 0.1f, 1.5f),
                ringFrequency = ringFreq
            });
        }

        private void UpdateShaderRipples(float now)
        {
            if (waterMaterial == null) return;

            activeRipples.RemoveAll(r => (now - r.startTime) >= r.duration || (now - r.startTime) < 0f);

            int count = Mathf.Min(activeRipples.Count, MaxRipples);
            for (int i = 0; i < MaxRipples; i++)
            {
                if (i < count)
                {
                    var r = activeRipples[i];
                    float age = Mathf.Clamp01((now - r.startTime) / r.duration);
                    shaderRippleData[i] = new Vector4(r.position.x, r.position.y, r.position.z, age);
                    shaderRippleParams[i] = new Vector4(r.maxRadius, r.intensity, r.ringFrequency, 0f);
                }
                else
                {
                    shaderRippleData[i] = Vector4.zero;
                    shaderRippleParams[i] = Vector4.zero;
                }
            }

            waterMaterial.SetInt("_RippleCount", count);
            waterMaterial.SetVectorArray("_RippleData", shaderRippleData);
            waterMaterial.SetVectorArray("_RippleParams", shaderRippleParams);
        }

        private void OnMouseDown()
        {
            if (!Application.isPlaying) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane waterPlane = new Plane(Vector3.up, new Vector3(0f, baseWaterLevelY, 0f));
            if (waterPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                RegisterRipple(hitPoint, 2.2f, 1.1f, 13.0f, 1.8f);

                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnCFXRWaterRipple(hitPoint, 0.85f);
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayWaterPlop(hitPoint);
                }
            }
        }

        // =========================================================
        // 3. SHADER & DEPTH INITIALIZATION
        // =========================================================
        private void EnsureCameraDepthTexture()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.depthTextureMode |= DepthTextureMode.Depth;
            }

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                urpAsset.supportsCameraDepthTexture = true;
            }
            if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset qUrpAsset)
            {
                qUrpAsset.supportsCameraDepthTexture = true;
            }
        }

        private void EnsureWaterMaterial()
        {
            Shader waterShader = Shader.Find("IdleFishing/ToonStylizedWater");
            if (waterShader == null) return;

#if UNITY_EDITOR
            string matPath = "Assets/Art/Materials/M_ToonOceanWater.mat";
            Material assetMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (assetMat == null)
            {
                assetMat = new Material(waterShader);
                assetMat.name = "M_ToonOceanWater";
                UnityEditor.AssetDatabase.CreateAsset(assetMat, matPath);
            }
            else if (assetMat.shader != waterShader)
            {
                assetMat.shader = waterShader;
            }
            waterMaterial = assetMat;
#else
            if (waterMaterial == null || waterMaterial.shader != waterShader)
            {
                waterMaterial = new Material(waterShader);
            }
#endif

            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = waterMaterial;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = true;
            }
        }

        public void SyncShaderParameters()
        {
            if (waterMaterial == null) return;

            waterMaterial.SetColor("_ShallowColor", MultiplyColor(shallowColor, worldTintMultiplier));
            waterMaterial.SetColor("_BaseColor", MultiplyColor(baseColor, worldTintMultiplier));
            waterMaterial.SetColor("_DeepColor", MultiplyColor(deepColor, worldTintMultiplier));
            waterMaterial.SetColor("_HorizonColor", horizonColor);
            waterMaterial.SetFloat("_DepthMaxDistance", depthMaxDistance);

            waterMaterial.SetFloat("_WaveAmplitude", waveAmplitude);
            waterMaterial.SetFloat("_WaveFrequency", waveFrequency);
            waterMaterial.SetFloat("_WaveSpeed", waveSpeed);
            waterMaterial.SetFloat("_SecondaryWaveScale", secondaryWaveScale);

            Vector2 fDir = FlowDirectionNormalized;
            waterMaterial.SetVector("_FlowDirection", new Vector4(fDir.x, fDir.y, 0f, 0f));
            waterMaterial.SetFloat("_FlowSpeed", flowSpeed);
            waterMaterial.SetFloat("_FlowStreakIntensity", flowStreakIntensity);
            waterMaterial.SetFloat("_FlowStreakScale", flowStreakScale);
            waterMaterial.SetFloat("_FlowBandCenterZ", flowBandCenterZ);
            waterMaterial.SetFloat("_FlowBandWidth", flowBandWidth);

            waterMaterial.SetColor("_CausticColor", causticColor);
            waterMaterial.SetFloat("_CausticScale", causticScale);
            waterMaterial.SetFloat("_CausticSpeed", causticSpeed);
            waterMaterial.SetFloat("_CausticThreshold", causticThreshold);
            waterMaterial.SetFloat("_CausticSoftness", causticSoftness);

            waterMaterial.SetColor("_FoamColor", foamColor);
            waterMaterial.SetFloat("_ContourFoamThickness", contourFoamThickness);
            waterMaterial.SetFloat("_ContourInnerLine", contourInnerLine);
            waterMaterial.SetFloat("_ContourOuterLine", contourOuterLine);
            waterMaterial.SetFloat("_ContourOuterWidth", contourOuterWidth);
            waterMaterial.SetFloat("_ContourNoiseScale", contourNoiseScale);
            waterMaterial.SetFloat("_ContourNoiseStrength", contourNoiseStrength);
            waterMaterial.SetFloat("_ContourPulseSpeed", contourPulseSpeed);
            waterMaterial.SetFloat("_ContourCutoffDepth", contourCutoffDepth);

            waterMaterial.SetFloat("_ShorelineZ", shorelineZ);
            waterMaterial.SetFloat("_ShoreFoamWidth", shoreFoamWidth);

            waterMaterial.SetColor("_SpecularColor", specularColor);
            waterMaterial.SetFloat("_SpecularSize", specularSize);
            waterMaterial.SetFloat("_SparkleIntensity", sparkleIntensity);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (waterMaterial != null)
            {
                SyncShaderParameters();
            }
        }
#endif

        [ContextMenu("Apply Settings to Material Asset")]
        public void ApplySettingsToMaterial()
        {
            EnsureWaterMaterial();
            SyncShaderParameters();
#if UNITY_EDITOR
            if (waterMaterial != null)
            {
                UnityEditor.EditorUtility.SetDirty(waterMaterial);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log("[OceanWaterFlowController] Water & Foam settings applied to Material asset successfully!");
            }
#endif
        }

        [ContextMenu("Read Settings from Material Asset")]
        public void ReadSettingsFromMaterial()
        {
            if (waterMaterial == null) EnsureWaterMaterial();
            if (waterMaterial == null) return;

            if (waterMaterial.HasProperty("_FoamColor")) foamColor = waterMaterial.GetColor("_FoamColor");
            if (waterMaterial.HasProperty("_ContourFoamThickness")) contourFoamThickness = waterMaterial.GetFloat("_ContourFoamThickness");
            if (waterMaterial.HasProperty("_ContourInnerLine")) contourInnerLine = waterMaterial.GetFloat("_ContourInnerLine");
            if (waterMaterial.HasProperty("_ContourOuterLine")) contourOuterLine = waterMaterial.GetFloat("_ContourOuterLine");
            if (waterMaterial.HasProperty("_ContourOuterWidth")) contourOuterWidth = waterMaterial.GetFloat("_ContourOuterWidth");
            if (waterMaterial.HasProperty("_ContourNoiseScale")) contourNoiseScale = waterMaterial.GetFloat("_ContourNoiseScale");
            if (waterMaterial.HasProperty("_ContourNoiseStrength")) contourNoiseStrength = waterMaterial.GetFloat("_ContourNoiseStrength");
            if (waterMaterial.HasProperty("_ContourPulseSpeed")) contourPulseSpeed = waterMaterial.GetFloat("_ContourPulseSpeed");
            if (waterMaterial.HasProperty("_ContourCutoffDepth")) contourCutoffDepth = waterMaterial.GetFloat("_ContourCutoffDepth");

            if (waterMaterial.HasProperty("_ShallowColor")) shallowColor = waterMaterial.GetColor("_ShallowColor");
            if (waterMaterial.HasProperty("_BaseColor")) baseColor = waterMaterial.GetColor("_BaseColor");
            if (waterMaterial.HasProperty("_DeepColor")) deepColor = waterMaterial.GetColor("_DeepColor");
            if (waterMaterial.HasProperty("_HorizonColor")) horizonColor = waterMaterial.GetColor("_HorizonColor");
            if (waterMaterial.HasProperty("_DepthMaxDistance")) depthMaxDistance = waterMaterial.GetFloat("_DepthMaxDistance");

            if (waterMaterial.HasProperty("_WaveAmplitude")) waveAmplitude = waterMaterial.GetFloat("_WaveAmplitude");
            if (waterMaterial.HasProperty("_WaveFrequency")) waveFrequency = waterMaterial.GetFloat("_WaveFrequency");
            if (waterMaterial.HasProperty("_WaveSpeed")) waveSpeed = waterMaterial.GetFloat("_WaveSpeed");
            if (waterMaterial.HasProperty("_SecondaryWaveScale")) secondaryWaveScale = waterMaterial.GetFloat("_SecondaryWaveScale");

            if (waterMaterial.HasProperty("_FlowDirection"))
            {
                Vector4 dirV = waterMaterial.GetVector("_FlowDirection");
                flowDirection = new Vector2(dirV.x, dirV.y);
            }
            if (waterMaterial.HasProperty("_FlowSpeed")) flowSpeed = waterMaterial.GetFloat("_FlowSpeed");
            if (waterMaterial.HasProperty("_FlowStreakIntensity")) flowStreakIntensity = waterMaterial.GetFloat("_FlowStreakIntensity");
            if (waterMaterial.HasProperty("_FlowStreakScale")) flowStreakScale = waterMaterial.GetFloat("_FlowStreakScale");
            if (waterMaterial.HasProperty("_FlowBandCenterZ")) flowBandCenterZ = waterMaterial.GetFloat("_FlowBandCenterZ");
            if (waterMaterial.HasProperty("_FlowBandWidth")) flowBandWidth = waterMaterial.GetFloat("_FlowBandWidth");

            if (waterMaterial.HasProperty("_CausticColor")) causticColor = waterMaterial.GetColor("_CausticColor");
            if (waterMaterial.HasProperty("_CausticScale")) causticScale = waterMaterial.GetFloat("_CausticScale");
            if (waterMaterial.HasProperty("_CausticSpeed")) causticSpeed = waterMaterial.GetFloat("_CausticSpeed");
            if (waterMaterial.HasProperty("_CausticThreshold")) causticThreshold = waterMaterial.GetFloat("_CausticThreshold");
            if (waterMaterial.HasProperty("_CausticSoftness")) causticSoftness = waterMaterial.GetFloat("_CausticSoftness");

            if (waterMaterial.HasProperty("_SpecularColor")) specularColor = waterMaterial.GetColor("_SpecularColor");
            if (waterMaterial.HasProperty("_SpecularSize")) specularSize = waterMaterial.GetFloat("_SpecularSize");
            if (waterMaterial.HasProperty("_SparkleIntensity")) sparkleIntensity = waterMaterial.GetFloat("_SparkleIntensity");
            if (waterMaterial.HasProperty("_ShorelineZ")) shorelineZ = waterMaterial.GetFloat("_ShorelineZ");
            if (waterMaterial.HasProperty("_ShoreFoamWidth")) shoreFoamWidth = waterMaterial.GetFloat("_ShoreFoamWidth");

            Debug.Log("[OceanWaterFlowController] Read settings from Material asset successfully!");
        }

        [ContextMenu("Rebuild Subdivided Mesh")]
        public void RebuildWaterMesh()
        {
            generatedWaterMesh = null;
            EnsureSubdividedWaterMesh();
            Debug.Log("[OceanWaterFlowController] Rebuilt subdivided water mesh successfully!");
        }

        private static Color MultiplyColor(Color a, Color tint)
        {
            return new Color(
                Mathf.Clamp01(a.r * tint.r),
                Mathf.Clamp01(a.g * tint.g),
                Mathf.Clamp01(a.b * tint.b),
                a.a
            );
        }

        // =========================================================
        // 4. SUBDIVIDED WATER MESH & SLOPED OCEAN SEABED
        // =========================================================
        private void EnsureSubdividedWaterMesh()
        {
            if (meshFilter == null) return;
            if (generatedWaterMesh != null && meshFilter.sharedMesh == generatedWaterMesh) return;

            int nx = Mathf.Clamp(gridResolutionX, 16, 120);
            int nz = Mathf.Clamp(gridResolutionZ, 16, 120);

            generatedWaterMesh = new Mesh
            {
                name = "Procedural_SubdividedOceanWater"
            };

            int vertCount = (nx + 1) * (nz + 1);
            Vector3[] verts = new Vector3[vertCount];
            Vector3[] norms = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] tris = new int[nx * nz * 6];

            int vIdx = 0;
            for (int z = 0; z <= nz; z++)
            {
                float fz = (float)z / nz;
                float localZ = fz - 0.5f;
                for (int x = 0; x <= nx; x++)
                {
                    float fx = (float)x / nx;
                    float localX = fx - 0.5f;
                    verts[vIdx] = new Vector3(localX, 0.5f, localZ);
                    norms[vIdx] = Vector3.up;
                    uvs[vIdx] = new Vector2(fx, fz);
                    vIdx++;
                }
            }

            int tIdx = 0;
            for (int z = 0; z < nz; z++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int i00 = z * (nx + 1) + x;
                    int i10 = i00 + 1;
                    int i01 = (z + 1) * (nx + 1) + x;
                    int i11 = i01 + 1;

                    tris[tIdx++] = i00;
                    tris[tIdx++] = i01;
                    tris[tIdx++] = i10;

                    tris[tIdx++] = i10;
                    tris[tIdx++] = i01;
                    tris[tIdx++] = i11;
                }
            }

            generatedWaterMesh.vertices = verts;
            generatedWaterMesh.normals = norms;
            generatedWaterMesh.uv = uvs;
            generatedWaterMesh.triangles = tris;
            generatedWaterMesh.bounds = new Bounds(Vector3.zero, new Vector3(1.2f, 2.0f, 1.2f));

            meshFilter.sharedMesh = generatedWaterMesh;
        }

        private void EnsureOceanSeabed()
        {
            Transform seabedTrans = transform.Find("Ocean_Seabed");
            if (seabedTrans == null)
            {
                GameObject sbObj = new GameObject("Ocean_Seabed");
                sbObj.transform.SetParent(transform, false);
                seabedTrans = sbObj.transform;
            }

            seabedTrans.localPosition = Vector3.zero;
            seabedTrans.localRotation = Quaternion.identity;
            seabedTrans.localScale = Vector3.one;

            MeshFilter sbMf = seabedTrans.GetComponent<MeshFilter>();
            if (sbMf == null) sbMf = seabedTrans.gameObject.AddComponent<MeshFilter>();

            MeshRenderer sbMr = seabedTrans.GetComponent<MeshRenderer>();
            if (sbMr == null) sbMr = seabedTrans.gameObject.AddComponent<MeshRenderer>();

            // Sloped seabed deep enough (Y <= -1.35) so it never creates surface contour foam
            int nx = 24;
            int nz = 24;
            Mesh sbMesh = new Mesh { name = "Procedural_OceanSeabed" };
            Vector3[] verts = new Vector3[(nx + 1) * (nz + 1)];
            Vector3[] norms = new Vector3[verts.Length];
            Vector2[] uvs = new Vector2[verts.Length];
            Color[] colors = new Color[verts.Length];
            int[] tris = new int[nx * nz * 6];

            Color sandColor = new Color(0.12f, 0.56f, 0.68f, 1f);
            Color reefFloorColor = new Color(0.08f, 0.38f, 0.58f, 1f);
            Color abyssColor = new Color(0.04f, 0.18f, 0.38f, 1f);

            int idx = 0;
            for (int z = 0; z <= nz; z++)
            {
                float fz = (float)z / nz;
                float localZ = fz - 0.5f;

                float worldY = Mathf.Lerp(-1.35f, -2.2f, Mathf.SmoothStep(0f, 0.4f, fz));
                if (fz > 0.4f)
                {
                    worldY = Mathf.Lerp(-2.2f, -3.6f, Mathf.SmoothStep(0.4f, 1.0f, fz));
                }
                float localY = (worldY - transform.position.y) / Mathf.Max(0.05f, transform.localScale.y);

                for (int x = 0; x <= nx; x++)
                {
                    float fx = (float)x / nx;
                    float localX = fx - 0.5f;
                    verts[idx] = new Vector3(localX, localY, localZ);
                    norms[idx] = Vector3.up;
                    uvs[idx] = new Vector2(fx, fz);
                    colors[idx] = fz < 0.3f ? Color.Lerp(sandColor, reefFloorColor, fz / 0.3f)
                                            : Color.Lerp(reefFloorColor, abyssColor, (fz - 0.3f) / 0.7f);
                    idx++;
                }
            }

            int t = 0;
            for (int z = 0; z < nz; z++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int i00 = z * (nx + 1) + x;
                    int i10 = i00 + 1;
                    int i01 = (z + 1) * (nx + 1) + x;
                    int i11 = i01 + 1;

                    tris[t++] = i00;
                    tris[t++] = i01;
                    tris[t++] = i10;

                    tris[t++] = i10;
                    tris[t++] = i01;
                    tris[t++] = i11;
                }
            }

            sbMesh.vertices = verts;
            sbMesh.normals = norms;
            sbMesh.uv = uvs;
            sbMesh.colors = colors;
            sbMesh.triangles = tris;
            sbMesh.RecalculateBounds();
            sbMf.sharedMesh = sbMesh;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material sbMat = sbMr.sharedMaterial;
            if (sbMat == null || sbMat.name != "M_OceanSeabed_Runtime")
            {
                sbMat = new Material(litShader) { name = "M_OceanSeabed_Runtime" };
            }
            Color seabedTint = new Color(0.08f, 0.42f, 0.62f, 1f);
            if (sbMat.HasProperty("_BaseColor")) sbMat.SetColor("_BaseColor", seabedTint);
            if (sbMat.HasProperty("_Color")) sbMat.color = seabedTint;
            if (sbMat.HasProperty("_Smoothness")) sbMat.SetFloat("_Smoothness", 0.15f);
            sbMr.sharedMaterial = sbMat;
            sbMr.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void RemoveLegacyVFXArtifacts()
        {
            Transform vfxRoot = transform.Find("--- WATER FLOW & FOAM VFX ---");
            if (vfxRoot != null)
            {
                DestroyImmediate(vfxRoot.gameObject);
            }
        }

        // =========================================================
        // 5. FLOATING HARBOR BUOYS (Showcasing wave buoyancy & contour)
        // =========================================================
        private void EnsureFloatingBuoys()
        {
            Transform buoyGroup = transform.Find("--- FLOATING HARBOR BUOYS ---");
            if (buoyGroup != null)
            {
                DestroyImmediate(buoyGroup.gameObject);
            }

            GameObject groupObj = new GameObject("--- FLOATING HARBOR BUOYS ---");
            groupObj.transform.SetParent(transform, true);
            groupObj.transform.position = Vector3.zero;
            groupObj.transform.rotation = Quaternion.identity;

            // Channel Buoy Left (Red/White)
            CreateChannelBuoy(groupObj.transform, "Buoy_Channel_Left", new Vector3(-5.5f, baseWaterLevelY, 13.8f), new Color(0.95f, 0.25f, 0.20f));

            // Channel Buoy Right (Green/White)
            CreateChannelBuoy(groupObj.transform, "Buoy_Channel_Right", new Vector3(3.8f, baseWaterLevelY, 13.5f), new Color(0.18f, 0.82f, 0.45f));

            // Floating Wooden Crate
            CreateFloatingCrate(groupObj.transform, "Floating_Harbor_Crate", new Vector3(-2.2f, baseWaterLevelY, 13.2f));
        }

        private void CreateChannelBuoy(Transform parent, string name, Vector3 worldPos, Color accentColor)
        {
            GameObject buoy = new GameObject(name);
            buoy.transform.SetParent(parent, true);
            buoy.transform.position = worldPos;

            GameObject baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseRing.name = "Buoy_Base";
            baseRing.transform.SetParent(buoy.transform, false);
            baseRing.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            baseRing.transform.localScale = new Vector3(0.62f, 0.12f, 0.62f);
            AssignSimpleLitColor(baseRing, accentColor);
            DestroyColliderImmediate(baseRing);

            GameObject midStripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            midStripe.name = "Buoy_Stripe";
            midStripe.transform.SetParent(buoy.transform, false);
            midStripe.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            midStripe.transform.localScale = new Vector3(0.42f, 0.14f, 0.42f);
            AssignSimpleLitColor(midStripe, new Color(0.96f, 0.98f, 1.0f));
            DestroyColliderImmediate(midStripe);

            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Buoy_Mast";
            mast.transform.SetParent(buoy.transform, false);
            mast.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            mast.transform.localScale = new Vector3(0.16f, 0.22f, 0.16f);
            AssignSimpleLitColor(mast, accentColor);
            DestroyColliderImmediate(mast);

            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "Buoy_Beacon";
            beacon.transform.SetParent(buoy.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            beacon.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
            AssignSimpleLitColor(beacon, new Color(1.0f, 0.92f, 0.35f));
            DestroyColliderImmediate(beacon);

            var floater = buoy.AddComponent<WaterBuoyancyFloater>();
            floater.Configure(0.02f, 1.0f, 18.0f, false, 0.0f);
        }

        private void CreateFloatingCrate(Transform parent, string name, Vector3 worldPos)
        {
            GameObject crate = new GameObject(name);
            crate.transform.SetParent(parent, true);
            crate.transform.position = worldPos;
            crate.transform.rotation = Quaternion.Euler(0f, 28f, 0f);

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Wooden_Box";
            box.transform.SetParent(crate.transform, false);
            box.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            box.transform.localScale = new Vector3(0.52f, 0.36f, 0.52f);
            AssignSimpleLitColor(box, new Color(0.68f, 0.46f, 0.24f));
            DestroyColliderImmediate(box);

            var floater = crate.AddComponent<WaterBuoyancyFloater>();
            floater.Configure(-0.02f, 1.0f, 15.0f, false, 0.0f);
        }

        private static void AssignSimpleLitColor(GameObject go, Color col)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material m = new Material(s);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Color")) m.color = col;
            r.sharedMaterial = m;
        }

        private static void DestroyColliderImmediate(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);
        }

        // =========================================================
        // 6. WORLD THEME & DAY/NIGHT ADAPTATION
        // =========================================================
        private void OnWorldChanged(WorldData world)
        {
            if (world == null) return;
            Color target = Color.Lerp(Color.white, world.waterColor * 1.35f, 0.35f);
            target.a = 1f;
            worldTintMultiplier = target;
            SyncShaderParameters();
        }

        private void UpdateDayNightWaterMood()
        {
            if (DayNightCycleManager.Instance == null || waterMaterial == null) return;

            bool isNight = DayNightCycleManager.Instance.IsNight;
            DayPhase phase = DayNightCycleManager.Instance.CurrentPhase;

            Color targetShallow = shallowColor;
            Color targetBase = baseColor;
            Color targetDeep = deepColor;

            if (isNight)
            {
                targetShallow = new Color(0.08f, 0.55f, 0.72f, 0.78f);
                targetBase = new Color(0.05f, 0.34f, 0.62f, 0.90f);
                targetDeep = new Color(0.02f, 0.14f, 0.36f, 0.97f);
            }
            else if (phase == DayPhase.Sunset || phase == DayPhase.Dawn)
            {
                targetShallow = new Color(0.22f, 0.78f, 0.82f, 0.75f);
                targetBase = new Color(0.12f, 0.48f, 0.78f, 0.88f);
                targetDeep = new Color(0.06f, 0.22f, 0.52f, 0.96f);
            }

            waterMaterial.SetColor("_ShallowColor", MultiplyColor(targetShallow, worldTintMultiplier));
            waterMaterial.SetColor("_BaseColor", MultiplyColor(targetBase, worldTintMultiplier));
            waterMaterial.SetColor("_DeepColor", MultiplyColor(targetDeep, worldTintMultiplier));
        }
    }
}
