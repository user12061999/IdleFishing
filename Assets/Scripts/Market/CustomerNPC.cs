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

        public bool IsShopping => isShopping;

        private void Awake()
        {
            crowdAgent = GetComponent<CrowdAvoidance>();
            if (crowdAgent == null)
            {
                crowdAgent = gameObject.AddComponent<CrowdAvoidance>();
                crowdAgent.Setup(0.42f, 1.0f);
            }
        }

        public void Initialize(DisplayShelf shelf, Transform exit)
        {
            targetShelf = shelf;
            exitPoint = exit;
            StartCoroutine(ShoppingRoutine());
        }

        private IEnumerator ShoppingRoutine()
        {
            isShopping = true;

            // 1. Walk to shelf (slotted queue position)
            if (targetShelf != null)
            {
                Vector3 targetPos = targetShelf.RegisterAndGetStandPosition(this);
                targetPos.y = transform.position.y;

                while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetPos.x, 0, targetPos.z)) > 0.25f)
                {
                    Vector3 nextPos = crowdAgent != null
                        ? crowdAgent.CalculateStep(targetPos, walkSpeed, Time.deltaTime)
                        : Vector3.MoveTowards(transform.position, targetPos, walkSpeed * Time.deltaTime);

                    Vector3 moveDir = (nextPos - transform.position).normalized;
                    moveDir.y = 0;
                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
                    }
                    transform.position = nextPos;
                    yield return null;
                }

                // 2. Browse
                if (crowdAgent != null) crowdAgent.IsStationary = true;
                yield return new WaitForSeconds(browseTime);

                // 3. Purchase (Support buying up to 2-3 items if shelf has high stock)
                int buyCount = 1;
                if (targetShelf.Inventory != null)
                {
                    if (targetShelf.Inventory.TotalItemCount > 16) buyCount = 3;
                    else if (targetShelf.Inventory.TotalItemCount > 8) buyCount = 2;
                }

                int totalEarnings = 0;
                bool hadHotTrendItem = false;
                for (int b = 0; b < buyCount; b++)
                {
                    if (targetShelf.TryBuyOne(out ItemData item))
                    {
                        float multiplier = 1.0f;
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
                        Instantiate(coinDropPrefab, transform.position + Vector3.up * 1.0f, Quaternion.identity);
                    }

                    if (IdleFishing.VFX.VFXManager.Instance != null)
                    {
                        IdleFishing.VFX.VFXManager.Instance.SpawnCustomerHeart(transform.position + Vector3.up * 1.8f);
                    }
                }
            }

            // 4. Release shelf queue spot before walking to exit
            if (targetShelf != null)
            {
                targetShelf.UnregisterCustomer(this);
            }

            // 5. Walk to exit
            if (crowdAgent != null) crowdAgent.IsStationary = false;
            if (exitPoint != null)
            {
                Vector3 exitPos = exitPoint.position;
                exitPos.y = transform.position.y;

                while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(exitPos.x, 0, exitPos.z)) > 0.35f)
                {
                    Vector3 nextPos = crowdAgent != null
                        ? crowdAgent.CalculateStep(exitPos, walkSpeed, Time.deltaTime)
                        : Vector3.MoveTowards(transform.position, exitPos, walkSpeed * Time.deltaTime);

                    Vector3 moveDir = (nextPos - transform.position).normalized;
                    moveDir.y = 0;
                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
                    }
                    transform.position = nextPos;
                    yield return null;
                }
            }

            Destroy(gameObject);
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
