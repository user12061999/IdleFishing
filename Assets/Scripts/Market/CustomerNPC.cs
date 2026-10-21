using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.UI;

namespace IdleFishing.Market
{
    public class CustomerNPC : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float browseTime = 0.8f;

        [Header("Coin FX (Optional)")]
        [SerializeField] private GameObject coinDropPrefab;

        private DisplayShelf targetShelf;
        private Transform exitPoint;
        private bool isShopping = false;
        private CrowdAvoidance crowdAgent;
        private float lifeTime = 0f;
        private const float MAX_LIFETIME = 22.0f;

        public bool IsShopping => isShopping;
        public float LifeTime => lifeTime;

        private void Awake()
        {
            crowdAgent = GetComponent<CrowdAvoidance>();
            if (crowdAgent == null)
            {
                crowdAgent = gameObject.AddComponent<CrowdAvoidance>();
                crowdAgent.Setup(0.42f, 1.0f);
            }

            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycastLayer < 0) ignoreRaycastLayer = 2;
            var cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i].gameObject.layer = ignoreRaycastLayer;
            }
        }

        private void Update()
        {
            lifeTime += Time.deltaTime;
            // Bảo hiểm toàn cục: Khách hàng không bao giờ bị kẹt vĩnh viễn trên bản đồ
            if (lifeTime > MAX_LIFETIME)
            {
                if (targetShelf != null)
                {
                    targetShelf.UnregisterCustomer(this);
                }
                Destroy(gameObject);
            }
        }

        public void Initialize(DisplayShelf shelf, Transform exit)
        {
            targetShelf = shelf;
            exitPoint = exit;
            StartCoroutine(ShoppingRoutine());
        }

        private IEnumerator MoveTowards(Vector3 targetPos, float stopDistance = 0.70f, float maxTimeout = 6.0f)
        {
            targetPos.y = transform.position.y;
            float elapsed = 0f;
            Vector3 lastPos = transform.position;
            float stuckTimer = 0f;

            while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetPos.x, 0, targetPos.z)) > stopDistance)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= maxTimeout)
                {
                    break; // Hết thời gian di chuyển fail-safe
                }

                if (Vector3.Distance(transform.position, lastPos) < 0.05f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= 1.25f) // Đã tới gần hoặc bị cản trở bởi người khác, kết thúc sớm
                    {
                        break;
                    }
                }
                else
                {
                    lastPos = transform.position;
                    stuckTimer = 0f;
                }

                Vector3 currentPos = transform.position;
                Vector3 nextPos = crowdAgent != null
                    ? crowdAgent.CalculateStep(targetPos, walkSpeed, Time.deltaTime)
                    : Vector3.MoveTowards(currentPos, targetPos, walkSpeed * Time.deltaTime);

                Vector3 moveDir = (nextPos - currentPos).normalized;
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
                }
                transform.position = nextPos;
                yield return null;
            }
        }

        private IEnumerator ShoppingRoutine()
        {
            isShopping = true;

            try
            {
                // 1. Walk to shelf (slotted queue position)
                if (targetShelf != null)
                {
                    Vector3 targetPos = targetShelf.RegisterAndGetStandPosition(this);
                    yield return MoveTowards(targetPos, stopDistance: 0.70f, maxTimeout: 6.0f);

                    // 2. Browse
                    if (crowdAgent != null) crowdAgent.IsStationary = true;
                    yield return IdleFishing.Core.YieldCache.WaitForSeconds(browseTime);

                    // 3. Purchase (Support buying up to 2-4 items if shelf has high stock to clear jams briskly)
                    int buyCount = 1;
                    if (targetShelf.Inventory != null)
                    {
                        if (targetShelf.Inventory.TotalItemCount >= 20) buyCount = 4;
                        else if (targetShelf.Inventory.TotalItemCount >= 14) buyCount = 3;
                        else if (targetShelf.Inventory.TotalItemCount >= 8) buyCount = 2;
                    }

                    int totalEarnings = 0;
                    bool hadHotTrendItem = false;
                    for (int b = 0; b < buyCount; b++)
                    {
                        if (targetShelf.TryBuyOne(out ItemData item))
                        {
                            float multiplier = 1.0f;

                            // Càng lên level cao thì giá trị hải sản càng cao (+25% mỗi cấp độ đảo)
                            if (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null)
                            {
                                int order = WorldManager.Instance.CurrentWorld.worldOrder;
                                multiplier *= (1.0f + (order - 1) * 0.25f);
                            }

                            if (UpgradeManager.Instance != null)
                            {
                                multiplier = UpgradeManager.Instance.GetValue(UpgradeType.MarketValueMultiplier);
                            }

                            // Xu Hướng Thị Trường (Market Trend)
                            if (MarketTrendManager.Instance != null)
                            {
                                multiplier *= MarketTrendManager.Instance.GetPriceMultiplier(item, out bool isHot);
                                if (isHot) hadHotTrendItem = true;
                            }

                            // Danh Tiếng Cảng Biển (Harbor Level Buff)
                            if (ContractManager.Instance != null)
                            {
                                multiplier *= ContractManager.Instance.GetGlobalPriceMultiplier();
                            }

                            // Sổ Tay Sinh Vật Biển (Bestiary Milestone Buff)
                            if (IdleFishing.Aquarium.BestiaryManager.Instance != null)
                            {
                                multiplier *= IdleFishing.Aquarium.BestiaryManager.Instance.GetGlobalMarketPriceMultiplier();
                            }

                            // Chợ Đêm Cảng Biển (Night Market Buff: +15% Gold)
                            if (IdleFishing.World.DayNightCycleManager.Instance != null)
                            {
                                multiplier *= IdleFishing.World.DayNightCycleManager.Instance.GetNightPriceMultiplier();
                            }

                            // Cơ Chế Đặc Quyền Vùng Đất (Signature World Gimmick: Pearl Frenzy x3, Desert Relics)
                            if (IdleFishing.Gimmicks.WorldGimmickManager.Instance != null)
                            {
                                multiplier *= IdleFishing.Gimmicks.WorldGimmickManager.Instance.GetGlobalPriceMultiplier();
                            }

                            int finalPrice = Mathf.RoundToInt(item.BasePrice * multiplier);
                            EconomyManager.Instance.AddCoins(finalPrice);
                            GameEvents.TriggerItemSold(item, finalPrice);
                            totalEarnings += finalPrice;
                        }
                    }

                    if (totalEarnings > 0)
                    {
                        // Visual Juice: Flying Coins to Top HUD & Gold popup
                        if (FlyingCoinManager.Instance != null)
                        {
                            FlyingCoinManager.Instance.PlayFlyingCoins(transform.position + Vector3.up * 1.0f, 3);
                        }
                        if (FloatingDamageManager.Instance != null)
                        {
                            FloatingDamageManager.Instance.SpawnDamage(transform.position + Vector3.up * 1.5f, totalEarnings, DamageStyle.GoldBonus);
                            if (hadHotTrendItem)
                            {
                                FloatingDamageManager.Instance.SpawnDamage(transform.position + Vector3.up * 2.3f, 0, DamageStyle.GoldBonus, "HOT TREND!");
                            }
                        }

                        if (coinDropPrefab != null)
                        {
                            if (IdleFishing.Core.ObjectPoolManager.Instance != null)
                            {
                                IdleFishing.Core.ObjectPoolManager.Instance.SpawnAutoRelease(coinDropPrefab, transform.position + Vector3.up * 1.0f, Quaternion.identity, 1.5f);
                            }
                            else
                            {
                                Instantiate(coinDropPrefab, transform.position + Vector3.up * 1.0f, Quaternion.identity);
                            }
                        }

                        if (IdleFishing.VFX.VFXManager.Instance != null)
                        {
                            IdleFishing.VFX.VFXManager.Instance.SpawnCustomerHeart(transform.position + Vector3.up * 1.8f);
                        }
                    }
                }
            }
            finally
            {
                // 4. Always release shelf queue spot before walking to exit
                if (targetShelf != null)
                {
                    targetShelf.UnregisterCustomer(this);
                }
            }

            // 5. Walk to exit
            if (crowdAgent != null) crowdAgent.IsStationary = false;
            if (exitPoint != null)
            {
                Vector3 exitPos = exitPoint.position;
                yield return MoveTowards(exitPos, stopDistance: 0.80f, maxTimeout: 6.5f);
            }

            Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (targetShelf != null)
            {
                targetShelf.UnregisterCustomer(this);
            }
        }

        private void OnDestroy()
        {
            if (targetShelf != null)
            {
                targetShelf.UnregisterCustomer(this);
            }
        }
    }
}
