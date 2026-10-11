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
        private bool isHopping = false;
        private int hopCount = 0;
        private float lastContactDamageTime = -10f;
        private Vector3 arenaCenter;
        private Vector3 currentGroundPos;
        private FloatingHealthBarUI healthBar;
        private GameObject bossAuraObj;
        private GameObject stunStarsObj;

        // Renderers & Materials for Flash Hit Anim
        private Renderer[] renderers;
        private Color[] originalColors;
        private Animator[] animators;

        public event Action<int, int> OnHealthChanged; // current, max
        public event Action<LiveFish> OnDefeated;

        public FishChainData SpeciesData => speciesData;
        public ItemData WholeFishItem => speciesData != null ? speciesData.WholeFish : null;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDefeated => isDefeated;
        public bool IsPickedUp => isPickedUp;
        public bool IsHopping => isHopping;
        public bool IsBossFish => isBossFish;
        public bool IsBoss => isBossFish;

        public LiveFish SetVisualRoot(Transform root)
        {
            visualRoot = root;
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

        public void Initialize(FishChainData data, Vector3 targetGroundPos, bool isBoss = false)
        {
            speciesData = data;
            isBossFish = isBoss || (data != null && data.IsApexBoss);
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

            // Health scales smoothly with fish tier and difficulty
            int baseHp = 100;
            if (data != null)
            {
                baseHp = Mathf.RoundToInt(85f + (data.Tier * 50f * data.CatchDifficulty));
            }

            if (isBossFish)
            {
                float hpMult = 3.5f;
                float dmgMult = 1.5f;
                float scaleMult = 1.75f;
                if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
                {
                    hpMult = GameManager.Instance.BalanceConfig.bossHealthMultiplier;
                    dmgMult = GameManager.Instance.BalanceConfig.bossDamageMultiplier;
                    scaleMult = GameManager.Instance.BalanceConfig.bossScaleMultiplier;
                }
                baseHp = Mathf.RoundToInt(baseHp * hpMult);
                fishAttackDamage = Mathf.RoundToInt(fishAttackDamage * dmgMult);
                transform.localScale = Vector3.one * scaleMult;

                // Keep authentic UV-mapped Shark/Ray colors for Unique Apex Ocean Bosses
                if (data == null || !data.IsApexBoss)
                {
                    SetRendererColor(new Color(1f, 0.85f, 0.2f));
                }

                SpawnBossAuraRing();
            }

            maxHealth = Mathf.Max(80, baseHp);
            currentHealth = maxHealth;

            // Procedurally spawn Floating Health Bar above fish
            GameObject barObj = new GameObject("Fish_HealthBar");
            healthBar = barObj.AddComponent<FloatingHealthBarUI>();

            string displayName = data != null ? data.SpeciesName : "Fish";
            if (isBossFish)
            {
                string titleLabel = (data != null && data.IsApexBoss && !string.IsNullOrEmpty(data.BossTitle))
                    ? $"[BOSS] {data.BossTitle}"
                    : $"[BOSS] {displayName}";
                healthBar.Setup(transform, titleLabel, new Vector3(0, 2.0f, 0), new Color(1f, 0.82f, 0.18f));
            }
            else
            {
                healthBar.Setup(transform, displayName, new Vector3(0, 1.3f, 0), new Color(0.95f, 0.35f, 0.25f));
            }

            healthBar.UpdateHealth(currentHealth, maxHealth);

            // Initial flight from rod to landing zone, then start random hopping loop
            StartCoroutine(InitialLandingRoutine(targetGroundPos));
        }

        private void SpawnBossAuraRing()
        {
            if (bossAuraObj != null) Destroy(bossAuraObj);

            bossAuraObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bossAuraObj.name = "Boss_Aura_Ring";
            bossAuraObj.transform.SetParent(transform, false);
            bossAuraObj.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            bossAuraObj.transform.localScale = new Vector3(1.35f, 0.015f, 1.35f);

            var col = bossAuraObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var ren = bossAuraObj.GetComponent<Renderer>();
            if (ren != null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material auraMat = new Material(s);
                Color auraCol = new Color(1f, 0.32f, 0.12f, 0.85f);
                if (auraMat.HasProperty("_BaseColor")) auraMat.SetColor("_BaseColor", auraCol);
                if (auraMat.HasProperty("_Color")) auraMat.color = auraCol;
                ren.material = auraMat;
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
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
                yield return new WaitForSeconds(wait);

                if (isDefeated || isPickedUp) yield break;

                // Pick random direction within arena
                Vector2 randCircle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.8f, hopDistance);
                Vector3 dest = currentGroundPos + new Vector3(randCircle.x, 0, randCircle.y);

                // Keep inside arena boundary
                if (Vector3.Distance(arenaCenter, dest) > arenaRadius)
                {
                    dest = arenaCenter + (dest - arenaCenter).normalized * arenaRadius;
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
                    bossAuraObj.transform.position = new Vector3(current.x, targetGround.y + 0.03f, current.z);
                    float pulse = 1.35f + Mathf.Sin(Time.time * 6f) * 0.12f;
                    bossAuraObj.transform.localScale = new Vector3(pulse, 0.015f, pulse);
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
                bossAuraObj.transform.localPosition = new Vector3(0f, 0.03f, 0f);
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

            // Play Hit Reaction Animation for Fish!
            Vector3 pushDir = hitOrigin.HasValue ? (transform.position - hitOrigin.Value).normalized : -transform.forward;
            pushDir.y = 0;
            StartCoroutine(FishHitReactionRoutine(pushDir, isPlayer));

            if (currentHealth <= 0)
            {
                Defeat();
            }
        }

        private IEnumerator FishHitReactionRoutine(Vector3 pushDir, bool isPlayer)
        {
            // 1. Flash Red/White on materials
            SetRendererColor(Color.red);

            // 2. Squash & Stretch deformation
            Vector3 origScale = Vector3.one;
            if (visualRoot != null)
            {
                origScale = visualRoot.localScale;
                visualRoot.localScale = new Vector3(origScale.x * 1.4f, origScale.y * 0.5f, origScale.z * 1.4f);
            }

            // 3. Knockback recoil
            float recoilAmount = isPlayer ? 0.35f : 0.2f;
            transform.position += pushDir * recoilAmount;
            currentGroundPos += pushDir * recoilAmount;

            yield return new WaitForSeconds(0.08f);

            // Spring back with overshoot
            if (visualRoot != null)
            {
                visualRoot.localScale = new Vector3(origScale.x * 0.85f, origScale.y * 1.25f, origScale.z * 0.85f);
            }

            yield return new WaitForSeconds(0.06f);

            // Reset scale and color
            if (visualRoot != null)
            {
                visualRoot.localScale = origScale;
            }
            ResetRendererColor();
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
            StopAllCoroutines();
            OnDefeated?.Invoke(this);

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
            }

            if (IdleFishing.VFX.VFXManager.Instance != null)
            {
                stunStarsObj = IdleFishing.VFX.VFXManager.Instance.SpawnStunStars(transform, 6.0f, Vector3.up * 0.8f);
            }

            // Boss Defeat Reward: Huge Instant Gold Bounty!
            if (isBossFish)
            {
                if (IdleFishing.VFX.VFXManager.Instance != null)
                {
                    IdleFishing.VFX.VFXManager.Instance.SpawnConfetti(transform.position + Vector3.up * 1.0f, 1.2f);
                }
                int bonusGold = 150 + (speciesData != null ? speciesData.Tier * 50 : 0);
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
                        $"+{bonusGold} G BOSS!"
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

            yield return new WaitForSeconds(1.2f);

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

            if (stunStarsObj != null)
            {
                Destroy(stunStarsObj);
                stunStarsObj = null;
            }

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

            if (stunStarsObj != null)
            {
                Destroy(stunStarsObj);
                stunStarsObj = null;
            }
        }

        public void SetPickedUp(Transform carryParent)
        {
            isPickedUp = true;
            StopAllCoroutines();
            transform.SetParent(carryParent);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            if (stunStarsObj != null)
            {
                Destroy(stunStarsObj);
                stunStarsObj = null;
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
        }

        private void OnMouseDown()
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

        private void OnDestroy()
        {
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
