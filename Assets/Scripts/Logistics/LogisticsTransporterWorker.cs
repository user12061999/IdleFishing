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

        public TransporterState CurrentState => currentState;
        public int MaxCarryCapacity => maxCarryCapacity;
        public Inventory.Inventory CartInventory => cartInventory;

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
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(165, 38);
        }

        public void UpdateBadgeVisuals()
        {
            if (badgeText == null) return;

            switch (currentState)
            {
                case TransporterState.Idle:
                    badgeText.text = "<color=#94A3B8>[Rest]</color>";
                    if (badgeBg != null) badgeBg.color = new Color(0.1f, 0.12f, 0.16f, 0.85f);
                    break;

                case TransporterState.MovingToPickup:
                    badgeText.text = "<color=#38BDF8>To Pier Storage</color>";
                    if (badgeBg != null) badgeBg.color = new Color(0.08f, 0.18f, 0.28f, 0.88f);
                    break;

                case TransporterState.Loading:
                    badgeText.text = "<color=#FDE047>Loading...</color>";
                    if (badgeBg != null) badgeBg.color = new Color(0.22f, 0.20f, 0.06f, 0.90f);
                    break;

                case TransporterState.MovingToDropoff:
                case TransporterState.Unloading:
                    int count = cartInventory != null ? cartInventory.TotalItemCount : 0;
                    badgeText.text = $"<color=#38BDF8>Fresh Fish x{count}</color>";
                    if (badgeBg != null) badgeBg.color = new Color(0.08f, 0.22f, 0.32f, 0.90f);
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
                // 1. IDLE: Check routes
                currentState = TransporterState.Idle;
                activeRoute = null;
                UpdateBadgeVisuals();

                if (LogisticsManager.Instance != null)
                {
                    activeRoute = LogisticsManager.Instance.FindBestAvailableRoute(this);
                }

                if (activeRoute == null)
                {
                    // If no goods to haul and not at rest spot, stroll back to rest area
                    if (Vector3.Distance(transform.position, restPosition) > 0.5f)
                    {
                        yield return MoveTowardsPosition(restPosition);
                    }
                    yield return new WaitForSeconds(0.6f);
                    continue;
                }

                // Reserve items at source so duplicate transporters don't crowd the same route
                int toReserve = Mathf.Min(maxCarryCapacity, activeRoute.sourceInventory.GetCountOfType(activeRoute.itemType));
                activeRoute.ReserveItems(toReserve);

                // 2. MOVING TO PICKUP
                currentState = TransporterState.MovingToPickup;
                UpdateBadgeVisuals();
                if (activeRoute.waypointsToPickup != null && activeRoute.waypointsToPickup.Count > 0)
                {
                    for (int i = 0; i < activeRoute.waypointsToPickup.Count; i++)
                    {
                        yield return MoveTowardsPosition(activeRoute.waypointsToPickup[i]);
                    }
                }
                yield return MoveTowardsPosition(activeRoute.pickupPosition);

                // 3. LOADING GOODS INTO CART
                currentState = TransporterState.Loading;
                UpdateBadgeVisuals();
                yield return PerformLoadingRoutine(activeRoute);

                // Release reservation now that loading has taken the items
                activeRoute.ReleaseReservation(toReserve);

                if (cartInventory == null || cartInventory.TotalItemCount == 0)
                {
                    // Nothing was loaded, brief cooldown and re-evaluate
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }

                // 4. MOVING TO DROPOFF
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

                // 5. UNLOADING GOODS
                currentState = TransporterState.Unloading;
                UpdateBadgeVisuals();
                yield return PerformUnloadingRoutine(activeRoute);

                yield return new WaitForSeconds(0.25f);
            }
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
            return moveSpeed * mult;
        }

        private IEnumerator MoveTowardsPosition(Vector3 targetPos)
        {
            float targetY = FishWranglerWorker.GetSurfaceY(targetPos);
            targetPos.y = targetY;

            while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetPos.x, 0, targetPos.z)) > 0.3f)
            {
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
            if (route == null || route.sourceInventory == null || cartInventory == null) yield break;

            int toLoad = Mathf.Min(maxCarryCapacity - cartInventory.TotalItemCount, route.sourceInventory.TotalItemCount);
            if (toLoad <= 0) yield break;

            if (crowdAgent != null) crowdAgent.IsStationary = true;

            // Load items one by one with a brisk cadence
            int loadedCount = 0;
            for (int i = 0; i < toLoad; i++)
            {
                if (cartInventory.IsFull) break;

                if (route.sourceInventory.TryGetFirstItemOfType(route.itemType, out ItemData foundItem))
                {
                    if (route.sourceInventory.TryRemove(foundItem, 1))
                    {
                        cartInventory.TryAdd(foundItem, 1, out _);
                        loadedCount++;
                        UpdateBadgeVisuals();

                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlayUIPop(transform.position);
                        }
                        yield return new WaitForSeconds(0.12f);
                    }
                }
            }

            if (crowdAgent != null) crowdAgent.IsStationary = false;
            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator PerformUnloadingRoutine(LogisticsRoute route)
        {
            if (route == null || route.targetInventory == null || cartInventory == null) yield break;

            if (crowdAgent != null) crowdAgent.IsStationary = true;

            // Transfer all matching items from cart to target
            int unloadedCount = 0;
            string lastDeliveredName = "";

            while (cartInventory.TotalItemCount > 0)
            {
                if (route.targetInventory.IsFull)
                {
                    // Target has filled up, wait briefly
                    yield return new WaitForSeconds(0.5f);
                    if (route.targetInventory.IsFull) break;
                }

                if (cartInventory.TryPopFirst(out ItemData item))
                {
                    if (route.targetInventory.TryAdd(item, 1, out _))
                    {
                        unloadedCount++;
                        lastDeliveredName = item.ItemName;
                        UpdateBadgeVisuals();

                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlayUIPop(transform.position);
                        }
                        yield return new WaitForSeconds(0.12f);
                    }
                    else
                    {
                        // Couldn't accept, put back in cart
                        cartInventory.TryAdd(item, 1, out _);
                        break;
                    }
                }
            }

            // Visual floating delivery notification
            if (unloadedCount > 0 && FloatingDamageManager.Instance != null)
            {
                string tag = string.IsNullOrEmpty(lastDeliveredName) ? "Cargo" : lastDeliveredName;
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 1.6f,
                    0,
                    DamageStyle.GoldBonus,
                    $"+{unloadedCount} {tag}"
                );
            }

            if (crowdAgent != null) crowdAgent.IsStationary = false;
            yield return new WaitForSeconds(0.2f);
        }
    }
}
