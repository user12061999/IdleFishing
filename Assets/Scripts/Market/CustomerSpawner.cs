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
            StartCoroutine(SpawnLoop());
            StartCoroutine(TouristSpawnLoop());
        }

        private IEnumerator TouristSpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(7f, 12f));

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

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                int totalStock = GetTotalStockOnShelves();
                float currentInterval = spawnInterval;
                int dynamicMaxCustomers = maxActiveCustomers;

                // Dynamic rush hour when goods accumulate to prevent shelf blocking
                if (totalStock > 16)
                {
                    currentInterval = 1.8f;
                    dynamicMaxCustomers = maxActiveCustomers + 2;
                }
                else if (totalStock > 8)
                {
                    currentInterval = 2.4f;
                    dynamicMaxCustomers = maxActiveCustomers + 1;
                }

                yield return new WaitForSeconds(currentInterval);

                // Clean up nulls
                activeCustomers.RemoveAll(c => c == null);

                if (activeCustomers.Count < dynamicMaxCustomers && HasAnyStockOnShelves())
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
            if (spawnPoint == null || shelves.Count == 0) return;

            // Find a shelf that has goods
            List<DisplayShelf> available = shelves.FindAll(s => s != null && s.HasGoods);
            if (available.Count == 0) return;

            DisplayShelf chosen = available[Random.Range(0, available.Count)];

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
