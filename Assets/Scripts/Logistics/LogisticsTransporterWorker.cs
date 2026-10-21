using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Audio;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Fishing;
using IdleFishing.Inventory;
using IdleFishing.UI;

namespace IdleFishing.Logistics
{
    public enum TransporterState
    {
        Idle,
        MovingToPickup,
        Loading,
        MovingToDropoff,
        Unloading
    }

    public class LogisticsTransporterWorker : MonoBehaviour
    {
        [Header("Cart & Inventories")]
        [SerializeField] private Inventory.Inventory cartInventory;
        [SerializeField] private WorldItemStackVisualizer cartVisualizer;
        [SerializeField] private Transform cartRoot;
        [SerializeField] private Transform leftWheel;
        [SerializeField] private Transform rightWheel;

        [Header("Movement & Tuning")]
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float wheelRadius = 0.2f;
        [SerializeField] private int maxCarryCapacity = 3;

        [Header("Runtime State")]
        [SerializeField] private TransporterState currentState = TransporterState.Idle;
        [SerializeField] private LogisticsRoute activeRoute;
        [SerializeField] private Vector3 restPosition;

        [Header("Overhead Visual Badge")]
        [SerializeField] private GameObject overheadBadgeObj;
        [SerializeField] private TextMeshProUGUI badgeText;
        [SerializeField] private Image badgeBg;

        private Coroutine workerLoopCoroutine;
        private Vector3 lastPosition;
        private float walkBobTimer = 0f;
        private float dustTimer = 0f;
        private Vector3 initialCartLocalPos = Vector3.zero;
        private CrowdAvoidance crowdAgent;
        private Camera mainCamera;
        private readonly List<CarriedFishPayload> carriedPayloads = new List<CarriedFishPayload>();

        public TransporterState CurrentState => currentState;
        public int MaxCarryCapacity => maxCarryCapacity;
        public Inventory.Inventory CartInventory => cartInventory;
        public LogisticsRoute ActiveRoute => activeRoute;

        private void Awake()
        {
            crowdAgent = GetComponent<CrowdAvoidance>();
            if (crowdAgent == null)
            {
                crowdAgent = gameObject.AddComponent<CrowdAvoidance>();
                crowdAgent.Setup(radius: 0.65f, priority: 1.5f);
            }

            if (cartRoot != null)
            {
                initialCartLocalPos = cartRoot.localPosition;
            }
            lastPosition = transform.position;
            mainCamera = Camera.main;

            CreateOverheadBadge();

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
            if (cartInventory != null)
            {
                cartInventory.SetMaxCapacity(maxCarryCapacity);
            }
            UpdateBadgeVisuals();
            workerLoopCoroutine = StartCoroutine(WorkerLoop());
        }

        private void LateUpdate()
        {
            if (overheadBadgeObj != null)
            {
                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    overheadBadgeObj.transform.rotation = mainCamera.transform.rotation;
                }
                float bob = Mathf.Sin(Time.time * 3.2f) * 0.04f;
                overheadBadgeObj.transform.localPosition = new Vector3(0f, 2.2f + bob, 0f);
            }
        }

        private void OnDestroy()
        {
            if (workerLoopCoroutine != null)
            {
                StopCoroutine(workerLoopCoroutine);
            }
        }

        private void CreateOverheadBadge()
        {
            if (overheadBadgeObj != null) return;

            overheadBadgeObj = new GameObject("Overhead_Status_Badge");
            overheadBadgeObj.transform.SetParent(transform, false);
            overheadBadgeObj.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            overheadBadgeObj.transform.localScale = Vector3.one * 0.0075f;

            Canvas canvas = overheadBadgeObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rect = overheadBadgeObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(170, 42);

            GameObject bgObj = new GameObject("Badge_BG");
            bgObj.transform.SetParent(overheadBadgeObj.transform, false);
            badgeBg = bgObj.AddComponent<Image>();
            badgeBg.color = new Color(0.08f, 0.12f, 0.18f, 0.88f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(170, 42);

            GameObject textObj = new GameObject("Badge_Text");
            textObj.transform.SetParent(bgObj.transform, false);
            badgeText = textObj.AddComponent<TextMeshProUGUI>();
            badgeText.fontSize = 15;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.color = Color.white;
            badgeText.text = "[Rest]";
            badgeText.ApplyFont(UIFontCategory.Button);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(165, 38);
        }

        public void UpdateBadgeVisuals()
        {
            if (badgeText == null) return;

            switch (currentState)
            {
                case TransporterState.Idle:
                    badgeText.text = "<color=#94A3B8>[Idle]</color>";
                    if (badgeBg != null) badgeBg.color = new Color(0.1f, 0.12f, 0.16f, 0.85f);
                    break;

                case TransporterState.MovingToPickup:
                    badgeText.text = "<color=#38BDF8>Collecting</color>";
                    if (badgeBg != null) badgeBg.color = new Color(0.08f, 0.18f, 0.28f, 0.88f);
                    break;

                case TransporterState.Loading:
                    badgeText.text = "<color=#FDE047>Hauling...</color>";
                    if (badgeBg != null) badgeBg.color = new Color(0.22f, 0.20f, 0.06f, 0.90f);
                    break;

                case TransporterState.MovingToDropoff:
                    int count = cartInventory != null ? cartInventory.TotalItemCount : 0;
                    badgeText.text = $"<color=#4ADE80>Delivering x{count}</color>";
                    if (badgeBg != null) badgeBg.color = new Color(0.08f, 0.24f, 0.18f, 0.90f);
                    break;

                case TransporterState.Unloading:
                    badgeText.text = "<color=#FACC15>Selling! +$</color>";
                    if (badgeBg != null) badgeBg.color = new Color(0.24f, 0.18f, 0.06f, 0.92f);
                    break;
            }
        }

        public void Setup(Inventory.Inventory cInv, WorldItemStackVisualizer cVis, Transform cRoot, Transform lWheel, Transform rWheel, Vector3 restPos, float speed = 3.2f, int capacity = 3)
        {
            cartInventory = cInv;
            cartVisualizer = cVis;
            cartRoot = cRoot;
            leftWheel = lWheel;
            rightWheel = rWheel;
            restPosition = restPos;
            moveSpeed = speed;
            maxCarryCapacity = capacity;

            if (cartInventory != null)
            {
                cartInventory.SetMaxCapacity(maxCarryCapacity);
            }
            if (cartRoot != null)
            {
                initialCartLocalPos = cartRoot.localPosition;
            }
            CreateOverheadBadge();
            UpdateBadgeVisuals();
        }

        public void SetMoveSpeed(float newSpeed)
        {
            moveSpeed = newSpeed;
        }

        public void SetCapacity(int newCapacity)
        {
            maxCarryCapacity = newCapacity;
            if (cartInventory != null)
            {
                cartInventory.SetMaxCapacity(maxCarryCapacity);
            }
        }

        private IEnumerator WorkerLoop()
        {
            while (true)
            {
                // A. Nếu trên xe đẩy đã có cá, lập tức đem đi bán lấy tiền vàng (không cần chờ khách)
                if (cartInventory != null && cartInventory.TotalItemCount > 0)
                {
                    LogisticsRoute dropoffRoute = (LogisticsManager.Instance != null)
                        ? LogisticsManager.Instance.FindRouteForDropoff(cartInventory)
                        : null;

                    Vector3 sellPos = dropoffRoute != null ? dropoffRoute.dropoffPosition : new Vector3(0f, 0.5f, -4.5f);

                    currentState = TransporterState.MovingToDropoff;
                    UpdateBadgeVisuals();
                    if (dropoffRoute != null && dropoffRoute.waypointsToDropoff != null && dropoffRoute.waypointsToDropoff.Count > 0)
                    {
                        for (int i = 0; i < dropoffRoute.waypointsToDropoff.Count; i++)
                        {
                            yield return MoveTowardsPosition(dropoffRoute.waypointsToDropoff[i]);
                        }
                    }
                    yield return MoveTowardsPosition(sellPos);

                    currentState = TransporterState.Unloading;
                    UpdateBadgeVisuals();
                    yield return PerformUnloadingRoutine(dropoffRoute);
                    yield return YieldCache.WaitForSeconds(0.2f);
                    continue;
                }

                // 1. IDLE: Kiểm tra xem có cá trong Kho Bến Cảng hoặc cá đã đánh hạ trên bến tàu không
                currentState = TransporterState.Idle;
                activeRoute = null;
                UpdateBadgeVisuals();

                if (LogisticsManager.Instance != null)
                {
                    activeRoute = LogisticsManager.Instance.FindBestAvailableRoute(this);
                }

                if (activeRoute == null)
                {
                    // Nếu chưa có cá để gom, lùi về điểm nghỉ chờ cá
                    if (Vector3.Distance(transform.position, restPosition) > 0.5f)
                    {
                        yield return MoveTowardsPosition(restPosition);
                    }
                    yield return YieldCache.WaitForSeconds(0.3f);
                    continue;
                }

                // Đặt chỗ số lượng cá tại kho nguồn (Frenzy mở rộng sức chứa)
                int currentCapacity = GetEffectiveCarryCapacity();
                if (cartInventory != null && cartInventory.MaxCapacity != currentCapacity)
                {
                    cartInventory.SetMaxCapacity(currentCapacity);
                }
                int availInSource = activeRoute.sourceInventory != null
                    ? activeRoute.sourceInventory.GetCountOfType(activeRoute.itemType)
                    : 0;
                int toReserve = Mathf.Min(currentCapacity, availInSource);
                if (toReserve > 0)
                {
                    activeRoute.ReserveItems(toReserve);
                }

                try
                {
                    // 2. MOVING TO PICKUP (Tự động đi gom cá ở Kho Bến Cảng & trên bến)
                    currentState = TransporterState.MovingToPickup;
                    UpdateBadgeVisuals();

                    if (availInSource > 0)
                    {
                        if (activeRoute.waypointsToPickup != null && activeRoute.waypointsToPickup.Count > 0)
                        {
                            for (int i = 0; i < activeRoute.waypointsToPickup.Count; i++)
                            {
                                yield return MoveTowardsPosition(activeRoute.waypointsToPickup[i]);
                            }
                        }
                        yield return MoveTowardsPosition(activeRoute.pickupPosition);
                    }

                    // 3. LOADING GOODS INTO CART (Chất cá lên xe đẩy)
                    currentState = TransporterState.Loading;
                    UpdateBadgeVisuals();
                    yield return PerformLoadingRoutine(activeRoute);
                }
                finally
                {
                    if (toReserve > 0)
                    {
                        activeRoute.ReleaseReservation(toReserve);
                    }
                }

                if (cartInventory == null || cartInventory.TotalItemCount == 0)
                {
                    yield return YieldCache.WaitForSeconds(0.2f);
                    continue;
                }

                // 4. MOVING TO DROPOFF (Đẩy xe đem cá đi bán)
                currentState = TransporterState.MovingToDropoff;
                UpdateBadgeVisuals();
                if (activeRoute.waypointsToDropoff != null && activeRoute.waypointsToDropoff.Count > 0)
                {
                    for (int i = 0; i < activeRoute.waypointsToDropoff.Count; i++)
                    {
                        yield return MoveTowardsPosition(activeRoute.waypointsToDropoff[i]);
                    }
                }
                yield return MoveTowardsPosition(activeRoute.dropoffPosition);

                // 5. UNLOADING / AUTO-SELLING (Bán cá thu tiền vàng trực tiếp)
                currentState = TransporterState.Unloading;
                UpdateBadgeVisuals();
                yield return PerformUnloadingRoutine(activeRoute);

                yield return YieldCache.WaitForSeconds(0.2f);
            }
        }

        public int GetEffectiveCarryCapacity()
        {
            int cap = maxCarryCapacity;
            if (FishingFrenzyManager.Instance != null && FishingFrenzyManager.Instance.IsFrenzyActive)
            {
                cap = Mathf.Max(cap * 2, cap + 3);
            }
            return cap;
        }

        private float GetEffectiveMoveSpeed()
        {
            float mult = 1.0f;
            if (IdleFishing.Economy.UpgradeManager.Instance != null)
            {
                mult = 1.0f + (IdleFishing.Economy.UpgradeManager.Instance.GetValue(UpgradeType.KitchenSpeed) - 1.0f) * 0.5f;
            }
            if (IdleFishing.Market.ContractManager.Instance != null)
            {
                mult *= IdleFishing.Market.ContractManager.Instance.GetGlobalStationSpeedMultiplier();
            }
            if (IdleFishing.Economy.PrestigeManager.Instance != null)
            {
                mult *= IdleFishing.Economy.PrestigeManager.Instance.TransporterSpeedMultiplier;
            }
            if (FishingFrenzyManager.Instance != null && FishingFrenzyManager.Instance.IsFrenzyActive)
            {
                mult *= 1.85f;
            }
            return moveSpeed * mult;
        }

        private IEnumerator MoveTowardsPosition(Vector3 targetPos)
        {
            float targetY = FishWranglerWorker.GetSurfaceY(targetPos);
            targetPos.y = targetY;

            float moveTimeout = 9.0f; // Fail-safe timeout so worker never gets trapped forever
            float moveElapsed = 0f;
            Vector3 lastStuckPos = transform.position;
            float stuckTimer = 0f;

            while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetPos.x, 0, targetPos.z)) > 0.35f)
            {
                moveElapsed += Time.deltaTime;
                if (moveElapsed >= moveTimeout)
                {
                    break;
                }

                if (Vector3.Distance(transform.position, lastStuckPos) < 0.04f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= 2.5f) // Stationary blockage detection
                    {
                        break;
                    }
                }
                else
                {
                    lastStuckPos = transform.position;
                    stuckTimer = 0f;
                }

                Vector3 currentPos = transform.position;
                Vector3 nextPos;
                float effectiveSpeed = GetEffectiveMoveSpeed();

                if (crowdAgent != null)
                {
                    nextPos = crowdAgent.CalculateStep(targetPos, effectiveSpeed, Time.deltaTime);
                }
                else
                {
                    nextPos = Vector3.MoveTowards(currentPos, targetPos, effectiveSpeed * Time.deltaTime);
                }

                // Smoothly adapt Y level to match the surface underneath (elevated pier deck vs harbor ground)
                float currentSurfaceY = FishWranglerWorker.GetSurfaceY(nextPos);
                nextPos.y = Mathf.MoveTowards(transform.position.y, currentSurfaceY, Time.deltaTime * 4.0f);

                Vector3 moveDir = (nextPos - currentPos).normalized;
                moveDir.y = 0;

                // Rotate towards actual steered movement direction
                if (moveDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
                }

                transform.position = nextPos;

                // Rolling wheels visual animation
                float distMoved = Vector3.Distance(transform.position, currentPos);
                if (distMoved > 0.0001f)
                {
                    float angleDeg = (distMoved / Mathf.Max(0.05f, wheelRadius)) * Mathf.Rad2Deg;
                    if (leftWheel != null) leftWheel.Rotate(Vector3.right, angleDeg, Space.Self);
                    if (rightWheel != null) rightWheel.Rotate(Vector3.right, angleDeg, Space.Self);

                    // Walking bobbing
                    walkBobTimer += Time.deltaTime * 8f;
                    if (cartRoot != null)
                    {
                        float bob = Mathf.Sin(walkBobTimer) * 0.035f;
                        cartRoot.localPosition = initialCartLocalPos + new Vector3(0, bob, 0);
                    }

                    // Cartoon dust puff behind wheels
                    dustTimer += Time.deltaTime;
                    if (dustTimer >= 0.42f)
                    {
                        dustTimer = 0f;
                        if (IdleFishing.VFX.VFXManager.Instance != null)
                        {
                            Vector3 backOffset = -transform.forward * 0.45f + Vector3.up * 0.08f;
                            IdleFishing.VFX.VFXManager.Instance.SpawnDustPuff(transform.position + backOffset, 0.45f);
                        }
                    }
                }

                yield return null;
            }

            // Snap position & reset cart bob
            transform.position = new Vector3(targetPos.x, FishWranglerWorker.GetSurfaceY(targetPos), targetPos.z);
            if (cartRoot != null)
            {
                cartRoot.localPosition = initialCartLocalPos;
            }
        }

        private IEnumerator PerformLoadingRoutine(LogisticsRoute route)
        {
            if (cartInventory == null) yield break;

            // 1. Gom cá từ Kho Bến Cảng (Harbor_IntakeStorage) nếu có
            if (route != null && route.sourceInventory != null && route.sourceInventory.TotalItemCount > 0)
            {
                int currentCap = GetEffectiveCarryCapacity();
                if (cartInventory.MaxCapacity != currentCap)
                {
                    cartInventory.SetMaxCapacity(currentCap);
                }
                int toLoad = Mathf.Min(currentCap - cartInventory.TotalItemCount, route.sourceInventory.TotalItemCount);
                if (toLoad > 0)
                {
                    if (crowdAgent != null) crowdAgent.IsStationary = true;
                    var meta = route.sourceInventory.GetComponent<IntakeStorageMetadata>();

                    for (int i = 0; i < toLoad; i++)
                    {
                        if (cartInventory.IsFull) break;

                        if (route.sourceInventory.TryGetFirstItemOfType(route.itemType, out ItemData foundItem))
                        {
                            if (route.sourceInventory.TryRemove(foundItem, 1))
                            {
                                cartInventory.TryAdd(foundItem, 1, out _);

                                CarriedFishPayload payload;
                                if (meta == null || !meta.TryRetrieveFish(out payload))
                                {
                                    payload = new CarriedFishPayload(foundItem, FishQualityData.RollRandomSize(), FishQualityData.RollRandomQuality());
                                }
                                carriedPayloads.Add(payload);

                                UpdateBadgeVisuals();

                                if (AudioManager.Instance != null)
                                {
                                    AudioManager.Instance.PlayUIPop(transform.position);
                                }
                                yield return YieldCache.WaitForSeconds(0.10f);
                            }
                        }
                    }

                    if (crowdAgent != null) crowdAgent.IsStationary = false;
                }
            }

            // 2. Nếu xe đẩy vẫn còn chỗ trống và trên bến cảng có cá đã bị đánh hạ, tự động đi gom luôn lên xe
            while (!cartInventory.IsFull && FishingPierManager.Instance != null)
            {
                LiveFish defeatedFish = FishingPierManager.Instance.GetNearestDefeatedFish(transform.position);
                if (defeatedFish == null || defeatedFish.WholeFishItem == null) break;

                defeatedFish.MarkReservedForPickup(6.0f);
                currentState = TransporterState.MovingToPickup;
                UpdateBadgeVisuals();

                Vector3 fishPos = defeatedFish.transform.position;
                if (transform.position.z < 4.65f && fishPos.z >= 4.75f)
                {
                    yield return MoveTowardsPosition(new Vector3(-2.5f, 0.60f, 4.75f));
                }

                if (defeatedFish != null && !defeatedFish.IsPickedUp)
                {
                    yield return MoveTowardsPosition(defeatedFish.transform.position);
                }

                if (defeatedFish != null && !defeatedFish.IsPickedUp && defeatedFish.WholeFishItem != null)
                {
                    currentState = TransporterState.Loading;
                    UpdateBadgeVisuals();

                    if (cartInventory.TryAdd(defeatedFish.WholeFishItem, 1, out _))
                    {
                        carriedPayloads.Add(new CarriedFishPayload(defeatedFish.WholeFishItem, defeatedFish.SizeCategory, defeatedFish.QualityTier));

                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlayUIPop(transform.position);
                        }
                        if (IdleFishing.VFX.VFXManager.Instance != null)
                        {
                            IdleFishing.VFX.VFXManager.Instance.SpawnDustPuff(defeatedFish.transform.position + Vector3.up * 0.25f, 0.4f);
                        }
                        Destroy(defeatedFish.gameObject);
                        UpdateBadgeVisuals();
                        yield return YieldCache.WaitForSeconds(0.12f);
                    }
                    else
                    {
                        defeatedFish.ClearReservation();
                        break;
                    }
                }
            }

            // Nếu đang ở trên sàn gỗ bến câu, đi qua lối dốc xuống cảng trước khi đi bán
            if (transform.position.z >= 4.75f)
            {
                currentState = TransporterState.MovingToDropoff;
                UpdateBadgeVisuals();
                yield return MoveTowardsPosition(new Vector3(-2.5f, 0.60f, 4.4f));
            }

            if (crowdAgent != null) crowdAgent.IsStationary = false;
            yield return YieldCache.WaitForSeconds(0.15f);
        }

        public static int CalculateDirectSellPrice(ItemData item, out bool isHotTrend)
        {
            isHotTrend = false;
            if (item == null) return 0;

            float multiplier = 1.0f;

            // Càng lên level cao thì giá trị hải sản càng cao (+25% mỗi cấp độ đảo)
            if (IdleFishing.Core.WorldManager.Instance != null && IdleFishing.Core.WorldManager.Instance.CurrentWorld != null)
            {
                int order = IdleFishing.Core.WorldManager.Instance.CurrentWorld.worldOrder;
                multiplier *= (1.0f + (order - 1) * 0.25f);
            }

            if (IdleFishing.Economy.UpgradeManager.Instance != null)
            {
                multiplier *= IdleFishing.Economy.UpgradeManager.Instance.GetValue(UpgradeType.MarketValueMultiplier);
            }

            if (IdleFishing.Market.MarketTrendManager.Instance != null)
            {
                multiplier *= IdleFishing.Market.MarketTrendManager.Instance.GetPriceMultiplier(item, out isHotTrend);
            }

            if (IdleFishing.Market.ContractManager.Instance != null)
            {
                multiplier *= IdleFishing.Market.ContractManager.Instance.GetGlobalPriceMultiplier();
            }

            if (IdleFishing.Aquarium.BestiaryManager.Instance != null)
            {
                multiplier *= IdleFishing.Aquarium.BestiaryManager.Instance.GetGlobalMarketPriceMultiplier();
            }

            if (IdleFishing.World.DayNightCycleManager.Instance != null)
            {
                multiplier *= IdleFishing.World.DayNightCycleManager.Instance.GetNightPriceMultiplier();
            }

            if (IdleFishing.Gimmicks.WorldGimmickManager.Instance != null)
            {
                multiplier *= IdleFishing.Gimmicks.WorldGimmickManager.Instance.GetGlobalPriceMultiplier();
            }

            if (FishingFrenzyManager.Instance != null && FishingFrenzyManager.Instance.IsFrenzyActive)
            {
                multiplier *= 2.0f;
            }

            return Mathf.Max(1, Mathf.RoundToInt(item.BasePrice * multiplier));
        }

        private IEnumerator PerformUnloadingRoutine(LogisticsRoute route)
        {
            if (cartInventory == null) yield break;

            if (crowdAgent != null) crowdAgent.IsStationary = true;

            int soldCount = 0;
            int totalEarnings = 0;
            bool hadHotTrendItem = false;
            string topSpecialCatchTag = null;

            // Nếu kệ cũ (targetInventory) còn sót cá tồn đọng, bán sạch luôn
            if (route != null && route.targetInventory != null && route.targetInventory.TotalItemCount > 0)
            {
                while (route.targetInventory.TryPopFirst(out ItemData shelfItem))
                {
                    int p = CalculateDirectSellPrice(shelfItem, out bool hot);
                    if (hot) hadHotTrendItem = true;
                    if (IdleFishing.Economy.EconomyManager.Instance != null)
                    {
                        IdleFishing.Economy.EconomyManager.Instance.AddCoins(p);
                    }
                    GameEvents.TriggerItemSold(shelfItem, p);
                    totalEarnings += p;
                    soldCount++;
                }
            }

            // Bán trực tiếp toàn bộ cá trên xe đẩy thành Tiền Vàng (có áp dụng hệ số Size & Quality)
            while (cartInventory.TotalItemCount > 0)
            {
                if (cartInventory.TryPopFirst(out ItemData item))
                {
                    CarriedFishPayload payload = carriedPayloads.Count > 0
                        ? carriedPayloads[0]
                        : new CarriedFishPayload(item, FishSizeCategory.Standard, FishQualityTier.Regular);
                    if (carriedPayloads.Count > 0) carriedPayloads.RemoveAt(0);

                    float qMult = FishQualityData.GetQualityPriceMultiplier(payload.quality);
                    float sMult = FishQualityData.GetSizePriceMultiplier(payload.size);

                    int basePrice = CalculateDirectSellPrice(item, out bool isHot);
                    if (isHot) hadHotTrendItem = true;

                    int finalPrice = Mathf.RoundToInt(basePrice * qMult * sMult);

                    if (payload.quality == FishQualityTier.Golden || payload.quality == FishQualityTier.Rainbow || payload.size == FishSizeCategory.Colossal)
                    {
                        topSpecialCatchTag = $"{FishQualityData.GetQualityPrefix(payload.quality, payload.size).Trim()}!";
                    }

                    if (IdleFishing.Economy.EconomyManager.Instance != null)
                    {
                        IdleFishing.Economy.EconomyManager.Instance.AddCoins(finalPrice);
                    }
                    GameEvents.TriggerItemSold(item, finalPrice);
                    GameEvents.TriggerPayloadTransported(1);

                    totalEarnings += finalPrice;
                    soldCount++;
                    UpdateBadgeVisuals();

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayUIPop(transform.position);
                    }
                    yield return YieldCache.WaitForSeconds(0.10f);
                }
                else
                {
                    break;
                }
            }

            carriedPayloads.Clear();

            // Hiệu ứng Tiền Vàng bay về HUD & Popup số vàng rực rỡ
            if (totalEarnings > 0)
            {
                bool isFrenzy = FishingFrenzyManager.Instance != null && FishingFrenzyManager.Instance.IsFrenzyActive;
                int coinCount = isFrenzy ? 12 : (!string.IsNullOrEmpty(topSpecialCatchTag) ? 7 : Mathf.Clamp(soldCount + 1, 3, 5));

                if (FlyingCoinManager.Instance != null)
                {
                    FlyingCoinManager.Instance.PlayFlyingCoins(transform.position + Vector3.up * 1.1f, coinCount);
                }

                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        transform.position + Vector3.up * 1.6f,
                        totalEarnings,
                        DamageStyle.GoldBonus,
                        $"+{NumberFormatter.Format(totalEarnings)} G ({soldCount} Fish)"
                    );

                    if (isFrenzy)
                    {
                        FloatingDamageManager.Instance.SpawnDamage(
                            transform.position + Vector3.up * 2.3f,
                            0,
                            DamageStyle.Crit,
                            "[FRENZY] 2X GOLD JACKPOT!"
                        );
                    }
                    else if (!string.IsNullOrEmpty(topSpecialCatchTag))
                    {
                        FloatingDamageManager.Instance.SpawnDamage(
                            transform.position + Vector3.up * 2.3f,
                            0,
                            DamageStyle.Crit,
                            $"{topSpecialCatchTag} JACKPOT!"
                        );
                    }
                    else if (hadHotTrendItem)
                    {
                        FloatingDamageManager.Instance.SpawnDamage(
                            transform.position + Vector3.up * 2.3f,
                            0,
                            DamageStyle.GoldBonus,
                            "HOT TREND!"
                        );
                    }
                }
            }

            if (crowdAgent != null) crowdAgent.IsStationary = false;
            yield return YieldCache.WaitForSeconds(0.15f);
        }
    }
}
