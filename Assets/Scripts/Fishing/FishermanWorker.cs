using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.UI;
using IdleFishing.VFX;
using IdleFishing.Audio;

namespace IdleFishing.Fishing
{
    public enum FishermanState
    {
        Idle,
        Casting,
        HookedReeling,
        CrowdedWait
    }

    public class FishermanWorker : MonoBehaviour
    {
        [Header("Spot & Landing References")]
        [SerializeField] private FishingSpot targetSpot;
        [SerializeField] private Transform landingZoneCenter;
        [SerializeField] private float landingZoneRadius = 2.0f;
        [SerializeField] private Transform fishingRodTip;

        [Header("Fishing Tuning")]
        [SerializeField] private float baseCastTime = 6.0f;
        [SerializeField] private float minCastTimeMultiplier = 0.65f;
        [SerializeField] private float maxCastTimeMultiplier = 1.45f;
        [SerializeField] private int maxPendingFishOnGround = 3;
        [SerializeField] private float bossSpawnChance = 0.12f;

        [Header("Visual Rod Animation")]
        [SerializeField] private Transform fishingRodTransform;
        [SerializeField] private FishingLineVisual lineVisual;

        private readonly List<LiveFish> activeLiveFish = new List<LiveFish>();
        private bool isFishing = false;
        private FishermanState currentState = FishermanState.Idle;
        private float currentCastTimer = 0f;
        private float currentCastDuration = 6.0f;

        public event Action<FishChainData> OnFishCaughtAndLanded;
        public event Action<float, float> OnFishingProgress; // (currentTimer, totalDuration)
        public event Action<FishermanState> OnStateChanged;

        public int ActiveLiveFishCount => activeLiveFish.Count;
        public bool IsFishing => isFishing;
        public FishermanState State => currentState;
        public float CurrentCastTimer => currentCastTimer;
        public float CurrentCastDuration => currentCastDuration;
        public float CurrentCastProgress => currentCastDuration > 0 ? Mathf.Clamp01(currentCastTimer / currentCastDuration) : 0f;

        private void Awake()
        {
            SetCollidersToIgnoreRaycast();
        }

        private void SetCollidersToIgnoreRaycast()
        {
            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycastLayer < 0) ignoreRaycastLayer = 2;
            var cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i].gameObject.layer = ignoreRaycastLayer;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
            {
                var cfg = GameManager.Instance.BalanceConfig;
                baseCastTime = cfg.fishermanBaseCastTime;
                maxPendingFishOnGround = cfg.maxPendingFishOnGround;
                bossSpawnChance = cfg.bossSpawnChance;
            }

            // Setup Floating Progress Bar above Fisherman
            if (GetComponentInChildren<FishermanProgressBarUI>() == null)
            {
                GameObject barObj = new GameObject("Fisherman_ProgressBar");
                barObj.transform.SetParent(transform, false);
                var pBar = barObj.AddComponent<FishermanProgressBarUI>();
                pBar.Setup(this, new Vector3(0, 1.9f, 0));
            }

            // Setup Fishing Line & Animated Bobber Visual
            if (lineVisual == null)
            {
                lineVisual = GetComponent<FishingLineVisual>();
                if (lineVisual == null)
                {
                    lineVisual = gameObject.AddComponent<FishingLineVisual>();
                }
            }
            Vector3 defaultSpot = targetSpot != null ? targetSpot.transform.position : transform.position + transform.forward * 4.5f;
            lineVisual.Setup(fishingRodTransform, fishingRodTip, defaultSpot);

            StartCoroutine(FishingRoutine());
        }

        private IEnumerator FishingRoutine()
        {
            // Khởi đầu lệch nhịp ngẫu nhiên để các thợ câu không bao giờ quăng cần cùng một lúc
            yield return YieldCache.WaitForSeconds(UnityEngine.Random.Range(0.15f, 2.6f));

            while (true)
            {
                // Clean up defeated or destroyed fish from list
                activeLiveFish.RemoveAll(f => f == null || f.IsDefeated || f.IsPickedUp);

                // If ground is crowded with angry flapping fish, wait a bit
                bool isCrowded = (FishingPierManager.Instance != null)
                    ? FishingPierManager.Instance.IsPierCrowded()
                    : (activeLiveFish.Count >= maxPendingFishOnGround);

                if (isCrowded)
                {
                    currentState = FishermanState.CrowdedWait;
                    currentCastTimer = 0f;
                    if (lineVisual != null) lineVisual.ResetToRest();
                    OnStateChanged?.Invoke(currentState);
                    yield return YieldCache.WaitForSeconds(UnityEngine.Random.Range(0.6f, 1.5f));
                    continue;
                }

                // 1. Casting & Waiting for bite (Buông cần & Chờ cá cắn câu)
                isFishing = true;
                currentState = FishermanState.Casting;
                OnStateChanged?.Invoke(currentState);

                float speedMultiplier = 1.0f;
                if (UpgradeManager.Instance != null)
                {
                    speedMultiplier = UpgradeManager.Instance.GetValue(UpgradeType.FishermanSpeed);
                }
                if (IdleFishing.Aquarium.BestiaryManager.Instance != null)
                {
                    speedMultiplier *= IdleFishing.Aquarium.BestiaryManager.Instance.GetReelSpeedMultiplier();
                }
                if (IdleFishing.World.LighthouseController.Instance != null)
                {
                    speedMultiplier *= IdleFishing.World.LighthouseController.Instance.GetFishingSpeedMultiplier();
                }
                if (IdleFishing.Gimmicks.WorldGimmickManager.Instance != null)
                {
                    speedMultiplier *= IdleFishing.Gimmicks.WorldGimmickManager.Instance.GetFishingSpeedMultiplier();
                }
                if (IdleFishing.Economy.PrestigeManager.Instance != null)
                {
                    speedMultiplier *= IdleFishing.Economy.PrestigeManager.Instance.GlobalSpeedMultiplier;
                }

                float frenzyMult = 1.0f;
                bool isFrenzy = (FishingFrenzyManager.Instance != null && FishingFrenzyManager.Instance.IsFrenzyActive);
                if (isFrenzy)
                {
                    frenzyMult = FishingFrenzyManager.Instance.SpeedMultiplier;
                }

                // Thời gian câu cá ngẫu nhiên trong khoảng cho mỗi lượt quăng cần
                float randomVariance = UnityEngine.Random.Range(minCastTimeMultiplier, maxCastTimeMultiplier);
                float randomizedBaseTime = baseCastTime * randomVariance;
                float minAllowedDuration = isFrenzy ? 0.65f : 1.6f;
                currentCastDuration = Mathf.Max(minAllowedDuration, randomizedBaseTime / (speedMultiplier * frenzyMult));
                currentCastTimer = 0f;

                // Kích hoạt hoạt ảnh vung cần, bay phao, chìm nước và sóng CFXR Water Ripples
                float targetZ = 13.80f; // Directly in front of pier at fishing water / ice holes
                float targetX = (lineVisual != null && lineVisual.RodTip != null)
                    ? lineVisual.RodTip.position.x
                    : (transform.position.x + 0.6f);
                Vector3 oceanTarget = new Vector3(targetX, 0.05f, targetZ);

                if (lineVisual != null)
                {
                    lineVisual.StartCast(oceanTarget, currentCastDuration);
                }

                float timer = 0f;
                while (timer < currentCastDuration)
                {
                    timer += Time.deltaTime;
                    currentCastTimer = timer;
                    OnFishingProgress?.Invoke(currentCastTimer, currentCastDuration);
                    yield return null;
                }

                currentCastTimer = currentCastDuration;
                currentState = FishermanState.HookedReeling;
                OnStateChanged?.Invoke(currentState);

                // Tính toán điểm cá/vật phẩm nhảy lên bờ
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * landingZoneRadius;
                Vector3 targetLandingPos = landingZoneCenter != null
                    ? landingZoneCenter.position + new Vector3(randomCircle.x, 0, randomCircle.y)
                    : transform.position + transform.forward * 3.0f + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Kích hoạt kéo cần giật mạnh và sóng CFXR rẽ nước
                float reelDuration = isFrenzy
                    ? UnityEngine.Random.Range(0.35f, 0.55f)
                    : UnityEngine.Random.Range(0.95f, 1.25f);
                if (lineVisual != null)
                {
                    lineVisual.StartReeling(targetLandingPos, reelDuration);
                }

                // 2. Hook and Reel (Check for Sunken Treasure Chest vs Normal Fish)
                float chestChance = 0.08f;
                if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
                {
                    chestChance = GameManager.Instance.BalanceConfig.treasureChestSpawnChance;
                }

                if (UnityEngine.Random.value < chestChance && SunkenTreasureChest.ActiveChestCount == 0)
                {
                    SpawnSunkenTreasureChest();
                }
                else if (targetSpot != null)
                {
                    float effectiveBossChance = bossSpawnChance;
                    if (IdleFishing.World.LighthouseController.Instance != null)
                    {
                        effectiveBossChance += IdleFishing.World.LighthouseController.Instance.GetBossChanceBonus();
                    }
                    if (IdleFishing.World.DayNightCycleManager.Instance != null)
                    {
                        effectiveBossChance *= IdleFishing.World.DayNightCycleManager.Instance.GetNightRareCatchMultiplier();
                    }

                    FishChainData caughtSpecies = null;
                    bool isBossCatch = false;

                    if (targetSpot != null && targetSpot.TryPopGuaranteedCatch(out var guaranteedFish, out var guaranteedBoss))
                    {
                        caughtSpecies = guaranteedFish;
                        isBossCatch = guaranteedBoss;
                    }
                    else
                    {
                        isBossCatch = UnityEngine.Random.value < effectiveBossChance;
                        caughtSpecies = isBossCatch
                            ? targetSpot.GetRandomBossFish()
                            : targetSpot.GetRandomFish();
                    }

                    if (caughtSpecies != null)
                    {
                        bool finalIsBoss = isBossCatch || caughtSpecies.IsApexBoss;
                        if (IdleFishing.Aquarium.BestiaryManager.Instance != null)
                        {
                            float weight = UnityEngine.Random.Range(caughtSpecies.MinWeightKg, caughtSpecies.MaxWeightKg);
                            if (finalIsBoss && !caughtSpecies.IsApexBoss) weight *= 2.2f;
                            IdleFishing.Aquarium.BestiaryManager.Instance.RegisterCatch(caughtSpecies, weight);
                        }
                        SpawnLandedFish(caughtSpecies, finalIsBoss);

                        // Abyssal Ghost Net Relic: Chance for Double Catch!
                        if (!finalIsBoss && IdleFishing.Economy.PrestigeManager.Instance != null && UnityEngine.Random.value < IdleFishing.Economy.PrestigeManager.Instance.DoubleCatchChance)
                        {
                            SpawnLandedFish(caughtSpecies, false);
                            if (FloatingDamageManager.Instance != null)
                            {
                                Vector3 textPos = transform.position + transform.forward * 2.5f + Vector3.up * 2.2f;
                                FloatingDamageManager.Instance.SpawnDamage(textPos, 0, DamageStyle.Crit, "[GHOST NET] DOUBLE CATCH!");
                            }
                        }
                    }
                }

                yield return YieldCache.WaitForSeconds(reelDuration);
                if (lineVisual != null)
                {
                    lineVisual.ResetToRest();
                }

                currentState = FishermanState.Idle;
                currentCastTimer = 0f;

                // Nghỉ ngắn ngẫu nhiên giữa 2 lần quăng cần (Frenzy quăng liên tục như súng liên thanh)
                float restDuration = isFrenzy
                    ? UnityEngine.Random.Range(0.05f, 0.15f)
                    : UnityEngine.Random.Range(0.25f, 1.15f);
                yield return YieldCache.WaitForSeconds(restDuration);
            }
        }

        private void SpawnSunkenTreasureChest()
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * landingZoneRadius;
            Vector3 targetLandingPos = landingZoneCenter != null
                ? landingZoneCenter.position + new Vector3(randomCircle.x, 0, randomCircle.y)
                : transform.position + transform.forward * 3.0f + new Vector3(randomCircle.x, 0, randomCircle.y);
            targetLandingPos.y = 0.64f; // Clean floor resting height for chest (pier floor = 0.62m, mesh min.y = -0.02m)

            Vector3 startSpawnPos = fishingRodTip != null ? fishingRodTip.position : transform.position + Vector3.up * 1.5f;

            GameObject chestObj = new GameObject("Sunken_Treasure_Chest");
            chestObj.transform.position = startSpawnPos;

            GameObject chestModelPrefab = null;
#if UNITY_EDITOR
            chestModelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Generated/Sunken_Treasure_Chest.obj");
#endif
            GameObject visualObj = null;
            if (chestModelPrefab != null)
            {
                visualObj = Instantiate(chestModelPrefab, chestObj.transform);
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.identity;
                visualObj.transform.localScale = Vector3.one * 1.0f;
            }
            else
            {
                visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObj.name = "Chest_Cube";
                visualObj.transform.SetParent(chestObj.transform, false);
                visualObj.transform.localScale = new Vector3(1.0f, 0.6f, 0.7f);
                if (visualObj.TryGetComponent<Collider>(out var c)) Destroy(c);
            }

            BoxCollider col = chestObj.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 1.0f, 1.2f);
            col.center = new Vector3(0, 0.50f, 0);

            var chest = chestObj.AddComponent<SunkenTreasureChest>();
            chest.SetVisualRoot(visualObj.transform);

            int reqTaps = 5;
            if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
            {
                reqTaps = GameManager.Instance.BalanceConfig.treasureChestRequiredTaps;
            }
            chest.Initialize(targetLandingPos, reqTaps);

            if (IdleFishing.CameraControl.MobilePortraitCameraController.Instance != null)
            {
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerShake(0.38f, 0.32f);
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerPunchFOV(-2.0f);
            }

            GameEvents.TriggerTreasureChestSpawned();
            Debug.Log("[FishermanWorker] Fisherman caught a SUNKEN TREASURE CHEST!");
        }

        private void SpawnLandedFish(FishChainData species, bool isBoss = false)
        {
            if (species == null || species.WholeFish == null || species.WholeFish.Prefab == null) return;

            // Compute landing spot on ground
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * landingZoneRadius;
            Vector3 targetLandingPos = landingZoneCenter != null
                ? landingZoneCenter.position + new Vector3(randomCircle.x, 0, randomCircle.y)
                : transform.position + transform.forward * 3.0f + new Vector3(randomCircle.x, 0, randomCircle.y);
            targetLandingPos.y = 0.62f; // Top surface of wooden pier planks

            Vector3 startSpawnPos = fishingRodTip != null ? fishingRodTip.position : transform.position + Vector3.up * 1.5f;

            // Create fish GameObject
            GameObject fishObj = new GameObject($"LiveFish_{species.SpeciesName}");
            fishObj.transform.position = startSpawnPos;

            // Instantiate fish visual model as child
            GameObject visualObj = Instantiate(species.WholeFish.Prefab, fishObj.transform);
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localRotation = Quaternion.identity;

            // Ensure collider for raycast / clicking (will be dynamically fitted to mesh by LiveFish.AlignGroundToFloor)
            BoxCollider col = fishObj.AddComponent<BoxCollider>();
            col.size = isBoss ? new Vector3(1.4f, 1.0f, 1.6f) : new Vector3(1.1f, 0.8f, 1.3f);
            col.center = new Vector3(0, 0.5f, 0);

            // Add LiveFish component with dynamic Size & Quality
            LiveFish liveFish = fishObj.AddComponent<LiveFish>();
            liveFish.SetVisualRoot(visualObj.transform);

            FishSizeCategory rolledSize = FishQualityData.RollRandomSize();
            FishQualityTier rolledQuality = FishQualityData.RollRandomQuality();

            liveFish.Initialize(species, targetLandingPos, isBoss, rolledSize, rolledQuality);
            activeLiveFish.Add(liveFish);
            if (FishingPierManager.Instance != null)
            {
                FishingPierManager.Instance.RegisterLiveFish(liveFish);
            }

            // Visual Juice & Announcements for Rare Catch (Golden, Rainbow, Colossal)
            if (rolledQuality == FishQualityTier.Golden || rolledQuality == FishQualityTier.Rainbow || rolledSize == FishSizeCategory.Colossal)
            {
                string announce = FishQualityData.GetQualityPrefix(rolledQuality, rolledSize).Trim();
                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        targetLandingPos + Vector3.up * 1.8f,
                        0,
                        DamageStyle.GoldBonus,
                        $"{announce} {species.SpeciesName}!"
                    );
                }

                if (IdleFishing.CameraControl.MobilePortraitCameraController.Instance != null)
                {
                    IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerShake(0.22f, 0.18f);
                }
            }

            // Water Splash & Ripple at ocean catch spot
            Vector3 oceanSplashPos = targetSpot != null ? targetSpot.transform.position : (transform.position + transform.forward * 4.2f);
            oceanSplashPos.y = 0.05f;
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSplash(oceanSplashPos, 1.25f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySplash(oceanSplashPos);
            }

            // Water Splash on dock landing floor & Drop sound
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSplash(targetLandingPos + Vector3.up * 0.05f, 0.7f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayFishDockDrop(targetLandingPos);
            }

            if (isBoss && IdleFishing.CameraControl.MobilePortraitCameraController.Instance != null)
            {
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerShake(0.48f, 0.38f);
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerPunchFOV(-2.5f);
            }

            GameEvents.TriggerFishCaught(species.WholeFish);
            if (isBoss)
            {
                GameEvents.TriggerBossFishSpawned(species);
            }
            OnFishCaughtAndLanded?.Invoke(species);

            Debug.Log($"[FishermanWorker] Fisherman caught {species.SpeciesName}! (Boss: {isBoss})");
        }

        public LiveFish GetNearestLivingFish(Vector3 fromPosition)
        {
            activeLiveFish.RemoveAll(f => f == null || f.IsPickedUp);

            LiveFish best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < activeLiveFish.Count; i++)
            {
                LiveFish fish = activeLiveFish[i];
                if (fish == null || fish.IsPickedUp) continue;

                float dist = Vector3.Distance(fromPosition, fish.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = fish;
                }
            }

            return best;
        }
    }
}
