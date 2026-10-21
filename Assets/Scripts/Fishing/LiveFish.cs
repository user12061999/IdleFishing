using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.UI;

namespace IdleFishing.Fishing
{
    public class LiveFish : MonoBehaviour
    {
        [Header("Fish Identity")]
        [SerializeField] private FishChainData speciesData;

        [Header("Health & Combat Stats")]
        [SerializeField] private int maxHealth = 200;
        [SerializeField] private int currentHealth = 200;
        [SerializeField] private int fishAttackDamage = 16;
        [SerializeField] private float contactDamageCooldown = 0.8f;
        [SerializeField] private float contactRadius = 0.95f;

        [Header("Random Hop Movement")]
        [SerializeField] private float hopIntervalMin = 0.4f;
        [SerializeField] private float hopIntervalMax = 0.8f;
        [SerializeField] private float hopDistance = 1.5f;
        [SerializeField] private float hopHeight = 1.1f;
        [SerializeField] private float hopDuration = 0.45f;
        [SerializeField] private float arenaRadius = 3.2f;

        [Header("Visual Effects")]
        [SerializeField] private Transform visualRoot;

        [Header("Boss Variant")]
        [SerializeField] private bool isBossFish = false;

        private bool isDefeated = false;
        private bool isPickedUp = false;
        private bool isReservedForPickup = false;
        private float reservationExpireTime = 0f;
        private bool isHopping = false;
        private int hopCount = 0;
        private float lastContactDamageTime = -10f;
        private Vector3 arenaCenter;
        private Vector3 currentGroundPos;
        private Vector3 initialVisualScale = Vector3.one;
        private Coroutine hitReactionCoroutine;
        private FloatingHealthBarUI healthBar;
        private GameObject bossAuraObj;
        private GameObject stunStarsObj;

        // Renderers & Materials for Flash Hit Anim
        private Renderer[] renderers;
        private Color[] originalColors;
        private Animator[] animators;

        [Header("Size & Quality Tier")]
        [SerializeField] private FishSizeCategory sizeCategory = FishSizeCategory.Standard;
        [SerializeField] private FishQualityTier qualityTier = FishQualityTier.Regular;
        private bool isRainbowVisual = false;
        private GameObject qualitySparklesObj;

        public event Action<int, int> OnHealthChanged; // current, max
        public event Action<LiveFish> OnDefeated;

        public FishChainData SpeciesData => speciesData;
        public ItemData WholeFishItem => speciesData != null ? speciesData.WholeFish : null;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDefeated => isDefeated;
        public bool IsPickedUp => isPickedUp;
        public bool IsReservedForPickup => isReservedForPickup && Time.time < reservationExpireTime;
        public bool IsHopping => isHopping;
        public bool IsBossFish => isBossFish;
        public bool IsBoss => isBossFish;
        public FishSizeCategory SizeCategory => sizeCategory;
        public FishQualityTier QualityTier => qualityTier;
        public float SizeScaleMultiplier => FishQualityData.GetSizeScale(sizeCategory);
        public float SizePriceMultiplier => FishQualityData.GetSizePriceMultiplier(sizeCategory);
        public float QualityPriceMultiplier => FishQualityData.GetQualityPriceMultiplier(qualityTier);

        public void MarkReservedForPickup(float duration = 5.0f)
        {
            isReservedForPickup = true;
            reservationExpireTime = Time.time + duration;
        }

        public void ClearReservation()
        {
            isReservedForPickup = false;
            reservationExpireTime = 0f;
        }

        public LiveFish SetVisualRoot(Transform root)
        {
            visualRoot = root;
            if (visualRoot != null)
            {
                initialVisualScale = visualRoot.localScale;
            }
            CacheRenderers();
            return this;
        }

        private void CacheRenderers()
        {
            if (visualRoot == null) return;
            animators = visualRoot.GetComponentsInChildren<Animator>();
            if (animators != null)
            {
                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] != null)
                    {
                        animators[i].speed = 2.2f;
                    }
                }
            }

            renderers = visualRoot.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                originalColors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i].material != null && renderers[i].material.HasProperty("_BaseColor"))
                    {
                        originalColors[i] = renderers[i].material.GetColor("_BaseColor");
                    }
                    else if (renderers[i].material != null && renderers[i].material.HasProperty("_Color"))
                    {
                        originalColors[i] = renderers[i].material.color;
                    }
                }
            }
        }

        public float AlignGroundToFloor(float pierFloorY = 0.62f)
        {
            if (visualRoot == null) return pierFloorY;

            Renderer[] rens = visualRoot.GetComponentsInChildren<Renderer>();
            if (rens == null || rens.Length == 0) return pierFloorY;

            Bounds totalBounds = rens[0].bounds;
            for (int i = 1; i < rens.Length; i++)
            {
                totalBounds.Encapsulate(rens[i].bounds);
            }

            float distToBottom = transform.position.y - totalBounds.min.y;
            float newGroundY = pierFloorY + distToBottom + 0.015f;

            Vector3 pos = transform.position;
            pos.y = newGroundY;
            transform.position = pos;
            currentGroundPos.y = newGroundY;
            if (arenaCenter != Vector3.zero)
            {
                arenaCenter.y = newGroundY;
            }

            var boxCol = GetComponent<BoxCollider>();
            if (boxCol != null)
            {
                totalBounds = rens[0].bounds;
                for (int i = 1; i < rens.Length; i++) totalBounds.Encapsulate(rens[i].bounds);

                Vector3 lossy = transform.lossyScale;
                float sx = Mathf.Max(0.001f, lossy.x);
                float sy = Mathf.Max(0.001f, lossy.y);
                float sz = Mathf.Max(0.001f, lossy.z);

                Vector3 localSize = new Vector3(
                    Mathf.Max(0.45f, totalBounds.size.x / sx),
                    Mathf.Max(0.30f, totalBounds.size.y / sy),
                    Mathf.Max(0.45f, totalBounds.size.z / sz)
                );

                Vector3 localCenter = transform.InverseTransformPoint(totalBounds.center);

                // Ensure collider bottom in world space is NEVER below pierFloorY + 0.01f
                float worldBottom = (transform.position.y + localCenter.y * sy) - (localSize.y * sy * 0.5f);
                if (worldBottom < pierFloorY + 0.01f)
                {
                    float diffWorld = (pierFloorY + 0.01f) - worldBottom;
                    localCenter.y += diffWorld / sy;
                }

                boxCol.center = localCenter;
                boxCol.size = localSize;
            }

            return newGroundY;
        }

        private void Update()
        {
            if (bossAuraObj != null && !isPickedUp && !isDefeated)
            {
                bossAuraObj.transform.position = new Vector3(transform.position.x, 0.635f, transform.position.z);
                bossAuraObj.transform.Rotate(Vector3.up, 25f * Time.deltaTime, Space.World);
            }

            if (isRainbowVisual && !isPickedUp && renderers != null)
            {
                Color rainbow = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.85f, 1f), 0.80f, 1f);
                SetRendererColor(rainbow);
            }
        }

        public void Initialize(FishChainData data, Vector3 targetGroundPos, bool isBoss = false, FishSizeCategory size = FishSizeCategory.Standard, FishQualityTier quality = FishQualityTier.Regular)
        {
            speciesData = data;
            isBossFish = isBoss || (data != null && data.IsApexBoss);
            sizeCategory = size;
            qualityTier = quality;
            hopCount = 0;
            arenaCenter = targetGroundPos;
            currentGroundPos = targetGroundPos;
            if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
            {
                var cfg = GameManager.Instance.BalanceConfig;
                hopIntervalMin = cfg.fishHopIntervalMin;
                hopIntervalMax = cfg.fishHopIntervalMax;
                hopDistance = cfg.fishHopDistance;
                hopHeight = cfg.fishHopHeight;
            }

            // Health scales smoothly with fish tier, difficulty and size
            int baseHp = 100;
            if (data != null)
            {
                baseHp = Mathf.RoundToInt(85f + (data.Tier * 50f * data.CatchDifficulty));
            }

            // Apply size multiplier to HP
            baseHp = Mathf.RoundToInt(baseHp * FishQualityData.GetSizeHPMultiplier(sizeCategory));

            if (isBossFish)
            {
                float hpMult = 3.5f;
                float dmgMult = 1.5f;
                float scaleMult = 2.4f;
                if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
                {
                    hpMult = GameManager.Instance.BalanceConfig.bossHealthMultiplier;
                    dmgMult = GameManager.Instance.BalanceConfig.bossDamageMultiplier;
                    scaleMult = Mathf.Max(2.2f, GameManager.Instance.BalanceConfig.bossScaleMultiplier * 1.4f);
                }
                baseHp = Mathf.RoundToInt(baseHp * hpMult);
                fishAttackDamage = Mathf.RoundToInt(fishAttackDamage * dmgMult);

                float finalBossScale = scaleMult * Mathf.Min(1.35f, SizeScaleMultiplier);
                transform.localScale = Vector3.one * Mathf.Clamp(finalBossScale, 2.2f, 3.4f);

                // Keep authentic UV-mapped Shark/Ray colors for Unique Apex Ocean Bosses
                if (data == null || !data.IsApexBoss)
                {
                    SetRendererColor(new Color(1f, 0.85f, 0.2f));
                }

                SpawnBossAuraRing();
                if (GetComponent<BossEncounterController>() == null)
                {
                    gameObject.AddComponent<BossEncounterController>();
                }
            }
            else
            {
                // Bold, chunky toon fish (easily readable and satisfying from high isometric camera!)
                float normalScale = 1.70f * SizeScaleMultiplier;
                transform.localScale = Vector3.one * Mathf.Clamp(normalScale, 1.45f, 2.80f);
            }

            if (visualRoot != null)
            {
                initialVisualScale = visualRoot.localScale;
            }

            // Calculate landing target Y based on mesh bounds so the fish lands cleanly on the pier deck (0.62m)
            float distToBottom = 0f;
            if (visualRoot != null)
            {
                var rens = visualRoot.GetComponentsInChildren<Renderer>();
                if (rens != null && rens.Length > 0)
                {
                    Bounds b = rens[0].bounds;
                    for (int i = 1; i < rens.Length; i++) b.Encapsulate(rens[i].bounds);
                    distToBottom = transform.position.y - b.min.y;
                }
            }
            float restingGroundY = 0.62f + distToBottom + 0.015f;
            targetGroundPos.y = restingGroundY;
            arenaCenter = targetGroundPos;
            currentGroundPos = targetGroundPos;

            maxHealth = Mathf.Max(80, baseHp);
            currentHealth = maxHealth;

            // Apply Material Visuals (Silver metallic, Golden shine, Rainbow hue)
            ApplyQualityVisuals();

            // Procedurally spawn Floating Health Bar above fish
            GameObject barObj = new GameObject("Fish_HealthBar");
            healthBar = barObj.AddComponent<FloatingHealthBarUI>();

            string displayName = data != null ? data.SpeciesName : "Fish";
            string qualityPrefix = FishQualityData.GetQualityPrefix(qualityTier, sizeCategory);

            if (isBossFish)
            {
                string titleLabel = (data != null && data.IsApexBoss && !string.IsNullOrEmpty(data.BossTitle))
                    ? $"[BOSS] {data.BossTitle}"
                    : $"[BOSS] {displayName}";
                healthBar.Setup(transform, qualityPrefix + titleLabel, new Vector3(0, 2.7f, 0), new Color(0.94f, 0.14f, 0.14f), isBoss: true);
            }
            else
            {
                Color titleCol = qualityTier == FishQualityTier.Regular
                    ? new Color(0.95f, 0.35f, 0.25f)
                    : FishQualityData.GetQualityColor(qualityTier);
                healthBar.Setup(transform, qualityPrefix + displayName, new Vector3(0, 2.0f, 0), titleCol, isBoss: false);
            }

            healthBar.UpdateHealth(currentHealth, maxHealth);

            // Initial flight from rod to landing zone, then start random hopping loop
            StartCoroutine(InitialLandingRoutine(targetGroundPos));
        }

        private void ApplyQualityVisuals()
        {
            if (renderers == null || renderers.Length == 0) return;

            switch (qualityTier)
            {
                case FishQualityTier.Silver:
                    // Ánh bạc kim loại lấp lánh
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        var mat = renderers[i].material;
                        if (mat == null) continue;
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.88f, 0.94f, 1.0f));
                        else if (mat.HasProperty("_Color")) mat.color = new Color(0.88f, 0.94f, 1.0f);
                        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.85f);
                        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.80f);
                    }
                    break;

                case FishQualityTier.Golden:
                    // Phủ vàng chói lọi
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        var mat = renderers[i].material;
                        if (mat == null) continue;
                        Color goldCol = new Color(1.0f, 0.82f, 0.12f);
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", goldCol);
                        else if (mat.HasProperty("_Color")) mat.color = goldCol;
                        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.95f);
                        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.88f);
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", new Color(0.4f, 0.3f, 0.05f));
                        }
                    }
                    SpawnGoldenSparkles();
                    break;

                case FishQualityTier.Rainbow:
                    isRainbowVisual = true;
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        var mat = renderers[i].material;
                        if (mat == null) continue;
                        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.70f);
                        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
                    }
                    SpawnBossAuraRing(); // Hào quang lấp lánh
                    break;
            }
        }

        private static Material cachedSparkleMaterial;

        private static Material GetSparkleMaterial()
        {
            if (cachedSparkleMaterial != null) return cachedSparkleMaterial;

#if UNITY_EDITOR
            cachedSparkleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VFX/Shared/Materials/star_AB.mat");
            if (cachedSparkleMaterial != null) return cachedSparkleMaterial;
#endif

            if (IdleFishing.VFX.VFXManager.Instance != null && IdleFishing.VFX.VFXManager.Instance.SharedParticleMaterial != null)
            {
                cachedSparkleMaterial = IdleFishing.VFX.VFXManager.Instance.SharedParticleMaterial;
                return cachedSparkleMaterial;
            }

            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                       ?? Shader.Find("Particles/Standard Unlit")
                       ?? Shader.Find("Sprites/Default");
            cachedSparkleMaterial = new Material(s);
            cachedSparkleMaterial.name = "Sparkle_Fallback_Material";
            return cachedSparkleMaterial;
        }

        private void SpawnGoldenSparkles()
        {
            if (qualitySparklesObj != null) return;
            qualitySparklesObj = new GameObject("Golden_Sparkles");
            qualitySparklesObj.transform.SetParent(transform, false);
            qualitySparklesObj.transform.localPosition = new Vector3(0, 0.35f, 0);

            var ps = qualitySparklesObj.AddComponent<ParticleSystem>();
            var pMain = ps.main;
            pMain.startLifetime = 0.85f;
            pMain.startSpeed = 0.45f;
            pMain.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.22f);
            pMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.88f, 0.25f, 0.95f), new Color(1f, 0.7f, 0.15f, 0.9f));
            pMain.maxParticles = 16;
            pMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var pEmit = ps.emission;
            pEmit.rateOverTime = 8f;

            var pShape = ps.shape;
            pShape.shapeType = ParticleSystemShapeType.Sphere;
            pShape.radius = 0.45f;

            // Twinkle size animation over lifetime
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sparkleCurve = new AnimationCurve();
            sparkleCurve.AddKey(0f, 0.1f);
            sparkleCurve.AddKey(0.35f, 1.25f);
            sparkleCurve.AddKey(1f, 0f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, sparkleCurve);

            // Subtle rotation over lifetime
            var rol = ps.rotationOverLifetime;
            rol.enabled = true;
            rol.z = new ParticleSystem.MinMaxCurve(-60f * Mathf.Deg2Rad, 60f * Mathf.Deg2Rad);

            // Assign URP particle material to prevent missing/pink material error!
            var psRen = qualitySparklesObj.GetComponent<ParticleSystemRenderer>();
            psRen.sharedMaterial = GetSparkleMaterial();
            psRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void SpawnBossAuraRing()
        {
            if (bossAuraObj != null) Destroy(bossAuraObj);

            bossAuraObj = new GameObject("Boss_Aura_Root");
            bossAuraObj.transform.position = new Vector3(transform.position.x, 0.635f, transform.position.z);
            bossAuraObj.transform.rotation = Quaternion.identity;

            // 1. Hollow Glowing Ground Ring Quad (Rotated flat on ground, transparent center)
            GameObject ringQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ringQuad.name = "Aura_GroundRing";
            ringQuad.transform.SetParent(bossAuraObj.transform, false);
            ringQuad.transform.localPosition = Vector3.zero;
            ringQuad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Lay flat on XZ plane
            ringQuad.transform.localScale = new Vector3(4.2f, 4.2f, 1f);

            var col = ringQuad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var ren = ringQuad.GetComponent<Renderer>();
            if (ren != null)
            {
                Material ringMat = null;
#if UNITY_EDITOR
                ringMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VFX/Shared/Materials/ring_shockwave_ADD.mat");
#endif
                if (ringMat != null)
                {
                    Material inst = new Material(ringMat);
                    Color fieryColor = new Color(1f, 0.35f, 0.12f, 0.95f);
                    if (inst.HasProperty("_BaseColor")) inst.SetColor("_BaseColor", fieryColor);
                    if (inst.HasProperty("_Color")) inst.color = fieryColor;
                    ren.material = inst;
                }
                else
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
                    Material mat = new Material(s);
                    ren.material = mat;
                }
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // 2. Rising Fiery Ember Particles around the ring perimeter
            GameObject embersObj = new GameObject("Aura_RisingEmbers");
            embersObj.transform.SetParent(bossAuraObj.transform, false);
            embersObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            ParticleSystem ps = embersObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.startLifetime = 0.9f;
            main.startSpeed = 0.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.2f, 0.9f), new Color(1f, 0.25f, 0.1f, 0.9f));
            main.gravityModifier = -0.3f; // Float gently upward
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 16f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.15f;
            shape.radiusThickness = 0.25f; // Ring/donut band around the perimeter
            shape.rotation = new Vector3(90f, 0f, 0f); // Emits from horizontal plane

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve shrinkCurve = new AnimationCurve();
            shrinkCurve.AddKey(0f, 1f);
            shrinkCurve.AddKey(1f, 0f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, shrinkCurve);

            var colOverLife = ps.colorOverLifetime;
            colOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f), new GradientColorKey(new Color(0.9f, 0.15f, 0.1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLife.color = grad;

            var psRen = embersObj.GetComponent<ParticleSystemRenderer>();
            if (IdleFishing.VFX.VFXManager.Instance != null && IdleFishing.VFX.VFXManager.Instance.SharedParticleMaterial != null)
            {
                psRen.sharedMaterial = IdleFishing.VFX.VFXManager.Instance.SharedParticleMaterial;
            }
            else
            {
                Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
                Material mat = new Material(s);
                psRen.material = mat;
            }
            psRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private IEnumerator InitialLandingRoutine(Vector3 target)
        {
            Vector3 startPos = transform.position;
            float duration = 0.8f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 current = Vector3.Lerp(startPos, target, t);
                float height = Mathf.Sin(t * Mathf.PI) * 3.5f;
                current.y += height;

                transform.position = current;
                transform.Rotate(Vector3.up, 360f * Time.deltaTime * 2f);
                yield return null;
            }

            transform.position = target;
            currentGroundPos = target;
            AlignGroundToFloor(0.62f);

            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlaySplash(target);
            }
            if (IdleFishing.VFX.VFXManager.Instance != null)
            {
                IdleFishing.VFX.VFXManager.Instance.SpawnSplash(target);
            }

            // Start random hopping loop
            StartCoroutine(RandomHopLoop());
        }

        private IEnumerator RandomHopLoop()
        {
            while (!isDefeated && !isPickedUp)
            {
                float wait = UnityEngine.Random.Range(hopIntervalMin, hopIntervalMax);
                yield return YieldCache.WaitForSeconds(wait);

                if (isDefeated || isPickedUp) yield break;

                // Pick random direction within arena
                Vector2 randCircle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.8f, hopDistance);
                Vector3 dest = currentGroundPos + new Vector3(randCircle.x, 0, randCircle.y);
                dest.y = currentGroundPos.y;

                // Keep inside arena boundary
                if (Vector3.Distance(arenaCenter, dest) > arenaRadius)
                {
                    Vector3 offset = (dest - arenaCenter).normalized * arenaRadius;
                    dest = arenaCenter + offset;
                    dest.y = currentGroundPos.y;
                }

                yield return StartCoroutine(HopToDestination(dest));
            }
        }

        private IEnumerator HopToDestination(Vector3 targetGround)
        {
            isHopping = true;
            Vector3 startPos = transform.position;
            Vector3 startGround = currentGroundPos;
            float elapsed = 0f;

            // Look in jump direction
            Vector3 dir = (targetGround - startGround).normalized;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }

            while (elapsed < hopDuration)
            {
                if (isDefeated || isPickedUp)
                {
                    isHopping = false;
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / hopDuration);

                // Lerp horizontal
                Vector3 current = Vector3.Lerp(startPos, targetGround, t);

                // Parabolic jump arc into the air
                float arc = Mathf.Sin(t * Mathf.PI) * hopHeight;
                current.y += arc;

                transform.position = current;

                if (bossAuraObj != null)
                {
                    bossAuraObj.transform.position = new Vector3(current.x, 0.635f, current.z);
                    float pulse = 1.0f + Mathf.Sin(Time.time * 6f) * 0.08f;
                    bossAuraObj.transform.localScale = new Vector3(pulse, 1f, pulse);
                    bossAuraObj.transform.Rotate(Vector3.up, 40f * Time.deltaTime, Space.World);
                }

                // Wiggle/twist while in mid-air
                if (visualRoot != null)
                {
                    float wiggle = Mathf.Sin(t * Mathf.PI * 4f) * 20f;
                    visualRoot.localRotation = Quaternion.Euler(wiggle, 0, wiggle * 0.5f);
                }

                // Check collision damage when fish touches worker during jump!
                CheckContactDamageWithWorker();

                yield return null;
            }

            transform.position = targetGround;
            currentGroundPos = targetGround;
            if (bossAuraObj != null)
            {
                bossAuraObj.transform.position = new Vector3(targetGround.x, 0.635f, targetGround.z);
                bossAuraObj.transform.localScale = Vector3.one;
            }
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.identity;
            }
            isHopping = false;
            hopCount++;

            if (isBossFish && !isDefeated && !isPickedUp && hopCount % 2 == 0)
            {
                PerformBossSlam(targetGround);
            }
            else
            {
                // Check contact damage on landing
                CheckContactDamageWithWorker();
            }
        }

        private void PerformBossSlam(Vector3 slamCenter)
        {
            if (IdleFishing.VFX.VFXManager.Instance != null)
            {
                IdleFishing.VFX.VFXManager.Instance.SpawnSplash(slamCenter, 1.35f);
                IdleFishing.VFX.VFXManager.Instance.SpawnHitPunch(slamCenter + Vector3.up * 0.3f, 1.25f);
            }
            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlaySplash(slamCenter);
            }
            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    slamCenter + Vector3.up * 1.45f,
                    0,
                    DamageStyle.FishAttack,
                    "[BOSS SLAM!]"
                );
            }

            float slamRadius = contactRadius * 1.85f;
            int slamDamage = Mathf.Max(8, Mathf.RoundToInt(fishAttackDamage * 0.85f));
            Collider[] hits = Physics.OverlapSphere(slamCenter, slamRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                var wrangler = hits[i].GetComponentInParent<FishWranglerWorker>();
                if (wrangler != null && !wrangler.IsCarrying && !wrangler.IsKnockedOut)
                {
                    wrangler.TakeFishHit(this, slamDamage);
                    lastContactDamageTime = Time.time;
                }
            }
        }

        private void CheckContactDamageWithWorker()
        {
            if (isDefeated || isPickedUp) return;
            if (Time.time < lastContactDamageTime + contactDamageCooldown) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, contactRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                var wrangler = hits[i].GetComponentInParent<FishWranglerWorker>();
                if (wrangler != null && !wrangler.IsCarrying && !wrangler.IsKnockedOut)
                {
                    // Deal damage ONLY when fish physically touches worker!
                    wrangler.TakeFishHit(this, fishAttackDamage);
                    lastContactDamageTime = Time.time;

                    // Fish bounces slightly off worker
                    Vector3 rebound = (transform.position - wrangler.transform.position).normalized * 0.35f;
                    rebound.y = 0.2f;
                    transform.position += rebound;
                    currentGroundPos += new Vector3(rebound.x, 0, rebound.z);

                    Debug.Log($"[LiveFish] Fish bounced into worker! Dealt {fishAttackDamage} damage!");
                    break;
                }
            }
        }

        public void TakeDamage(int damage, bool isPlayer = false, Vector3? hitOrigin = null)
        {
            if (isDefeated) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (healthBar != null)
            {
                healthBar.UpdateHealth(currentHealth, maxHealth);
                healthBar.TriggerHitPunch();
            }

            if (isPlayer)
            {
                var bossCtrl = GetComponent<BossEncounterController>();
                if (bossCtrl != null && bossCtrl.IsTelegraphing)
                {
                    bossCtrl.OnPlayerTappedDuringTelegraph();
                }
            }

            // Spawn Animated Floating Damage Text!
            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 0.8f,
                    damage,
                    isPlayer ? DamageStyle.PlayerTap : DamageStyle.WorkerAttack
                );
            }

            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayWhack(transform.position);
            }
            if (IdleFishing.VFX.VFXManager.Instance != null)
            {
                Vector3 hitPos = transform.position + Vector3.up * 0.4f;
                IdleFishing.VFX.VFXManager.Instance.SpawnHitImpact(hitPos);
                IdleFishing.VFX.VFXManager.Instance.SpawnHitPunch(hitPos, isPlayer ? 1.0f : 0.75f);
                if (isPlayer)
                {
                    IdleFishing.VFX.VFXManager.Instance.SpawnSwordSlash(hitPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), 0.85f);
                }
            }

            if (hitReactionCoroutine != null)
            {
                StopCoroutine(hitReactionCoroutine);
                if (visualRoot != null) visualRoot.localScale = initialVisualScale;
                ResetRendererColor();
            }

            // Play Hit Reaction Animation for Fish!
            Vector3 pushDir = hitOrigin.HasValue ? (transform.position - hitOrigin.Value).normalized : -transform.forward;
            pushDir.y = 0;
            hitReactionCoroutine = StartCoroutine(FishHitReactionRoutine(pushDir, isPlayer));

            if (currentHealth <= 0)
            {
                Defeat();
            }
        }

        private IEnumerator FishHitReactionRoutine(Vector3 pushDir, bool isPlayer)
        {
            // 1. Flash Red/White on materials
            SetRendererColor(Color.red);

            // 2. Squash & Stretch deformation - ALWAYS anchored to initialVisualScale to prevent runaway multiplication
            if (visualRoot != null)
            {
                visualRoot.localScale = new Vector3(initialVisualScale.x * 1.18f, initialVisualScale.y * 0.75f, initialVisualScale.z * 1.18f);
            }

            // 3. Elastic Recoil (Springs back! Does NOT permanently push fish into ocean!)
            float recoilAmount = isBossFish ? 0.05f : (isPlayer ? 0.16f : 0.08f);
            Vector3 anchorGround = currentGroundPos;
            Vector3 recoilTarget = anchorGround + pushDir * recoilAmount;
            transform.position = recoilTarget;

            yield return YieldCache.WaitForSeconds(0.06f);

            // Spring back with overshoot
            if (visualRoot != null)
            {
                visualRoot.localScale = new Vector3(initialVisualScale.x * 0.94f, initialVisualScale.y * 1.08f, initialVisualScale.z * 0.94f);
            }
            transform.position = Vector3.Lerp(recoilTarget, anchorGround, 0.7f);

            yield return YieldCache.WaitForSeconds(0.06f);

            // Reset position, scale and color
            transform.position = anchorGround;
            if (visualRoot != null)
            {
                visualRoot.localScale = initialVisualScale;
            }
            ResetRendererColor();
            hitReactionCoroutine = null;
        }

        private void SetRendererColor(Color flashCol)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    if (renderers[i].material.HasProperty("_BaseColor"))
                        renderers[i].material.SetColor("_BaseColor", flashCol);
                    else if (renderers[i].material.HasProperty("_Color"))
                        renderers[i].material.color = flashCol;
                }
            }
        }

        private void ResetRendererColor()
        {
            if (renderers == null || originalColors == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null && i < originalColors.Length)
                {
                    if (renderers[i].material.HasProperty("_BaseColor"))
                        renderers[i].material.SetColor("_BaseColor", originalColors[i]);
                    else if (renderers[i].material.HasProperty("_Color"))
                        renderers[i].material.color = originalColors[i];
                }
            }
        }

        private void Defeat()
        {
            isDefeated = true;
            if (hitReactionCoroutine != null)
            {
                StopCoroutine(hitReactionCoroutine);
                hitReactionCoroutine = null;
            }
            StopAllCoroutines();
            if (visualRoot != null)
            {
                visualRoot.localScale = initialVisualScale;
            }

            // Immediately disable collider to stop further hits, raycasts, or duplicate worker targeting
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            OnDefeated?.Invoke(this);
            GameEvents.TriggerFishDefeated(speciesData, this);

            if (FishingFrenzyManager.Instance != null)
            {
                float charge = 6.0f;
                if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
                {
                    charge = GameManager.Instance.BalanceConfig.frenzyChargePerDefeat;
                }
                FishingFrenzyManager.Instance.AddCharge(charge);
            }

            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
                healthBar = null;
            }
            if (bossAuraObj != null)
            {
                Destroy(bossAuraObj);
                bossAuraObj = null;
            }

            ResetRendererColor();

            if (animators != null)
            {
                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] != null)
                    {
                        animators[i].enabled = false;
                    }
                }
            }

            // Knocked out pose (lie flat on side for fish/sharks, flat on belly for rays, belly-up for lobster/crab)
            if (visualRoot != null)
            {
                string sid = speciesData != null && !string.IsNullOrEmpty(speciesData.SpeciesId) ? speciesData.SpeciesId : "";
                bool isCrustacean = sid.Contains("crab") || sid.Contains("lobster");
                bool isFlatRay = sid.Contains("ray");
                float zRot = isCrustacean ? 180f : (isFlatRay ? 0f : 90f);
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, zRot);
                AlignGroundToFloor(0.62f);
            }

            if (IdleFishing.VFX.VFXManager.Instance != null)
            {
                stunStarsObj = IdleFishing.VFX.VFXManager.Instance.SpawnStunStars(transform, 6.0f, Vector3.up * 1.15f);
            }

            // Boss Defeat Reward: Huge Instant Gold Bounty!
            if (isBossFish)
            {
                if (IdleFishing.VFX.VFXManager.Instance != null)
                {
                    IdleFishing.VFX.VFXManager.Instance.SpawnConfetti(transform.position + Vector3.up * 1.0f, 1.2f);
                }
                int fishBasePrice = (speciesData != null && speciesData.WholeFish != null) 
                    ? speciesData.WholeFish.BasePrice 
                    : (150 + (speciesData != null ? speciesData.Tier * 50 : 0));
                int bonusGold = fishBasePrice * 3; // Tiền thưởng Boss gấp 3 lần giá trị nguyên con!
                if (IdleFishing.Economy.PrestigeManager.Instance != null)
                {
                    bonusGold = Mathf.RoundToInt(bonusGold * IdleFishing.Economy.PrestigeManager.Instance.BossBonusMultiplier);
                }
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddCoins(bonusGold);
                }
                GameEvents.TriggerBossFishDefeated(speciesData, bonusGold);

                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        transform.position + Vector3.up * 1.5f,
                        bonusGold,
                        DamageStyle.GoldBonus,
                        $"+{NumberFormatter.Format(bonusGold)} G BOSS!"
                    );
                }

                if (FlyingCoinManager.Instance != null)
                {
                    FlyingCoinManager.Instance.PlayFlyingCoins(transform.position + Vector3.up * 1.0f, 6);
                }
            }

            if (FishingPierManager.Instance != null)
            {
                FishingPierManager.Instance.OnFishDefeated(this);
            }

            // If no wranglers hired or idle, prompt player tap and auto-deliver after brief delay
            if (WranglerManager.Instance == null || WranglerManager.Instance.ActiveWranglerCount == 0)
            {
                StartCoroutine(DefeatedAutoDeliverRoutine());
            }

            Debug.Log($"[LiveFish] {speciesData?.SpeciesName} defeated! (Boss: {isBossFish})");
        }

        private IEnumerator DefeatedAutoDeliverRoutine()
        {
            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 1.2f,
                    0,
                    DamageStyle.GoldBonus,
                    "[TAP TO HARVEST]"
                );
            }

            yield return YieldCache.WaitForSeconds(1.2f);

            if (isDefeated && !isPickedUp)
            {
                DeliverToStorage();
            }
        }

        public void DeliverToStorage()
        {
            if (isPickedUp) return;
            isPickedUp = true;
            StopAllCoroutines();

            ClearStunStars();

            if (FishingPierManager.Instance != null)
            {
                FishingPierManager.Instance.DeliverFishToStorage(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetManualDeliveryInProgress()
        {
            isPickedUp = true;
            StopAllCoroutines();

            ClearStunStars();
        }

        public void SetPickedUp(Transform carryParent)
        {
            isPickedUp = true;
            isReservedForPickup = true;
            if (hitReactionCoroutine != null)
            {
                StopCoroutine(hitReactionCoroutine);
                hitReactionCoroutine = null;
            }
            StopAllCoroutines();

            // Immediately disable collider so fish cannot be clicked or collided with while carried
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            transform.SetParent(carryParent, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            if (visualRoot != null)
            {
                visualRoot.localScale = initialVisualScale;
            }

            ClearStunStars();

            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
                healthBar = null;
            }
            if (bossAuraObj != null)
            {
                Destroy(bossAuraObj);
                bossAuraObj = null;
            }
            if (qualitySparklesObj != null)
            {
                Destroy(qualitySparklesObj);
                qualitySparklesObj = null;
            }
        }

        private int lastTapFrame = -1;

        public void TapAttack()
        {
            if (Time.frameCount == lastTapFrame) return;
            lastTapFrame = Time.frameCount;
            PerformTapAttack();
        }

        private void OnMouseDown()
        {
            TapAttack();
        }

        private void PerformTapAttack()
        {
            if (isPickedUp) return;

            // If fish is already defeated, player tap immediately delivers fish to kitchen storage!
            if (isDefeated)
            {
                DeliverToStorage();
                return;
            }

            int basePlayerDamage = 10;
            if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
            {
                basePlayerDamage = GameManager.Instance.BalanceConfig.playerClickBaseDamage;
            }

            int playerDamage = basePlayerDamage;
            if (UpgradeManager.Instance != null)
            {
                playerDamage = Mathf.RoundToInt(basePlayerDamage * UpgradeManager.Instance.GetValue(UpgradeType.WranglerPower));
            }
            if (FishingFrenzyManager.Instance != null && FishingFrenzyManager.Instance.IsFrenzyActive)
            {
                playerDamage = Mathf.RoundToInt(playerDamage * FishingFrenzyManager.Instance.DamageMultiplier);
            }

            if (FishingFrenzyManager.Instance != null)
            {
                float tapCharge = 1.0f;
                if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
                {
                    tapCharge = GameManager.Instance.BalanceConfig.frenzyChargePerTap;
                }
                FishingFrenzyManager.Instance.AddCharge(tapCharge);
            }

            Vector3 clickPoint = Camera.main != null ? Camera.main.transform.position : transform.position - transform.forward;
            TakeDamage(playerDamage, isPlayer: true, hitOrigin: clickPoint);
        }

        private void ClearStunStars()
        {
            if (stunStarsObj != null)
            {
                if (IdleFishing.VFX.VFXManager.Instance != null)
                {
                    IdleFishing.VFX.VFXManager.Instance.Despawn(stunStarsObj);
                }
                else
                {
                    Destroy(stunStarsObj);
                }
                stunStarsObj = null;
            }
        }

        private void OnDestroy()
        {
            ClearStunStars();

            if (bossAuraObj != null)
            {
                Destroy(bossAuraObj);
                bossAuraObj = null;
            }

            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
                healthBar = null;
            }

            if (FishingPierManager.Instance != null)
            {
                FishingPierManager.Instance.UnregisterLiveFish(this);
            }
        }
    }
}
