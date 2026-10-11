using System;
using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Inventory;
using IdleFishing.UI;

namespace IdleFishing.Fishing
{
    public enum WranglerState
    {
        Idle,
        MovingToFish,
        AttackingFish,
        PickingUp,
        Delivering,
        Unloading,
        KnockedOut
    }

    public class FishWranglerWorker : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FishermanWorker fisherman;
        [SerializeField] private Inventory.Inventory intakeStorage;
        [SerializeField] private Transform carryPoint;
        [SerializeField] private Transform restWaypoint;
        [SerializeField] private Transform weaponTransform; // Gậy / Chùy đánh cá

        [Header("Health & Combat Stats")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private float knockoutDuration = 4.0f;
        [SerializeField] private float baseMoveSpeed = 3.8f;
        [SerializeField] private int baseAttackDamage = 16;
        [SerializeField] private float attackInterval = 0.6f;
        [SerializeField] private float stoppingDistance = 1.3f;

        [Header("Runtime State")]
        [SerializeField] private WranglerState currentState = WranglerState.Idle;
        [SerializeField] private LiveFish currentTargetFish;
        [SerializeField] private bool isKnockedOut = false;

        private float nextAttackTime = 0f;
        private bool isCarrying = false;
        private Vector3 weaponOriginalLocalPos;
        private FloatingHealthBarUI healthBar;
        private CrowdAvoidance crowdAgent;
        private IdleFishing.Characters.StickmanCartoonVisual stickmanVisual;

        // Renderers & Flash color
        private Renderer[] renderers;
        private Color[] originalColors;

        public event Action<WranglerState> OnStateChanged;
        public event Action<int, int> OnHealthChanged;

        public WranglerState State => currentState;
        public bool IsCarrying => isCarrying;
        public bool IsKnockedOut => isKnockedOut;
        public LiveFish TargetFish => currentTargetFish;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        private void Awake()
        {
            crowdAgent = GetComponent<CrowdAvoidance>();
            if (crowdAgent == null)
            {
                crowdAgent = gameObject.AddComponent<CrowdAvoidance>();
                crowdAgent.Setup(radius: 0.45f, priority: 1.1f);
            }

            if (weaponTransform != null)
            {
                weaponOriginalLocalPos = weaponTransform.localPosition;
            }
            stickmanVisual = GetComponentInChildren<IdleFishing.Characters.StickmanCartoonVisual>();
            CacheRenderers();
        }

        private void CacheRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>();
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

        private void OnEnable()
        {
            GameEvents.UpgradePurchased += OnUpgradePurchased;
        }

        private void OnDisable()
        {
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
        }

        private void OnUpgradePurchased(UpgradeType type, int level)
        {
            if (type == UpgradeType.WranglerPower)
            {
                int oldMax = maxHealth;
                UpdateMaxHealthFromUpgrades();
                currentHealth = Mathf.Min(maxHealth, currentHealth + (maxHealth - oldMax));
                if (healthBar != null)
                {
                    healthBar.UpdateHealth(currentHealth, maxHealth);
                }
            }
        }

        public void UpdateMaxHealthFromUpgrades()
        {
            float mult = 1.0f;
            if (UpgradeManager.Instance != null)
            {
                mult = UpgradeManager.Instance.GetValue(UpgradeType.WranglerPower);
            }
            int baseHp = 100;
            if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
            {
                baseHp = GameManager.Instance.BalanceConfig.wranglerBaseHealth;
            }
            maxHealth = Mathf.RoundToInt(baseHp * mult);
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
            {
                var cfg = GameManager.Instance.BalanceConfig;
                knockoutDuration = cfg.wranglerKnockoutDuration;
                baseMoveSpeed = cfg.wranglerBaseMoveSpeed;
                baseAttackDamage = cfg.wranglerBaseDamage;
                attackInterval = cfg.wranglerAttackInterval;
            }

            UpdateMaxHealthFromUpgrades();
            currentHealth = maxHealth;

            if (WranglerManager.Instance != null)
            {
                WranglerManager.Instance.RegisterWrangler(this);
            }

            // Spawn floating health bar above worker head
            GameObject barObj = new GameObject("Wrangler_HealthBar");
            healthBar = barObj.AddComponent<FloatingHealthBarUI>();
            healthBar.Setup(transform, "Fish Wrangler", new Vector3(0, 1.8f, 0), new Color(0.2f, 0.75f, 0.95f));
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }

        private void Update()
        {
            if (isKnockedOut) return;

            switch (currentState)
            {
                case WranglerState.Idle:
                    SearchForFish();
                    break;
                case WranglerState.MovingToFish:
                    HandleMovingToFish();
                    break;
                case WranglerState.AttackingFish:
                    HandleAttackingFish();
                    break;
                case WranglerState.Delivering:
                    HandleDelivering();
                    break;
            }
        }

        public void SetTargetIntake(Inventory.Inventory storage)
        {
            intakeStorage = storage;
        }

        public void SetFisherman(FishermanWorker fisher)
        {
            fisherman = fisher;
        }

        public void SetupRuntime(Inventory.Inventory storage, Transform cPoint, Transform rWaypoint, Transform wTransform)
        {
            intakeStorage = storage;
            carryPoint = cPoint;
            restWaypoint = rWaypoint;
            weaponTransform = wTransform;
            if (weaponTransform != null)
            {
                weaponOriginalLocalPos = weaponTransform.localPosition;
            }
        }

        private float GetActualMoveSpeed()
        {
            float mult = 1.0f;
            if (UpgradeManager.Instance != null)
            {
                mult = UpgradeManager.Instance.GetValue(UpgradeType.WranglerPower);
            }
            float speedMult = 1.0f + (mult - 1.0f) * 0.35f;
            if (FishingFrenzyManager.Instance != null && FishingFrenzyManager.Instance.IsFrenzyActive)
            {
                speedMult *= 1.35f;
            }
            return baseMoveSpeed * speedMult;
        }

        private int GetActualAttackDamage()
        {
            float mult = 1.0f;
            if (UpgradeManager.Instance != null)
            {
                mult = UpgradeManager.Instance.GetValue(UpgradeType.WranglerPower);
            }
            if (FishingFrenzyManager.Instance != null && FishingFrenzyManager.Instance.IsFrenzyActive)
            {
                mult *= FishingFrenzyManager.Instance.DamageMultiplier;
            }
            if (IdleFishing.Aquarium.BestiaryManager.Instance != null)
            {
                mult *= IdleFishing.Aquarium.BestiaryManager.Instance.GetWranglerAttackMultiplier();
            }
            return Mathf.RoundToInt(baseAttackDamage * mult);
        }

        private void SearchForFish()
        {
            LiveFish candidate = null;
            if (FishingPierManager.Instance != null)
            {
                candidate = FishingPierManager.Instance.GetNearestLivingFish(transform.position, this);
                if (candidate == null)
                {
                    candidate = FishingPierManager.Instance.GetNearestDefeatedFish(transform.position, this);
                }
            }
            else if (fisherman != null)
            {
                candidate = fisherman.GetNearestLivingFish(transform.position);
            }

            if (candidate != null && !candidate.IsPickedUp)
            {
                currentTargetFish = candidate;
                SetState(WranglerState.MovingToFish);
            }
            else if (restWaypoint != null && Vector3.Distance(transform.position, restWaypoint.position) > 0.5f)
            {
                Vector3 currentDest = restWaypoint.position;
                // If coming back from harbor ground onto the elevated pier, walk up the entrance ramp
                if (transform.position.z < 4.65f && restWaypoint.position.z >= 4.75f)
                {
                    currentDest = new Vector3(-2.5f, 0.60f, 4.75f);
                }
                MoveTowardsPosition(currentDest);
            }
        }

        public static float GetSurfaceY(Vector3 worldPos)
        {
            // Wooden Fishing Pier deck surface
            if (worldPos.x >= -6.0f && worldPos.x <= 6.0f && worldPos.z >= 4.75f && worldPos.z <= 11.25f)
            {
                return 0.60f;
            }
            // Ramp transition zone connecting harbor ground to pier deck
            if (worldPos.x >= -4.2f && worldPos.x <= -0.8f && worldPos.z >= 3.50f && worldPos.z < 4.75f)
            {
                float t = Mathf.InverseLerp(3.50f, 4.75f, worldPos.z);
                return Mathf.Lerp(0.05f, 0.60f, t);
            }
            return 0.05f; // Harbor stone/ground surface
        }

        private void HandleMovingToFish()
        {
            if (currentTargetFish == null || currentTargetFish.IsPickedUp)
            {
                SetState(WranglerState.Idle);
                return;
            }

            Vector3 targetPos = currentTargetFish.transform.position;
            // Angular combat encirclement offset so multiple wranglers surround fish rather than converging on single point
            if (!currentTargetFish.IsDefeated)
            {
                float combatAngle = (Mathf.Abs(GetInstanceID()) % 6) * 60f * Mathf.Deg2Rad;
                Vector3 combatOffset = new Vector3(Mathf.Cos(combatAngle), 0f, Mathf.Sin(combatAngle)) * 1.0f;
                targetPos += combatOffset;
            }
            targetPos.y = GetSurfaceY(targetPos);

            MoveTowardsPosition(targetPos);

            if (Vector3.Distance(transform.position, targetPos) <= stoppingDistance)
            {
                if (currentTargetFish.IsDefeated)
                {
                    StartCoroutine(PickUpRoutine());
                }
                else
                {
                    SetState(WranglerState.AttackingFish);
                }
            }
        }

        private void HandleAttackingFish()
        {
            if (currentTargetFish == null || currentTargetFish.IsPickedUp)
            {
                SetState(WranglerState.Idle);
                return;
            }

            // Look at fish
            Vector3 lookDir = (currentTargetFish.transform.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 12f);
            }

            // If fish hopped away outside attack range, chase it!
            float distToFish = Vector3.Distance(transform.position, currentTargetFish.transform.position);
            if (distToFish > stoppingDistance + 0.8f)
            {
                SetState(WranglerState.MovingToFish);
                return;
            }

            // If fish was defeated, pick it up!
            if (currentTargetFish.IsDefeated)
            {
                StartCoroutine(PickUpRoutine());
                return;
            }

            // Attack cooldown
            if (Time.time >= nextAttackTime)
            {
                PerformAttack();
                nextAttackTime = Time.time + attackInterval;
            }
        }

        private void PerformAttack()
        {
            if (currentTargetFish == null || currentTargetFish.IsDefeated) return;

            // Weapon swing animation
            if (stickmanVisual != null)
            {
                stickmanVisual.TriggerWhack(attackInterval * 0.55f);
            }
            else if (weaponTransform != null)
            {
                StartCoroutine(WeaponSwingRoutine());
            }

            int dmg = GetActualAttackDamage();
            bool willDefeat = currentTargetFish.CurrentHealth <= dmg;
            bool isBoss = currentTargetFish.IsBossFish;

            // Pass origin so fish plays hit recoil animation away from worker
            currentTargetFish.TakeDamage(dmg, isPlayer: false, hitOrigin: transform.position);

            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                if (willDefeat || isBoss)
                {
                    IdleFishing.Audio.AudioManager.Instance.PlayWranglerCritWhack(transform.position);
                }
                else
                {
                    IdleFishing.Audio.AudioManager.Instance.PlayWhack(transform.position);
                }
            }

            if (willDefeat && IdleFishing.CameraControl.MobilePortraitCameraController.Instance != null)
            {
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerShake(isBoss ? 0.35f : 0.20f, 0.16f);
            }
            if (IdleFishing.VFX.VFXManager.Instance != null)
            {
                Vector3 hitPos = currentTargetFish.transform.position + Vector3.up * 0.35f;
                IdleFishing.VFX.VFXManager.Instance.SpawnHitImpact(hitPos);
                IdleFishing.VFX.VFXManager.Instance.SpawnHitPunch(hitPos, 0.75f);
                IdleFishing.VFX.VFXManager.Instance.SpawnSwordSlash(hitPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), 0.75f);
            }

            Debug.Log($"[FishWrangler] Wrangler attacked fish! Dealt {dmg} damage (Fish HP: {currentTargetFish.CurrentHealth})");
        }

        private IEnumerator WeaponSwingRoutine()
        {
            if (weaponTransform == null) yield break;

            weaponTransform.localPosition = weaponOriginalLocalPos + transform.forward * 0.45f + Vector3.down * 0.2f;
            yield return new WaitForSeconds(0.12f);
            weaponTransform.localPosition = weaponOriginalLocalPos;
        }

        public void TakeFishHit(LiveFish fish, int damage)
        {
            if (isKnockedOut) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (healthBar != null)
            {
                healthBar.UpdateHealth(currentHealth, maxHealth);
            }

            // Spawn Red Floating Damage Text on worker!
            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 1.6f,
                    damage,
                    DamageStyle.FishAttack
                );
            }

            // Direction of attack impact
            Vector3 pushDir = (transform.position - fish.transform.position).normalized;
            pushDir.y = 0;

            // Play Worker Hit Reaction Animation!
            StartCoroutine(WorkerHitReactionRoutine(pushDir));

            if (currentHealth <= 0)
            {
                StartCoroutine(KnockoutRecoveryRoutine());
            }
        }

        private IEnumerator WorkerHitReactionRoutine(Vector3 pushDir)
        {
            // 1. Flash Red on worker
            SetRendererColor(new Color(1f, 0.25f, 0.25f));

            // 2. Flinch backward & knockback impulse
            Quaternion origRot = transform.rotation;
            transform.rotation = Quaternion.Euler(-25f, origRot.eulerAngles.y, 0f);
            transform.position += pushDir * 0.25f;

            if (weaponTransform != null)
            {
                weaponTransform.localPosition = weaponOriginalLocalPos - Vector3.forward * 0.2f;
            }

            yield return new WaitForSeconds(0.12f);

            // 3. Return to combat stance
            transform.rotation = origRot;
            if (weaponTransform != null)
            {
                weaponTransform.localPosition = weaponOriginalLocalPos;
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

        private IEnumerator KnockoutRecoveryRoutine()
        {
            isKnockedOut = true;
            if (crowdAgent != null) crowdAgent.IsStationary = true;
            SetState(WranglerState.KnockedOut);

            currentTargetFish = null;
            ResetRendererColor();

            // Worker falls backwards onto ground
            Quaternion fallenRot = Quaternion.Euler(80f, transform.eulerAngles.y, 0f);
            transform.rotation = fallenRot;
            if (stickmanVisual != null) stickmanVisual.SetKnockedOut(true);

            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayStun(transform.position);
            }
            if (IdleFishing.VFX.VFXManager.Instance != null)
            {
                IdleFishing.VFX.VFXManager.Instance.SpawnStunStars(transform, knockoutDuration, new Vector3(0f, 1.2f, 0.4f));
            }

            float elapsed = 0f;
            while (elapsed < knockoutDuration)
            {
                elapsed += Time.deltaTime;
                float remaining = Mathf.Max(0f, knockoutDuration - elapsed);

                if (healthBar != null)
                {
                    healthBar.SetStatusText($"KNOCKED OUT ({remaining:0.0}s)", isKnockedOut: true);
                }

                yield return null;
            }

            // Recover and stand back up
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            currentHealth = maxHealth;
            if (stickmanVisual != null) stickmanVisual.SetKnockedOut(false);

            if (healthBar != null)
            {
                healthBar.UpdateHealth(currentHealth, maxHealth);
                healthBar.SetStatusText(null, isKnockedOut: false);
            }

            isKnockedOut = false;
            if (crowdAgent != null) crowdAgent.IsStationary = false;
            SetState(WranglerState.Idle);

            Debug.Log("[FishWrangler] Wrangler recovered and resumed catching fish!");
        }

        private IEnumerator PickUpRoutine()
        {
            SetState(WranglerState.PickingUp);
            if (crowdAgent != null) crowdAgent.IsStationary = true;
            yield return new WaitForSeconds(0.3f);

            if (currentTargetFish != null)
            {
                Transform pParent = carryPoint != null ? carryPoint : transform;
                currentTargetFish.SetPickedUp(pParent);
                isCarrying = true;
            }

            if (crowdAgent != null) crowdAgent.IsStationary = false;
            SetState(WranglerState.Delivering);
        }

        private void HandleDelivering()
        {
            if (intakeStorage == null || currentTargetFish == null)
            {
                SetState(WranglerState.Idle);
                return;
            }

            // Phân tán điểm trả cá quanh thùng chứa theo từng thợ để không chụm đầu vào 1 điểm
            float deliveryAngle = (Mathf.Abs(GetInstanceID()) % 4) * 90f * Mathf.Deg2Rad;
            Vector3 deliveryOffset = new Vector3(Mathf.Cos(deliveryAngle), 0f, Mathf.Sin(deliveryAngle)) * 1.35f;
            Vector3 finalDest = intakeStorage.transform.position + deliveryOffset;
            finalDest.y = GetSurfaceY(finalDest);

            Vector3 chosenDest = finalDest;
            // If on the pier deck (Z >= 4.8), walk through the entrance ramp threshold first
            if (transform.position.z >= 4.8f)
            {
                chosenDest = new Vector3(-2.5f, 0.60f, 4.75f);
            }

            MoveTowardsPosition(chosenDest);

            float distToSlot = Vector3.Distance(transform.position, finalDest);
            float distToStorage = Vector3.Distance(transform.position, intakeStorage.transform.position);

            if (distToSlot <= 0.6f || distToStorage <= stoppingDistance + 0.6f)
            {
                StartCoroutine(UnloadFishRoutine());
            }
        }

        private IEnumerator UnloadFishRoutine()
        {
            SetState(WranglerState.Unloading);
            if (crowdAgent != null) crowdAgent.IsStationary = true;
            yield return new WaitForSeconds(0.3f);

            if (currentTargetFish != null && currentTargetFish.WholeFishItem != null)
            {
                bool sentToAquarium = false;
                if (IdleFishing.Aquarium.AquariumManager.Instance != null && currentTargetFish.SpeciesData != null)
                {
                    if (IdleFishing.Aquarium.AquariumManager.Instance.ShouldAutoTransferToAquarium(currentTargetFish.SpeciesData, currentTargetFish.IsBossFish))
                    {
                        float weight = UnityEngine.Random.Range(currentTargetFish.SpeciesData.MinWeightKg, currentTargetFish.SpeciesData.MaxWeightKg);
                        if (currentTargetFish.IsBossFish) weight *= 2.2f;

                        if (IdleFishing.Aquarium.AquariumManager.Instance.TryAddFish(currentTargetFish.SpeciesData, weight, currentTargetFish.IsBossFish, out _))
                        {
                            sentToAquarium = true;
                            if (FloatingDamageManager.Instance != null)
                            {
                                FloatingDamageManager.Instance.SpawnDamage(
                                    transform.position + Vector3.up * 1.5f,
                                    0,
                                    DamageStyle.GoldBonus,
                                    $"[+] AQUARIUM EXHIBIT: {currentTargetFish.SpeciesData.SpeciesName}"
                                );
                            }
                        }
                    }
                }

                if (!sentToAquarium && intakeStorage != null)
                {
                    intakeStorage.TryAdd(currentTargetFish.WholeFishItem, 1, out _);
                }

                if (IdleFishing.VFX.VFXManager.Instance != null)
                {
                    IdleFishing.VFX.VFXManager.Instance.SpawnDustPuff(transform.position + transform.forward * 0.45f + Vector3.up * 0.3f, 0.45f);
                }

                Destroy(currentTargetFish.gameObject);
                currentTargetFish = null;
            }

            isCarrying = false;
            yield return new WaitForSeconds(0.2f);

            if (crowdAgent != null) crowdAgent.IsStationary = false;
            SetState(WranglerState.Idle);
        }

        private void MoveTowardsPosition(Vector3 targetPos)
        {
            float targetY = GetSurfaceY(targetPos);
            targetPos.y = targetY;

            Vector3 nextPos;
            if (crowdAgent != null)
            {
                nextPos = crowdAgent.CalculateStep(targetPos, GetActualMoveSpeed(), Time.deltaTime);
            }
            else
            {
                nextPos = Vector3.MoveTowards(transform.position, targetPos, GetActualMoveSpeed() * Time.deltaTime);
            }

            // Smoothly adapt Y level to match the surface underneath
            float currentSurfaceY = GetSurfaceY(nextPos);
            nextPos.y = Mathf.MoveTowards(transform.position.y, currentSurfaceY, Time.deltaTime * 4.0f);

            Vector3 moveDir = (nextPos - transform.position).normalized;
            moveDir.y = 0;
            if (moveDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
            }

            transform.position = nextPos;
        }

        private void SetState(WranglerState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        private void OnDestroy()
        {
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
                healthBar = null;
            }
        }
    }
}
