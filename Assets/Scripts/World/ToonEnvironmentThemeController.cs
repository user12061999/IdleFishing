using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using IdleFishing.Core;
using IdleFishing.Data;

namespace IdleFishing.World
{
    /// <summary>
    /// Synchronizes materials, decor, directional lighting, atmospheric fog,
    /// ambient illumination, and biome particle effects across the 5 Worlds / Biomes.
    /// Provides smooth cross-fade transitions when progressing through stages or voyaging.
    /// </summary>
    public class ToonEnvironmentThemeController : MonoBehaviour
    {
        public static ToonEnvironmentThemeController Instance { get; private set; }

        [Header("5 Season Materials")]
        [SerializeField] private Material springMaterial;
        [SerializeField] private Material summerMaterial;
        [SerializeField] private Material fallMaterial;
        [SerializeField] private Material winterMaterial;
        [SerializeField] private Material desertMaterial;

        [Header("5 Season Ground Asphalt Materials")]
        [SerializeField] private Material springAsphaltMaterial;
        [SerializeField] private Material summerAsphaltMaterial;
        [SerializeField] private Material fallAsphaltMaterial;
        [SerializeField] private Material winterAsphaltMaterial;
        [SerializeField] private Material desertAsphaltMaterial;

        [Header("Ground & Water Integration")]
        [SerializeField] private Renderer groundPlaneRenderer;

        [Header("5 Distinct Land Decor Groups")]
        [SerializeField] private GameObject decorLevel1_Temperate;
        [SerializeField] private GameObject decorLevel2_Tropical;
        [SerializeField] private GameObject decorLevel3_Autumn;
        [SerializeField] private GameObject decorLevel4_Arctic;
        [SerializeField] private GameObject decorLevel5_Desert;

        [Header("Atmospheric Lighting & Fog")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private bool enableAtmosphericFog = true;
        [SerializeField] private bool enableAmbientTransitions = true;
        [SerializeField] private float transitionDuration = 1.6f;

        [Header("5 Biome Atmosphere Particle Effects")]
        [SerializeField] private ParticleSystem biomeVfx1_Temperate;
        [SerializeField] private ParticleSystem biomeVfx2_Tropical;
        [SerializeField] private ParticleSystem biomeVfx3_Autumn;
        [SerializeField] private ParticleSystem biomeVfx4_Arctic;
        [SerializeField] private ParticleSystem biomeVfx5_Mystic;

        [Header("Managed Renderers")]
        [SerializeField] private List<Renderer> managedRenderers = new List<Renderer>();

        private Coroutine activeTransitionRoutine;
        private int currentActiveWorldOrder = 1;

        public int CurrentActiveWorldOrder => currentActiveWorldOrder;

        private void OnEnable()
        {
            GameEvents.WorldChanged += HandleWorldChanged;
        }

        private void OnDisable()
        {
            GameEvents.WorldChanged -= HandleWorldChanged;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsurePropsGrounded();
            ResolveDecorGroups();
            ResolveGroundRenderer();
            ResolveLightAndVfx();
        }

        private void Start()
        {
            EnsurePropsGrounded();
            ResolveDecorGroups();
            ResolveGroundRenderer();
            ResolveLightAndVfx();

            if (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null)
            {
                ApplyWorldTheme(WorldManager.Instance.CurrentWorld, immediate: true);
            }
            else
            {
                ApplyTheme(1, immediate: true);
            }
        }

        private void ResolveLightAndVfx()
        {
            if (directionalLight == null)
            {
                if (RenderSettings.sun != null) directionalLight = RenderSettings.sun;
                else
                {
                    var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                    for (int i = 0; i < lights.Length; i++)
                    {
                        if (lights[i].type == LightType.Directional)
                        {
                            directionalLight = lights[i];
                            break;
                        }
                    }
                }
            }

            Transform vfxParent = transform.Find("--- BIOME ATMOSPHERE VFX ---");
            if (vfxParent != null)
            {
                if (biomeVfx1_Temperate == null) biomeVfx1_Temperate = vfxParent.Find("VFX_Biome1_TemperatePollen")?.GetComponent<ParticleSystem>();
                if (biomeVfx2_Tropical == null) biomeVfx2_Tropical = vfxParent.Find("VFX_Biome2_TropicalSparkles")?.GetComponent<ParticleSystem>();
                if (biomeVfx3_Autumn == null) biomeVfx3_Autumn = vfxParent.Find("VFX_Biome3_AutumnLeaves")?.GetComponent<ParticleSystem>();
                if (biomeVfx4_Arctic == null) biomeVfx4_Arctic = vfxParent.Find("VFX_Biome4_ArcticSnow")?.GetComponentInChildren<ParticleSystem>();
                if (biomeVfx5_Mystic == null) biomeVfx5_Mystic = vfxParent.Find("VFX_Biome5_MysticMotes")?.GetComponent<ParticleSystem>();
            }
        }

        private void ResolveGroundRenderer()
        {
            if (groundPlaneRenderer == null)
            {
                var g = GameObject.Find("Ground Plane");
                if (g != null) groundPlaneRenderer = g.GetComponent<Renderer>();
            }
        }

        public void RegisterDecorGroups(
            GameObject d1, GameObject d2, GameObject d3, GameObject d4, GameObject d5)
        {
            decorLevel1_Temperate = d1;
            decorLevel2_Tropical = d2;
            decorLevel3_Autumn = d3;
            decorLevel4_Arctic = d4;
            decorLevel5_Desert = d5;
        }

        public void RegisterGroundRenderer(Renderer gRenderer)
        {
            groundPlaneRenderer = gRenderer;
        }

        private void ResolveDecorGroups()
        {
            if (decorLevel1_Temperate == null)
            {
                var t = transform.Find("--- DECOR LEVEL 1 (TEMPERATE) ---");
                if (t != null) decorLevel1_Temperate = t.gameObject;
            }
            if (decorLevel2_Tropical == null)
            {
                var t = transform.Find("--- DECOR LEVEL 2 (TROPICAL CORAL) ---");
                if (t != null) decorLevel2_Tropical = t.gameObject;
            }
            if (decorLevel3_Autumn == null)
            {
                var t = transform.Find("--- DECOR LEVEL 3 (GOLDEN AUTUMN) ---");
                if (t != null) decorLevel3_Autumn = t.gameObject;
            }
            if (decorLevel4_Arctic == null)
            {
                var t = transform.Find("--- DECOR LEVEL 4 (ARCTIC FROST) ---");
                if (t != null) decorLevel4_Arctic = t.gameObject;
            }
            if (decorLevel5_Desert == null)
            {
                var t = transform.Find("--- DECOR LEVEL 5 (DESERT OASIS) ---");
                if (t != null) decorLevel5_Desert = t.gameObject;
            }
        }

        [ContextMenu("Ensure Props Grounded")]
        public void EnsurePropsGrounded()
        {
            var treesGroup = transform.Find("--- FORESTS & COASTAL TREES ---");
            if (treesGroup != null)
            {
                for (int i = 0; i < treesGroup.childCount; i++)
                {
                    var child = treesGroup.GetChild(i);
                    if (child.position.y > -0.5f)
                    {
                        float scale = child.localScale.y;
                        Vector3 pos = child.position;
                        pos.y = 0.0f - (1.0f * scale) - 0.05f;
                        child.position = pos;
                    }
                }
            }

            var cabinsGroup = transform.Find("--- CABINS & OUTPOSTS ---");
            if (cabinsGroup != null)
            {
                for (int i = 0; i < cabinsGroup.childCount; i++)
                {
                    var child = cabinsGroup.GetChild(i);
                    if (child.position.y > -0.5f)
                    {
                        float scale = child.localScale.y;
                        Vector3 pos = child.position;
                        pos.y = 0.0f - (1.0f * scale) - 0.02f;
                        child.position = pos;
                    }
                }
            }
        }

        private void HandleWorldChanged(WorldData newWorld)
        {
            ApplyWorldTheme(newWorld, immediate: false);
        }

        public void ApplyTheme(int order, bool immediate = false)
        {
            var fakeWorld = ScriptableObject.CreateInstance<WorldData>();
            fakeWorld.worldOrder = order;
            fakeWorld.worldName = order switch
            {
                1 => "Temperate Bay",
                2 => "Tropical Coral",
                3 => "Golden Autumn Cove",
                4 => "Arctic Frost Haven",
                5 => "Sunken Oasis Desert",
                _ => "Unknown Realm"
            };
            ApplyWorldTheme(fakeWorld, immediate);
            if (Application.isPlaying) Destroy(fakeWorld);
            else DestroyImmediate(fakeWorld);
        }

        public void ApplyWorldTheme(WorldData world, bool immediate = false)
        {
            if (world == null) return;

            ResolveDecorGroups();
            ResolveGroundRenderer();
            ResolveLightAndVfx();

            int targetOrder = Mathf.Clamp(world.worldOrder, 1, 5);

            if (immediate || !Application.isPlaying)
            {
                ApplyWorldInstant(targetOrder, world.worldName);
            }
            else
            {
                if (activeTransitionRoutine != null) StopCoroutine(activeTransitionRoutine);
                activeTransitionRoutine = StartCoroutine(TransitionEnvironmentRoutine(targetOrder, world.worldName, transitionDuration));
            }
        }

        private void ApplyWorldInstant(int order, string worldName)
        {
            currentActiveWorldOrder = order;

            // 1. Materials & Decor
            Material targetMat = GetMaterialForOrder(order);
            SetMaterialOnAll(targetMat);

            if (groundPlaneRenderer != null && targetMat != null)
            {
                groundPlaneRenderer.sharedMaterial = targetMat;
            }

            SetDecorActive(decorLevel1_Temperate, order == 1);
            SetDecorActive(decorLevel2_Tropical, order == 2);
            SetDecorActive(decorLevel3_Autumn, order == 3);
            SetDecorActive(decorLevel4_Arctic, order == 4);
            SetDecorActive(decorLevel5_Desert, order == 5);

            // 2. Water Preset
            ApplyWaterPresetForLevel(order);

            // 3. Lighting & Fog
            var profile = GetAtmosphereProfile(order);
            if (directionalLight != null)
            {
                directionalLight.color = profile.sunColor;
                directionalLight.intensity = profile.sunIntensity;
                directionalLight.transform.rotation = Quaternion.Euler(profile.sunRotation);
            }

            if (enableAtmosphericFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = profile.fogColor;
                RenderSettings.fogStartDistance = profile.fogStart;
                RenderSettings.fogEndDistance = profile.fogEnd;
            }

            if (enableAmbientTransitions)
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = profile.ambientSky;
                RenderSettings.ambientEquatorColor = profile.ambientEquator;
                RenderSettings.ambientGroundColor = profile.ambientGround;
            }

            // 4. Biome Atmosphere Particles
            UpdateVfxEmission(order);

            // 5. Travel Ship Visual
            var ship = OffshoreTravelShip.Instance != null ? OffshoreTravelShip.Instance : FindFirstObjectByType<OffshoreTravelShip>();
            if (ship != null)
            {
                ship.SetShipVisualForWorld(order);
            }

            Debug.Log($"[ToonTheme] Instant applied Theme & Environment for World {order}: {worldName}");
        }

        private IEnumerator TransitionEnvironmentRoutine(int targetOrder, string worldName, float duration)
        {
            currentActiveWorldOrder = targetOrder;
            float elapsed = 0f;

            // Initial parameters
            Color startSunColor = directionalLight != null ? directionalLight.color : Color.white;
            float startSunIntensity = directionalLight != null ? directionalLight.intensity : 1f;
            Quaternion startSunRot = directionalLight != null ? directionalLight.transform.rotation : Quaternion.identity;

            Color startFogColor = RenderSettings.fogColor;
            float startFogStart = RenderSettings.fogStartDistance;
            float startFogEnd = RenderSettings.fogEndDistance;

            Color startSky = RenderSettings.ambientSkyColor;
            Color startEquator = RenderSettings.ambientEquatorColor;
            Color startGround = RenderSettings.ambientGroundColor;

            var targetProfile = GetAtmosphereProfile(targetOrder);
            Quaternion targetSunRot = Quaternion.Euler(targetProfile.sunRotation);

            // Turn on fog mode
            if (enableAtmosphericFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
            }
            if (enableAmbientTransitions)
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
            }

            // Material & Decor swap at midpoint
            bool swappedDecor = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (directionalLight != null)
                {
                    directionalLight.color = Color.Lerp(startSunColor, targetProfile.sunColor, t);
                    directionalLight.intensity = Mathf.Lerp(startSunIntensity, targetProfile.sunIntensity, t);
                    directionalLight.transform.rotation = Quaternion.Slerp(startSunRot, targetSunRot, t);
                }

                if (enableAtmosphericFog)
                {
                    RenderSettings.fogColor = Color.Lerp(startFogColor, targetProfile.fogColor, t);
                    RenderSettings.fogStartDistance = Mathf.Lerp(startFogStart, targetProfile.fogStart, t);
                    RenderSettings.fogEndDistance = Mathf.Lerp(startFogEnd, targetProfile.fogEnd, t);
                }

                if (enableAmbientTransitions)
                {
                    RenderSettings.ambientSkyColor = Color.Lerp(startSky, targetProfile.ambientSky, t);
                    RenderSettings.ambientEquatorColor = Color.Lerp(startEquator, targetProfile.ambientEquator, t);
                    RenderSettings.ambientGroundColor = Color.Lerp(startGround, targetProfile.ambientGround, t);
                }

                if (!swappedDecor && t >= 0.45f)
                {
                    swappedDecor = true;
                    Material targetMat = GetMaterialForOrder(targetOrder);
                    SetMaterialOnAll(targetMat);
                    if (groundPlaneRenderer != null && targetMat != null)
                    {
                        groundPlaneRenderer.sharedMaterial = targetMat;
                    }

                    SetDecorActive(decorLevel1_Temperate, targetOrder == 1);
                    SetDecorActive(decorLevel2_Tropical, targetOrder == 2);
                    SetDecorActive(decorLevel3_Autumn, targetOrder == 3);
                    SetDecorActive(decorLevel4_Arctic, targetOrder == 4);
                    SetDecorActive(decorLevel5_Desert, targetOrder == 5);

                    ApplyWaterPresetForLevel(targetOrder);
                    UpdateVfxEmission(targetOrder);

                    var ship = OffshoreTravelShip.Instance != null ? OffshoreTravelShip.Instance : FindFirstObjectByType<OffshoreTravelShip>();
                    if (ship != null) ship.SetShipVisualForWorld(targetOrder);
                }

                yield return null;
            }

            // Snap final values
            if (directionalLight != null)
            {
                directionalLight.color = targetProfile.sunColor;
                directionalLight.intensity = targetProfile.sunIntensity;
                directionalLight.transform.rotation = targetSunRot;
            }
            if (enableAtmosphericFog)
            {
                RenderSettings.fogColor = targetProfile.fogColor;
                RenderSettings.fogStartDistance = targetProfile.fogStart;
                RenderSettings.fogEndDistance = targetProfile.fogEnd;
            }

            activeTransitionRoutine = null;
            Debug.Log($"[ToonTheme] Smooth environment transition complete to World {targetOrder}: {worldName}");
        }

        private void UpdateVfxEmission(int activeOrder)
        {
            SetParticleActive(biomeVfx1_Temperate, activeOrder == 1);
            SetParticleActive(biomeVfx2_Tropical, activeOrder == 2);
            SetParticleActive(biomeVfx3_Autumn, activeOrder == 3);
            SetParticleActive(biomeVfx4_Arctic, activeOrder == 4);
            SetParticleActive(biomeVfx5_Mystic, activeOrder == 5);
        }

        private void SetParticleActive(ParticleSystem ps, bool active)
        {
            if (ps == null) return;
            if (active)
            {
                if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);
                if (!ps.isPlaying) ps.Play();
                var em = ps.emission;
                em.enabled = true;
            }
            else
            {
                var em = ps.emission;
                em.enabled = false;
                // Deactivate completely after lingering particles finish
                if (ps.gameObject.activeSelf && Application.isPlaying)
                {
                    StartCoroutine(DelayedDeactivateRoutine(ps.gameObject, 4.0f));
                }
                else
                {
                    ps.gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator DelayedDeactivateRoutine(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (go != null && !go.GetComponent<ParticleSystem>().emission.enabled)
            {
                go.SetActive(false);
            }
        }

        private Material GetMaterialForOrder(int order)
        {
            return order switch
            {
                1 => springMaterial != null ? springMaterial : springMaterial,
                2 => summerMaterial != null ? summerMaterial : springMaterial,
                3 => fallMaterial != null ? fallMaterial : springMaterial,
                4 => winterMaterial != null ? winterMaterial : springMaterial,
                5 => desertMaterial != null ? desertMaterial : springMaterial,
                _ => springMaterial
            };
        }

        private AtmosphereProfile GetAtmosphereProfile(int order)
        {
            return order switch
            {
                1 => new AtmosphereProfile // World 1 - Temperate Bay (Crisp morning daylight)
                {
                    sunColor = new Color(1.0f, 0.96f, 0.88f, 1f),
                    sunIntensity = 1.15f,
                    sunRotation = new Vector3(45f, 135f, 0f),
                    fogColor = new Color(0.72f, 0.86f, 0.96f, 1f),
                    fogStart = 35f,
                    fogEnd = 120f,
                    ambientSky = new Color(0.36f, 0.48f, 0.62f, 1f),
                    ambientEquator = new Color(0.24f, 0.36f, 0.46f, 1f),
                    ambientGround = new Color(0.16f, 0.22f, 0.18f, 1f)
                },
                2 => new AtmosphereProfile // World 2 - Tropical Coral (Vibrant noon sun, crystal clear horizon)
                {
                    sunColor = new Color(1.0f, 0.98f, 0.82f, 1f),
                    sunIntensity = 1.25f,
                    sunRotation = new Vector3(55f, 125f, 0f),
                    fogColor = new Color(0.66f, 0.94f, 0.97f, 1f),
                    fogStart = 45f,
                    fogEnd = 140f,
                    ambientSky = new Color(0.42f, 0.58f, 0.68f, 1f),
                    ambientEquator = new Color(0.28f, 0.46f, 0.54f, 1f),
                    ambientGround = new Color(0.24f, 0.35f, 0.28f, 1f)
                },
                3 => new AtmosphereProfile // World 3 - Golden Autumn (Warm amber sunset glow)
                {
                    sunColor = new Color(1.0f, 0.80f, 0.55f, 1f),
                    sunIntensity = 1.20f,
                    sunRotation = new Vector3(34f, 165f, 0f),
                    fogColor = new Color(0.92f, 0.74f, 0.52f, 1f),
                    fogStart = 28f,
                    fogEnd = 95f,
                    ambientSky = new Color(0.55f, 0.40f, 0.30f, 1f),
                    ambientEquator = new Color(0.38f, 0.28f, 0.20f, 1f),
                    ambientGround = new Color(0.22f, 0.16f, 0.10f, 1f)
                },
                4 => new AtmosphereProfile // World 4 - Arctic Frost (Pale glacial ice-blue, dense frost mist)
                {
                    sunColor = new Color(0.85f, 0.92f, 1.0f, 1f),
                    sunIntensity = 1.15f,
                    sunRotation = new Vector3(38f, 115f, 0f),
                    fogColor = new Color(0.82f, 0.90f, 0.98f, 1f),
                    fogStart = 20f,
                    fogEnd = 75f,
                    ambientSky = new Color(0.45f, 0.52f, 0.65f, 1f),
                    ambientEquator = new Color(0.32f, 0.38f, 0.48f, 1f),
                    ambientGround = new Color(0.25f, 0.30f, 0.38f, 1f)
                },
                5 => new AtmosphereProfile // World 5 - Mythic Maelstrom / Sunken Oasis (Mystical emerald twilight)
                {
                    sunColor = new Color(0.78f, 0.96f, 0.90f, 1f),
                    sunIntensity = 1.22f,
                    sunRotation = new Vector3(36f, 180f, 0f),
                    fogColor = new Color(0.38f, 0.70f, 0.65f, 1f),
                    fogStart = 22f,
                    fogEnd = 80f,
                    ambientSky = new Color(0.30f, 0.52f, 0.50f, 1f),
                    ambientEquator = new Color(0.20f, 0.38f, 0.36f, 1f),
                    ambientGround = new Color(0.12f, 0.24f, 0.22f, 1f)
                },
                _ => new AtmosphereProfile()
            };
        }

        private void ApplyWaterPresetForLevel(int order)
        {
            if (OceanWaterFlowController.Instance == null) return;

            switch (order)
            {
                case 1: // Level 1 - Temperate Bay: Fresh cool blue water, gentle waves
                    OceanWaterFlowController.Instance.ApplyWaterProfile(
                        shallow: new Color(0.22f, 0.72f, 0.90f, 0.75f),
                        baseCol: new Color(0.10f, 0.50f, 0.78f, 0.88f),
                        deep: new Color(0.05f, 0.25f, 0.52f, 0.96f),
                        foam: new Color(0.98f, 0.99f, 1.0f, 0.96f),
                        caustic: new Color(0.60f, 0.90f, 1.0f, 0.28f),
                        maxDepth: 2.6f,
                        waveAmp: 0.052f
                    );
                    break;

                case 2: // Level 2 - Tropical Coral Atoll: Vibrant turquoise / cyan lagoon
                    OceanWaterFlowController.Instance.ApplyWaterProfile(
                        shallow: new Color(0.12f, 0.96f, 0.92f, 0.52f),
                        baseCol: new Color(0.04f, 0.78f, 0.84f, 0.82f),
                        deep: new Color(0.02f, 0.42f, 0.68f, 0.94f),
                        foam: new Color(0.92f, 1.0f, 0.98f, 0.92f),
                        caustic: new Color(0.85f, 1.0f, 0.95f, 0.55f),
                        maxDepth: 3.8f,
                        waveAmp: 0.038f
                    );
                    break;

                case 3: // Level 3 - Autumn Cove: Crystal clear sapphire cove with golden sunlight caustics
                    OceanWaterFlowController.Instance.ApplyWaterProfile(
                        shallow: new Color(0.16f, 0.70f, 0.88f, 0.55f),
                        baseCol: new Color(0.08f, 0.48f, 0.72f, 0.82f),
                        deep: new Color(0.04f, 0.25f, 0.50f, 0.94f),
                        foam: new Color(0.96f, 0.98f, 1.0f, 0.94f),
                        caustic: new Color(0.96f, 0.88f, 0.55f, 0.50f),
                        maxDepth: 3.6f,
                        waveAmp: 0.042f
                    );
                    break;

                case 4: // Level 4 - Arctic Frost: Glacial ice-blue water revealing submerged ice shelves
                    OceanWaterFlowController.Instance.ApplyWaterProfile(
                        shallow: new Color(0.18f, 0.82f, 0.95f, 0.52f),
                        baseCol: new Color(0.06f, 0.45f, 0.72f, 0.84f),
                        deep: new Color(0.02f, 0.20f, 0.48f, 0.95f),
                        foam: new Color(1.0f, 1.0f, 1.0f, 0.98f),
                        caustic: new Color(0.85f, 0.95f, 1.0f, 0.55f),
                        maxDepth: 3.8f,
                        waveAmp: 0.045f
                    );
                    break;

                case 5: // Level 5 - Sunken Oasis / Mythic: Emerald green oasis reflection & deep trench
                    OceanWaterFlowController.Instance.ApplyWaterProfile(
                        shallow: new Color(0.14f, 0.88f, 0.80f, 0.60f),
                        baseCol: new Color(0.08f, 0.68f, 0.64f, 0.85f),
                        deep: new Color(0.03f, 0.38f, 0.48f, 0.96f),
                        foam: new Color(0.96f, 1.0f, 0.95f, 0.92f),
                        caustic: new Color(0.95f, 0.98f, 0.70f, 0.48f),
                        maxDepth: 3.2f,
                        waveAmp: 0.042f
                    );
                    break;
            }
        }

        private void SetDecorActive(GameObject decorObj, bool active)
        {
            if (decorObj != null && decorObj.activeSelf != active)
            {
                decorObj.SetActive(active);
            }
        }

        public void SetMaterialOnAll(Material mat)
        {
            if (mat == null) return;
            for (int i = 0; i < managedRenderers.Count; i++)
            {
                if (managedRenderers[i] != null)
                {
                    managedRenderers[i].sharedMaterial = mat;
                }
            }
        }

        [ContextMenu("Collect All Child Renderers")]
        public void CollectAllRenderers()
        {
            managedRenderers.Clear();
            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (r != null && r.sharedMaterial != null && !r.sharedMaterial.name.StartsWith("Color") && !r.sharedMaterial.name.StartsWith("fx_"))
                {
                    managedRenderers.Add(r);
                }
            }
        }

        public void SetThemeMaterials(
            Material spring, Material summer, Material fall, Material winter, Material desert,
            Material springAsphalt = null, Material summerAsphalt = null, Material fallAsphalt = null, Material winterAsphalt = null, Material desertAsphalt = null)
        {
            springMaterial = spring;
            summerMaterial = summer;
            fallMaterial = fall;
            winterMaterial = winter;
            desertMaterial = desert;

            springAsphaltMaterial = springAsphalt;
            summerAsphaltMaterial = summerAsphalt;
            fallAsphaltMaterial = fallAsphalt;
            winterAsphaltMaterial = winterAsphalt;
            desertAsphaltMaterial = desertAsphalt;
        }

        private struct AtmosphereProfile
        {
            public Color sunColor;
            public float sunIntensity;
            public Vector3 sunRotation;
            public Color fogColor;
            public float fogStart;
            public float fogEnd;
            public Color ambientSky;
            public Color ambientEquator;
            public Color ambientGround;
        }
    }
}
