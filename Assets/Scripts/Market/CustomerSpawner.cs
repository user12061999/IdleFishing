using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IdleFishing.Market
{
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private GameObject[] customerPrefabs;
        [SerializeField] private GameObject[] touristPrefabs;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private float spawnInterval = 3.0f;
        [SerializeField] private int maxActiveCustomers = 5;

        [Header("Available Shelves")]
        [SerializeField] private List<DisplayShelf> shelves = new List<DisplayShelf>();

        private readonly List<CustomerNPC> activeCustomers = new List<CustomerNPC>();

        private void Start()
        {
            // Đã bỏ cơ chế chợ chờ khách — Transporter tự động đi gom cá và đem đi bán trực tiếp
            var existingCustomers = FindObjectsByType<CustomerNPC>(FindObjectsSortMode.None);
            for (int i = 0; i < existingCustomers.Length; i++)
            {
                if (existingCustomers[i] != null)
                {
                    Destroy(existingCustomers[i].gameObject);
                }
            }
        }

        private IEnumerator TouristSpawnLoop()
        {
            while (true)
            {
                yield return IdleFishing.Core.YieldCache.WaitForSeconds(Random.Range(7f, 12f));

                if (IdleFishing.Aquarium.AquariumManager.Instance != null && 
                    IdleFishing.Aquarium.AquariumManager.Instance.CurrentFishCount > 0 &&
                    IdleFishing.Aquarium.AquariumTankVisual.Instance != null)
                {
                    SpawnAquariumTourist();
                }
            }
        }

        private void SpawnAquariumTourist()
        {
            if (spawnPoint == null) return;

            Vector3 spawnPos = spawnPoint.position + new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.15f, 0.15f));
            GameObject touristObj;

            GameObject selectedPrefab = (touristPrefabs != null && touristPrefabs.Length > 0)
                ? touristPrefabs[Random.Range(0, touristPrefabs.Length)]
                : ((customerPrefabs != null && customerPrefabs.Length > 0) ? customerPrefabs[Random.Range(0, customerPrefabs.Length)] : customerPrefab);

            if (selectedPrefab != null)
            {
                touristObj = Instantiate(selectedPrefab, spawnPos, spawnPoint.rotation);
                touristObj.name = "AquariumTourist";
            }
            else
            {
                touristObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                touristObj.name = "AquariumTourist";
                touristObj.transform.position = spawnPos;
                touristObj.transform.localScale = new Vector3(0.65f, 0.95f, 0.65f);
                var tCol = touristObj.GetComponent<CapsuleCollider>();
                if (tCol != null) tCol.isTrigger = true;

                var r = touristObj.GetComponent<Renderer>();
                if (r != null && r.material != null)
                {
                    Color[] touristColors = new Color[] {
                        new Color(0.2f, 0.85f, 0.95f),
                        new Color(1.0f, 0.85f, 0.25f),
                        new Color(0.85f, 0.45f, 0.95f)
                    };
                    Color c = touristColors[Random.Range(0, touristColors.Length)];
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", c);
                    else if (r.material.HasProperty("_Color")) r.material.color = c;
                }
            }

            Vector3 viewSpot = IdleFishing.Aquarium.AquariumTankVisual.Instance.transform.position + new Vector3(0, 0, -2.2f);
            var agent = touristObj.AddComponent<IdleFishing.Aquarium.AquariumTouristAgent>();
            agent.Initialize(viewSpot, exitPoint != null ? exitPoint : spawnPoint);
        }

        private int GetTotalStockOnShelves()
        {
            int total = 0;
            for (int i = 0; i < shelves.Count; i++)
            {
                if (shelves[i] != null && shelves[i].Inventory != null)
                {
                    total += shelves[i].Inventory.TotalItemCount;
                }
            }
            return total;
        }

        private void EnsureShelvesPopulated()
        {
            shelves.RemoveAll(s => s == null);
            if (shelves.Count == 0)
            {
                var found = FindObjectsByType<DisplayShelf>(FindObjectsSortMode.None);
                if (found != null && found.Length > 0)
                {
                    shelves.AddRange(found);
                }
            }
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                EnsureShelvesPopulated();

                int totalStock = GetTotalStockOnShelves();
                float currentInterval = spawnInterval;
                int dynamicMaxCustomers = Mathf.Max(6, maxActiveCustomers);

                // Lưu lượng khách tăng mạnh khi hàng tồn nhiều, giữ nhịp chợ không bao giờ tắt
                if (totalStock >= 14)
                {
                    currentInterval = 1.2f;
                    dynamicMaxCustomers = maxActiveCustomers + 3;
                }
                else if (totalStock >= 8)
                {
                    currentInterval = 1.6f;
                    dynamicMaxCustomers = maxActiveCustomers + 2;
                }
                else if (totalStock >= 1)
                {
                    currentInterval = 2.0f;
                    dynamicMaxCustomers = maxActiveCustomers + 1;
                }
                else
                {
                    currentInterval = 2.8f;
                    dynamicMaxCustomers = 3;
                }

                yield return IdleFishing.Core.YieldCache.WaitForSeconds(currentInterval);

                // Clean up nulls & stale stuck customers
                for (int i = activeCustomers.Count - 1; i >= 0; i--)
                {
                    var c = activeCustomers[i];
                    if (c == null || !c.gameObject.activeInHierarchy)
                    {
                        activeCustomers.RemoveAt(i);
                        continue;
                    }
                    if (c.LifeTime > 18.0f)
                    {
                        Destroy(c.gameObject);
                        activeCustomers.RemoveAt(i);
                    }
                }

                if (activeCustomers.Count < dynamicMaxCustomers && (HasAnyStockOnShelves() || totalStock > 0))
                {
                    SpawnCustomer();
                }
            }
        }

        private bool HasAnyStockOnShelves()
        {
            for (int i = 0; i < shelves.Count; i++)
            {
                if (shelves[i] != null && shelves[i].HasGoods) return true;
            }
            return false;
        }

        private void SpawnCustomer()
        {
            EnsureShelvesPopulated();
            if (spawnPoint == null || shelves.Count == 0) return;

            // Find a shelf that has goods, fallback to first available shelf
            List<DisplayShelf> available = shelves.FindAll(s => s != null && s.HasGoods);
            DisplayShelf chosen = (available.Count > 0) ? available[Random.Range(0, available.Count)] : shelves[0];
            if (chosen == null) return;

            Vector3 spawnPos = spawnPoint.position + new Vector3(Random.Range(-0.35f, 0.35f), 0, Random.Range(-0.15f, 0.15f));
            GameObject customerObj;

            GameObject selectedPrefab = (customerPrefabs != null && customerPrefabs.Length > 0)
                ? customerPrefabs[Random.Range(0, customerPrefabs.Length)]
                : customerPrefab;

            if (selectedPrefab != null)
            {
                customerObj = Instantiate(selectedPrefab, spawnPos, spawnPoint.rotation);
            }
            else
            {
                // Fallback procedural capsule if no custom prefab is assigned yet
                customerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                customerObj.name = "Customer_Capsule";
                customerObj.transform.position = spawnPos;
                customerObj.transform.localScale = new Vector3(0.7f, 1.0f, 0.7f);
                var cCol = customerObj.GetComponent<CapsuleCollider>();
                if (cCol != null) cCol.isTrigger = true;
            }

            CustomerNPC npc = customerObj.GetComponent<CustomerNPC>();
            if (npc == null)
            {
                npc = customerObj.AddComponent<CustomerNPC>();
            }

            npc.Initialize(chosen, exitPoint != null ? exitPoint : spawnPoint);
            activeCustomers.Add(npc);
        }
    }
}
