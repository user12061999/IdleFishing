using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;

namespace IdleFishing.VFX
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Water VFX Prefabs")]
        [SerializeField] private GameObject waterSplashPrefab;
        [SerializeField] private GameObject waterRipplePrefab;
        [SerializeField] private GameObject cfxrWaterRipplePrefab;
        [SerializeField] private GameObject cfxrWaterRippleLoopPrefab;

        [Header("Group 1: Dock & Combat VFX")]
        [SerializeField] private GameObject stunStarsPrefab;
        [SerializeField] private GameObject hitPunchPrefab;
        [SerializeField] private GameObject dustPuffPrefab;

        [Header("Group 2: Kitchen & Market VFX")]
        [SerializeField] private GameObject swordSlashPrefab;
        [SerializeField] private GameObject smokeWhitePrefab;
        [SerializeField] private GameObject heartPoofPrefab;

        [Header("Group 3: Rewards & Milestones VFX")]
        [SerializeField] private GameObject confettiPrefab;
        [SerializeField] private GameObject levelUpPrefab;

        [Header("Shared Particle Assets")]
        [SerializeField] private Texture2D defaultParticleTex;

        private Material particleMaterial;
        private ParticleSystem splashPool;
        private ParticleSystem hitImpactPool;
        private ParticleSystem goldBurstPool;
        private GameObject frenzyAuraObj;
        private ParticleSystem frenzyAuraSystem;

        public Material SharedParticleMaterial => particleMaterial;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            LoadPrefabsInEditor();
#endif

            CreateSharedParticleMaterial();
            BuildParticleSystems();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            LoadPrefabsInEditor();
        }

        public void LoadPrefabsInEditor()
        {
            if (waterSplashPrefab == null) waterSplashPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Water/WaterSplashSoft.prefab");
            if (waterRipplePrefab == null) waterRipplePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Water/WaterRippleSoft.prefab");
            if (cfxrWaterRipplePrefab == null) cfxrWaterRipplePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Liquids/CFXR Water Ripples.prefab");
            if (cfxrWaterRippleLoopPrefab == null) cfxrWaterRippleLoopPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Liquids/Variants/CFXR Water Ripples (Loop).prefab");
            if (stunStarsPrefab == null) stunStarsPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Combat/StunnedCirclingStars.prefab");
            if (hitPunchPrefab == null) hitPunchPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Combat/ToonPunchMedium.prefab");
            if (dustPuffPrefab == null) dustPuffPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Combat/DustDirtyPoofSoft.prefab");
            if (swordSlashPrefab == null) swordSlashPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Kitchen/SwordSlashThickWhite.prefab");
            if (smokeWhitePrefab == null) smokeWhitePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Kitchen/SmokeWhiteSoft.prefab");
            if (heartPoofPrefab == null) heartPoofPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Kitchen/HeartPoof.prefab");
            if (confettiPrefab == null) confettiPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Rewards/ConfettiBlastRainbow.prefab");
            if (levelUpPrefab == null) levelUpPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Rewards/LevelupNovaYellow.prefab");
            if (defaultParticleTex == null) defaultParticleTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/VFX/Shared/Textures/magic_orb2.png");
        }
#endif

        private void Start()
        {
            PrewarmPools();

            GameEvents.FrenzyStarted += OnFrenzyStarted;
            GameEvents.FrenzyEnded += OnFrenzyEnded;
            GameEvents.TreasureChestOpened += OnChestOpened;
            GameEvents.PrestigeCompleted += OnPrestigeCompleted;
            GameEvents.UpgradePurchased += OnUpgradePurchased;
        }

        private void PrewarmPools()
        {
            if (ObjectPoolManager.Instance == null) return;

            ObjectPoolManager.Instance.Prewarm(waterSplashPrefab, 10);
            ObjectPoolManager.Instance.Prewarm(waterRipplePrefab, 10);
            ObjectPoolManager.Instance.Prewarm(cfxrWaterRipplePrefab, 10);
            ObjectPoolManager.Instance.Prewarm(cfxrWaterRippleLoopPrefab, 4);
            ObjectPoolManager.Instance.Prewarm(stunStarsPrefab, 5);
            ObjectPoolManager.Instance.Prewarm(hitPunchPrefab, 15);
            ObjectPoolManager.Instance.Prewarm(dustPuffPrefab, 10);
            ObjectPoolManager.Instance.Prewarm(swordSlashPrefab, 15);
            ObjectPoolManager.Instance.Prewarm(smokeWhitePrefab, 5);
            ObjectPoolManager.Instance.Prewarm(heartPoofPrefab, 10);
            ObjectPoolManager.Instance.Prewarm(confettiPrefab, 5);
            ObjectPoolManager.Instance.Prewarm(levelUpPrefab, 3);
        }

        private void OnDestroy()
        {
            GameEvents.FrenzyStarted -= OnFrenzyStarted;
            GameEvents.FrenzyEnded -= OnFrenzyEnded;
            GameEvents.TreasureChestOpened -= OnChestOpened;
            GameEvents.PrestigeCompleted -= OnPrestigeCompleted;
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
        }

        private void CreateSharedParticleMaterial()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                       ?? Shader.Find("Particles/Standard Unlit") 
                       ?? Shader.Find("Mobile/Particles/Additive")
                       ?? Shader.Find("Sprites/Default");

            particleMaterial = new Material(s);
            particleMaterial.name = "VFX_SharedParticleMaterial_URP";

            Texture2D tex = defaultParticleTex != null ? defaultParticleTex : GenerateSoftCircleTexture();

            // Configure proper URP Transparency & Additive Blending
            if (particleMaterial.HasProperty("_Surface")) particleMaterial.SetFloat("_Surface", 1f); // 1 = Transparent
            if (particleMaterial.HasProperty("_Blend")) particleMaterial.SetFloat("_Blend", 2f); // 2 = Additive in URP
            if (particleMaterial.HasProperty("_SrcBlend")) particleMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); // 5
            if (particleMaterial.HasProperty("_DstBlend")) particleMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One); // 1
            if (particleMaterial.HasProperty("_SrcBlendAlpha")) particleMaterial.SetFloat("_SrcBlendAlpha", 1f);
            if (particleMaterial.HasProperty("_DstBlendAlpha")) particleMaterial.SetFloat("_DstBlendAlpha", 1f);
            if (particleMaterial.HasProperty("_ZWrite")) particleMaterial.SetFloat("_ZWrite", 0f);
            if (particleMaterial.HasProperty("_Cull")) particleMaterial.SetFloat("_Cull", 0f);
            if (particleMaterial.HasProperty("_SoftParticlesEnabled")) particleMaterial.SetFloat("_SoftParticlesEnabled", 0f);

            particleMaterial.renderQueue = 3000;
            particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            particleMaterial.DisableKeyword("_SOFTPARTICLES_ON");
            particleMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            if (particleMaterial.HasProperty("_BaseMap")) particleMaterial.SetTexture("_BaseMap", tex);
            if (particleMaterial.HasProperty("_MainTex")) particleMaterial.SetTexture("_MainTex", tex);
            if (particleMaterial.HasProperty("_BaseColor")) particleMaterial.SetColor("_BaseColor", Color.white);
            if (particleMaterial.HasProperty("_Color")) particleMaterial.color = Color.white;
        }

        private Texture2D GenerateSoftCircleTexture()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "SoftCircleTex_Procedural";
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] cols = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxRadius = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxRadius;
                    float a = Mathf.Clamp01(1.0f - dist);
                    a = a * a; // Smooth quadratic falloff
                    cols[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private void BuildParticleSystems()
        {
            // 1. Water Splash System
            GameObject splashObj = new GameObject("VFX_SplashSystem");
            splashObj.transform.SetParent(transform);
            splashPool = splashObj.AddComponent<ParticleSystem>();
            ConfigureParticleSystem(splashPool, new Color(0.6f, 0.9f, 1f, 0.9f), 0.5f, 2.8f, 0.35f, 25);

            // 2. Hit Impact / Meat Bits System (Snappy combat sparks/bits)
            GameObject hitObj = new GameObject("VFX_HitImpactSystem");
            hitObj.transform.SetParent(transform);
            hitImpactPool = hitObj.AddComponent<ParticleSystem>();
            ConfigureParticleSystem(hitImpactPool, new Color(1f, 0.35f, 0.2f, 0.95f), 0.35f, 3.2f, 0.28f, 20);

            // 3. Gold Sparkle Burst System
            GameObject goldObj = new GameObject("VFX_GoldBurstSystem");
            goldObj.transform.SetParent(transform);
            goldBurstPool = goldObj.AddComponent<ParticleSystem>();
            ConfigureParticleSystem(goldBurstPool, new Color(1f, 0.88f, 0.2f, 1f), 0.7f, 3.5f, 0.45f, 35);

            // 4. Frenzy Aura System (Rising glowing golden aura at the dock)
            frenzyAuraObj = new GameObject("VFX_FrenzyAura");
            frenzyAuraObj.transform.SetParent(transform);
            frenzyAuraObj.transform.position = new Vector3(0f, 0.12f, 1.5f);
            frenzyAuraSystem = frenzyAuraObj.AddComponent<ParticleSystem>();

            var main = frenzyAuraSystem.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 2.4f;
            main.startSize = 0.42f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.92f, 0.35f, 0.9f), new Color(1f, 0.45f, 0.1f, 0.85f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = frenzyAuraSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8.5f, 0.2f, 4.0f);

            var emission = frenzyAuraSystem.emission;
            emission.rateOverTime = 38;

            var vel = frenzyAuraSystem.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            vel.y = new ParticleSystem.MinMaxCurve(1.8f, 2.6f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

            // Size animation: grow then taper
            var sol = frenzyAuraSystem.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve auraSizeCurve = new AnimationCurve();
            auraSizeCurve.AddKey(0f, 0.25f);
            auraSizeCurve.AddKey(0.35f, 1.0f);
            auraSizeCurve.AddKey(1f, 0.15f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, auraSizeCurve);

            // Golden fire fade
            var col = frenzyAuraSystem.colorOverLifetime;
            col.enabled = true;
            Gradient auraGrad = new Gradient();
            auraGrad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 0.95f, 0.4f), 0f), 
                    new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0.6f),
                    new GradientColorKey(new Color(1f, 0.2f, 0.05f), 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.1f, 0f), 
                    new GradientAlphaKey(0.95f, 0.25f), 
                    new GradientAlphaKey(0.6f, 0.7f), 
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            col.color = auraGrad;

            var ren = frenzyAuraObj.GetComponent<ParticleSystemRenderer>();
            ren.sharedMaterial = particleMaterial;

            frenzyAuraObj.SetActive(false);
        }

        private void ConfigureParticleSystem(ParticleSystem ps, Color baseColor, float lifeTime, float speed, float size, int burstCount)
        {
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = lifeTime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = baseColor;
            main.gravityModifier = 1.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var emission = ps.emission;
            emission.rateOverTime = 0; // Pure burst on demand

            // Shrink over lifetime so particles don't stay square/static
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve shrinkCurve = new AnimationCurve();
            shrinkCurve.AddKey(0f, 1f);
            shrinkCurve.AddKey(1f, 0.1f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, shrinkCurve);

            // Alpha fade out over lifetime
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var ren = ps.GetComponent<ParticleSystemRenderer>();
            ren.sharedMaterial = particleMaterial;
        }

        // ==================== PUBLIC API ====================
        public void SpawnSplash(Vector3 position, float scale = 1.0f)
        {
            if (position.z > 9.5f && IdleFishing.World.OceanWaterFlowController.Instance != null)
            {
                IdleFishing.World.OceanWaterFlowController.Instance.RegisterRipple(position, 2.2f * scale, 1.05f, 12.0f, 1.9f);
            }

            if (waterSplashPrefab != null)
            {
                if (ObjectPoolManager.Instance != null)
                {
                    var splash = ObjectPoolManager.Instance.SpawnAutoRelease(waterSplashPrefab, position, Quaternion.identity, 1.8f);
                    if (splash != null) splash.transform.localScale = Vector3.one * scale;
                }
                else
                {
                    var splash = Instantiate(waterSplashPrefab, position, Quaternion.identity);
                    splash.transform.localScale = Vector3.one * scale;
                    Destroy(splash, 1.8f);
                }
            }
            else if (splashPool != null)
            {
                splashPool.transform.position = position;
                splashPool.Emit(16);
            }
        }

        public void SpawnWaterRipple(Vector3 position, float scale = 1.0f)
        {
            if (IdleFishing.World.OceanWaterFlowController.Instance != null)
            {
                IdleFishing.World.OceanWaterFlowController.Instance.RegisterRipple(position, 1.9f * scale, 0.9f, 11.5f, 1.8f);
            }

            GameObject prefab = cfxrWaterRipplePrefab != null ? cfxrWaterRipplePrefab : waterRipplePrefab;
            if (prefab != null)
            {
                Vector3 spawnPos = position + Vector3.up * 0.05f;
                if (ObjectPoolManager.Instance != null)
                {
                    var ripple = ObjectPoolManager.Instance.SpawnAutoRelease(prefab, spawnPos, Quaternion.identity, 2.0f);
                    if (ripple != null) ripple.transform.localScale = Vector3.one * scale;
                }
                else
                {
                    var ripple = Instantiate(prefab, spawnPos, Quaternion.identity);
                    ripple.transform.localScale = Vector3.one * scale;
                    Destroy(ripple, 2.0f);
                }
            }
        }

        public GameObject SpawnCFXRWaterRipple(Vector3 position, float scale = 1.0f)
        {
            if (IdleFishing.World.OceanWaterFlowController.Instance != null)
            {
                IdleFishing.World.OceanWaterFlowController.Instance.RegisterRipple(position, 1.85f * scale, 0.9f, 11.5f, 1.8f);
            }

            GameObject prefab = cfxrWaterRipplePrefab != null ? cfxrWaterRipplePrefab : waterRipplePrefab;
            if (prefab != null)
            {
                Vector3 spawnPos = position + Vector3.up * 0.05f;
                if (ObjectPoolManager.Instance != null)
                {
                    var ripple = ObjectPoolManager.Instance.SpawnAutoRelease(prefab, spawnPos, Quaternion.identity, 2.0f);
                    if (ripple != null) ripple.transform.localScale = Vector3.one * scale;
                    return ripple;
                }
                else
                {
                    var ripple = Instantiate(prefab, spawnPos, Quaternion.identity);
                    ripple.transform.localScale = Vector3.one * scale;
                    Destroy(ripple, 2.0f);
                    return ripple;
                }
            }
            return null;
        }

        public GameObject AttachCFXRWaterRippleLoop(Transform parent, Vector3 localOffset = default, float scale = 1.0f)
        {
            if (cfxrWaterRippleLoopPrefab == null || parent == null) return null;
            GameObject ripple = null;
            if (ObjectPoolManager.Instance != null)
            {
                ripple = ObjectPoolManager.Instance.Spawn(cfxrWaterRippleLoopPrefab, parent.position + localOffset, Quaternion.identity, parent);
            }
            else
            {
                ripple = Instantiate(cfxrWaterRippleLoopPrefab, parent.position + localOffset, Quaternion.identity, parent);
            }

            if (ripple != null)
            {
                ripple.transform.localPosition = localOffset;
                ripple.transform.localScale = Vector3.one * scale;
            }
            return ripple;
        }

        public void SpawnHitImpact(Vector3 position)
        {
            if (hitImpactPool == null) return;
            hitImpactPool.transform.position = position;
            hitImpactPool.Emit(12);
        }

        public void SpawnGoldBurst(Vector3 position, int count = 25)
        {
            if (goldBurstPool == null) return;
            goldBurstPool.transform.position = position;
            goldBurstPool.Emit(count);
        }

        // --- GROUP 1: DOCK & COMBAT ---
        public GameObject SpawnStunStars(Transform targetParent, float duration = 3.5f, Vector3 localOffset = default)
        {
            if (stunStarsPrefab == null || targetParent == null) return null;

            Vector3 offset = localOffset != default ? localOffset : new Vector3(0f, 1.35f, 0f);
            Vector3 worldPos = targetParent.position + offset;
            Quaternion worldHaloRot = Quaternion.Euler(-90f, 0f, 0f); // StunnedCirclingStars root circles around local Z, which points to world up at -90 deg X

            GameObject stars = null;
            if (ObjectPoolManager.Instance != null)
            {
                stars = ObjectPoolManager.Instance.SpawnAutoRelease(stunStarsPrefab, worldPos, worldHaloRot, duration, null);
            }
            else
            {
                stars = Instantiate(stunStarsPrefab, worldPos, worldHaloRot);
                Destroy(stars, duration);
            }

            if (stars != null)
            {
                stars.transform.position = worldPos;
                stars.transform.rotation = worldHaloRot;
                stars.transform.localScale = Vector3.one * 0.75f;

                var follower = stars.GetComponent<StunStarsFollower>();
                if (follower == null) follower = stars.AddComponent<StunStarsFollower>();
                follower.Setup(targetParent, offset);
            }
            return stars;
        }

        public void SpawnHitPunch(Vector3 position, float scale = 0.85f)
        {
            if (hitPunchPrefab == null) return;
            if (ObjectPoolManager.Instance != null)
            {
                var punch = ObjectPoolManager.Instance.SpawnAutoRelease(hitPunchPrefab, position, Quaternion.identity, 1.0f);
                if (punch != null) punch.transform.localScale = Vector3.one * scale;
            }
            else
            {
                GameObject punch = Instantiate(hitPunchPrefab, position, Quaternion.identity);
                punch.transform.localScale = Vector3.one * scale;
                Destroy(punch, 1.0f);
            }
        }

        public void SpawnDustPuff(Vector3 position, float scale = 0.5f)
        {
            if (dustPuffPrefab == null) return;
            if (ObjectPoolManager.Instance != null)
            {
                var dust = ObjectPoolManager.Instance.SpawnAutoRelease(dustPuffPrefab, position, Quaternion.identity, 1.2f);
                if (dust != null) dust.transform.localScale = Vector3.one * scale;
            }
            else
            {
                GameObject dust = Instantiate(dustPuffPrefab, position, Quaternion.identity);
                dust.transform.localScale = Vector3.one * scale;
                Destroy(dust, 1.2f);
            }
        }

        // --- GROUP 2: KITCHEN & MARKET ---
        public void SpawnSwordSlash(Vector3 position, Quaternion rotation = default, float scale = 0.8f)
        {
            if (swordSlashPrefab == null) return;
            Quaternion rot = rotation != default ? rotation : Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            if (ObjectPoolManager.Instance != null)
            {
                var slash = ObjectPoolManager.Instance.SpawnAutoRelease(swordSlashPrefab, position, rot, 1.0f);
                if (slash != null) slash.transform.localScale = Vector3.one * scale;
            }
            else
            {
                GameObject slash = Instantiate(swordSlashPrefab, position, rot);
                slash.transform.localScale = Vector3.one * scale;
                Destroy(slash, 1.0f);
            }
        }

        public GameObject AttachKitchenSteam(Transform parent, Vector3 localOffset = default)
        {
            if (smokeWhitePrefab == null || parent == null) return null;
            GameObject steam = null;
            if (ObjectPoolManager.Instance != null)
            {
                steam = ObjectPoolManager.Instance.Spawn(smokeWhitePrefab, parent.position + localOffset, Quaternion.identity, parent);
            }
            else
            {
                steam = Instantiate(smokeWhitePrefab, parent.position + localOffset, Quaternion.identity, parent);
            }

            if (steam != null)
            {
                steam.transform.localPosition = localOffset;
                steam.transform.localScale = Vector3.one * 0.65f;
            }
            return steam;
        }

        public void SpawnCustomerHeart(Vector3 position, float scale = 0.75f)
        {
            if (heartPoofPrefab != null)
            {
                if (ObjectPoolManager.Instance != null)
                {
                    var heart = ObjectPoolManager.Instance.SpawnAutoRelease(heartPoofPrefab, position, Quaternion.identity, 1.5f);
                    if (heart != null) heart.transform.localScale = Vector3.one * scale;
                }
                else
                {
                    GameObject heart = Instantiate(heartPoofPrefab, position, Quaternion.identity);
                    heart.transform.localScale = Vector3.one * scale;
                    Destroy(heart, 1.5f);
                }
            }
            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayHeart(position);
            }
        }

        // --- GROUP 3: REWARDS & MILESTONES ---
        public void SpawnConfetti(Vector3 position, float scale = 1.0f)
        {
            if (confettiPrefab != null)
            {
                if (ObjectPoolManager.Instance != null)
                {
                    var confetti = ObjectPoolManager.Instance.SpawnAutoRelease(confettiPrefab, position, Quaternion.identity, 2.8f);
                    if (confetti != null) confetti.transform.localScale = Vector3.one * scale;
                }
                else
                {
                    GameObject confetti = Instantiate(confettiPrefab, position, Quaternion.identity);
                    confetti.transform.localScale = Vector3.one * scale;
                    Destroy(confetti, 2.8f);
                }
            }
            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayConfetti(position);
            }
        }

        public void SpawnLevelUp(Vector3 position, float scale = 1.0f)
        {
            if (levelUpPrefab == null) return;
            if (ObjectPoolManager.Instance != null)
            {
                var levelUp = ObjectPoolManager.Instance.SpawnAutoRelease(levelUpPrefab, position, Quaternion.identity, 2.0f);
                if (levelUp != null) levelUp.transform.localScale = Vector3.one * scale;
            }
            else
            {
                GameObject levelUp = Instantiate(levelUpPrefab, position, Quaternion.identity);
                levelUp.transform.localScale = Vector3.one * scale;
                Destroy(levelUp, 2.0f);
            }
        }

        public void Despawn(GameObject vfxObject)
        {
            if (vfxObject == null) return;
            if (ObjectPoolManager.Instance != null && vfxObject.GetComponent<PooledInstance>() != null)
            {
                ObjectPoolManager.Instance.Release(vfxObject);
            }
            else
            {
                Destroy(vfxObject);
            }
        }

        public void SetFrenzyAura(bool active)
        {
            if (frenzyAuraObj == null || frenzyAuraSystem == null) return;

            frenzyAuraObj.SetActive(active);
            if (active)
            {
                frenzyAuraSystem.Play();
            }
            else
            {
                frenzyAuraSystem.Stop();
            }
        }

        // ==================== EVENT HANDLERS ====================
        private void OnFrenzyStarted()
        {
            SetFrenzyAura(true);
        }

        private void OnFrenzyEnded()
        {
            SetFrenzyAura(false);
        }

        private void OnChestOpened(int gold)
        {
            SpawnGoldBurst(new Vector3(0f, 1.2f, 1.5f), 35);
            SpawnConfetti(new Vector3(0f, 1.2f, 1.5f), 1.0f);
        }

        private void OnPrestigeCompleted(int medals)
        {
            SpawnGoldBurst(new Vector3(0f, 2.0f, 1.5f), 60);
            SpawnConfetti(new Vector3(0f, 2.0f, 1.5f), 1.4f);
        }

        private void OnUpgradePurchased(UpgradeType type, int newLevel)
        {
            // Spawn LevelUp beam near world center / dock
            SpawnLevelUp(new Vector3(0f, 0.1f, 1.5f), 1.0f);
            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayChestOpen();
            }
        }
    }
}
