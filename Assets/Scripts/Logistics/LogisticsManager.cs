using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Inventory;
using IdleFishing.UI;
using IdleFishing.World;

namespace IdleFishing.Logistics
{
    public class LogisticsManager : MonoBehaviour
    {
        public static LogisticsManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private List<LogisticsRoute> routes = new List<LogisticsRoute>();
        [SerializeField] private List<LogisticsTransporterWorker> activeWorkers = new List<LogisticsTransporterWorker>();

        [Header("Prefabs & Spawn")]
        [SerializeField] private GameObject workerModelPrefab;
        [SerializeField] private Vector3 defaultRestPosition = new Vector3(-3.5f, 0.5f, 0.5f);

        [Header("Hire Station Pad")]
        [SerializeField] private GameObject hirePadObj;
        [SerializeField] private InWorldPurchasePad hirePad;
        [SerializeField] private WorldTransporterHirePad transporterHirePad;
        [SerializeField] private int maxTransporters = 2;
        [SerializeField] private int hireCost = 120;

        public IReadOnlyList<LogisticsRoute> Routes => routes;
        public IReadOnlyList<LogisticsTransporterWorker> ActiveWorkers => activeWorkers;
        public int WorkerCount => activeWorkers.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        [Header("Progression Gating")]
        [SerializeField] private bool autoSpawnFirstWorkerOnStart = true;

        private void Start()
        {
            if (transporterHirePad == null)
            {
                transporterHirePad = FindAnyObjectByType<WorldTransporterHirePad>();
            }

            // Early tutorial check: If Stage 1 or 2 is active, hold off first transporter until Stage 3
            bool isEarlyTutorial = false;
            if (IdleFishing.Progression.StageProgressionManager.Instance != null &&
                IdleFishing.Progression.StageProgressionManager.Instance.CurrentStageNumber < 3)
            {
                isEarlyTutorial = true;
            }

            // Spawn default 1st Transporter worker if none exists and not early tutorial
            if (!isEarlyTutorial && activeWorkers.Count == 0 && autoSpawnFirstWorkerOnStart)
            {
                SpawnTransporterWorker(0);
            }
            UpdateHirePadVisuals();
        }

        public void RegisterRoute(LogisticsRoute route)
        {
            if (route != null && !routes.Contains(route))
            {
                routes.Add(route);
                routes.Sort((a, b) => a.priority.CompareTo(b.priority));
            }
        }

        public void RegisterDynamicRoute(LogisticsRoute route)
        {
            if (route != null && !routes.Contains(route))
            {
                routes.Insert(0, route); // Ưu tiên hàng đầu
            }
        }

        public void UnregisterRoute(LogisticsRoute route)
        {
            if (route != null && routes.Contains(route))
            {
                routes.Remove(route);
            }
        }

        public void UnregisterRouteByName(string routeName)
        {
            routes.RemoveAll(r => r != null && r.routeName == routeName);
        }

        public LogisticsRoute FindBestAvailableRoute(LogisticsTransporterWorker requester)
        {
            if (routes == null || routes.Count == 0) return null;

            LogisticsRoute bestRoute = null;
            float bestScore = float.MaxValue;
            Vector3 workerPos = requester != null ? requester.transform.position : Vector3.zero;

            for (int i = 0; i < routes.Count; i++)
            {
                LogisticsRoute r = routes[i];
                if (r == null || !r.CanExecute()) continue;

                RouteUrgencyLevel urgency = r.GetDynamicUrgency();
                float dist = Vector3.Distance(workerPos, r.pickupPosition);

                // Điểm số ưu tiên: Urgency có trọng số 1000, khoảng cách quãng đường 1, priority tĩnh 10
                float score = (int)urgency * 1000f + dist + (r.priority * 10f);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestRoute = r;
                }
            }
            return bestRoute;
        }

        public LogisticsRoute FindRouteForDropoff(Inventory.Inventory cartInv)
        {
            if (routes == null || routes.Count == 0 || cartInv == null || cartInv.TotalItemCount == 0) return null;

            for (int i = 0; i < routes.Count; i++)
            {
                var r = routes[i];
                if (r == null) continue;

                if (cartInv.GetCountOfType(r.itemType) > 0)
                {
                    return r;
                }
            }
            return routes[0];
        }

        public bool IsMarketShelfFull()
        {
            // Đã bỏ cơ chế chợ chờ khách — Transporter bán cá trực tiếp nên không bao giờ bị đầy
            return false;
        }

        public void SetupHirePad(GameObject padObj, InWorldPurchasePad padScript, Vector3 restPos, GameObject modelPrefab = null)
        {
            hirePadObj = padObj;
            hirePad = padScript;
            defaultRestPosition = restPos;
            if (modelPrefab != null) workerModelPrefab = modelPrefab;
            UpdateHirePadVisuals();
        }

        public void SetupHirePad(GameObject padObj, WorldTransporterHirePad padScript, Vector3 restPos, GameObject modelPrefab = null)
        {
            hirePadObj = padObj;
            transporterHirePad = padScript;
            defaultRestPosition = restPos;
            if (modelPrefab != null) workerModelPrefab = modelPrefab;
            UpdateHirePadVisuals();
        }

        public bool TryHireAdditionalTransporter()
        {
            if (activeWorkers.Count >= maxTransporters) return false;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendCoins(hireCost)) return false;

            int index = activeWorkers.Count;
            var newWorker = SpawnTransporterWorker(index);

            if (FloatingDamageManager.Instance != null && newWorker != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    newWorker.transform.position + Vector3.up * 2f,
                    0,
                    DamageStyle.GoldBonus,
                    $"[!] HIRED TRANSPORTER #{index + 1}! [!]"
                );
            }

            UpdateHirePadVisuals();
            return true;
        }

        public LogisticsTransporterWorker SpawnTransporterWorker(int index)
        {
            Vector3 spawnPos = defaultRestPosition + new Vector3(index * 1.5f, 0, 0);
            spawnPos.y = 0.05f;

            GameObject workerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            workerObj.name = $"Logistics_Transporter_{index + 1}";
            workerObj.transform.SetParent(transform);
            workerObj.transform.position = spawnPos;
            workerObj.transform.localScale = new Vector3(0.8f, 1.0f, 0.8f);
            var logCol = workerObj.GetComponent<CapsuleCollider>();
            if (logCol != null) logCol.isTrigger = true;

            // Visual Humanoid Model
            GameObject modelPrefab = workerModelPrefab;
#if UNITY_EDITOR
            if (modelPrefab == null)
            {
                modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Stickman_Transporter.prefab");
            }
#endif
            if (modelPrefab != null)
            {
                var vis = Instantiate(modelPrefab, workerObj.transform);
                vis.name = "Model_Porter";
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localRotation = Quaternion.identity;
                vis.transform.localScale = Vector3.one;
                if (workerObj.TryGetComponent<MeshRenderer>(out var mr)) mr.enabled = false;
            }

            // Procedural Cute Wooden Cart
            GameObject cartRoot = new GameObject("Cart_Root");
            cartRoot.transform.SetParent(workerObj.transform, false);
            cartRoot.transform.localPosition = new Vector3(0f, 0.0f, 0.85f);

            // 1. Cart Wooden Box
            GameObject cartBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cartBox.name = "Cart_WoodenBox";
            cartBox.transform.SetParent(cartRoot.transform, false);
            cartBox.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            cartBox.transform.localScale = new Vector3(0.68f, 0.32f, 0.85f);
            SetObjectColor(cartBox, new Color(0.62f, 0.40f, 0.22f)); // Wood Brown
            RemoveCollider(cartBox);

            // 2. Wheels
            GameObject leftWheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftWheel.name = "Cart_Wheel_L";
            leftWheel.transform.SetParent(cartRoot.transform, false);
            leftWheel.transform.localPosition = new Vector3(-0.40f, 0.12f, 0f);
            leftWheel.transform.localScale = new Vector3(0.24f, 0.04f, 0.24f);
            leftWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            SetObjectColor(leftWheel, new Color(0.25f, 0.22f, 0.18f)); // Dark Wood
            RemoveCollider(leftWheel);

            GameObject rightWheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightWheel.name = "Cart_Wheel_R";
            rightWheel.transform.SetParent(cartRoot.transform, false);
            rightWheel.transform.localPosition = new Vector3(0.40f, 0.12f, 0f);
            rightWheel.transform.localScale = new Vector3(0.24f, 0.04f, 0.24f);
            rightWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            SetObjectColor(rightWheel, new Color(0.25f, 0.22f, 0.18f));
            RemoveCollider(rightWheel);

            // 3. Handles connecting to worker hands at waist height
            GameObject handleL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handleL.name = "Handle_L";
            handleL.transform.SetParent(cartRoot.transform, false);
            handleL.transform.localPosition = new Vector3(-0.28f, 0.50f, -0.36f);
            handleL.transform.localScale = new Vector3(0.04f, 0.38f, 0.04f);
            handleL.transform.localRotation = Quaternion.Euler(40f, 0f, 0f);
            SetObjectColor(handleL, new Color(0.45f, 0.30f, 0.15f));
            RemoveCollider(handleL);

            GameObject handleR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handleR.name = "Handle_R";
            handleR.transform.SetParent(cartRoot.transform, false);
            handleR.transform.localPosition = new Vector3(0.28f, 0.50f, -0.36f);
            handleR.transform.localScale = new Vector3(0.04f, 0.38f, 0.04f);
            handleR.transform.localRotation = Quaternion.Euler(40f, 0f, 0f);
            SetObjectColor(handleR, new Color(0.45f, 0.30f, 0.15f));
            RemoveCollider(handleR);

            // 4. Cart Inventory & Visualizer for Stacking Items in Cart
            var cartInv = cartRoot.AddComponent<Inventory.Inventory>();
            cartInv.SetMaxCapacity(3);

            var cartVis = cartRoot.AddComponent<WorldItemStackVisualizer>();
            cartVis.ConfigureLayout(new Vector3(0f, 0.38f, 0f), 2, 1, 0.26f, 0.26f, new Vector3(0.55f, 0.55f, 0.55f));

            // 5. Worker Component Setup
            var workerScript = workerObj.AddComponent<LogisticsTransporterWorker>();
            workerScript.Setup(
                cartInv,
                cartVis,
                cartRoot.transform,
                leftWheel.transform,
                rightWheel.transform,
                spawnPos,
                speed: 3.4f,
                capacity: 3
            );

            activeWorkers.Add(workerScript);
            IdleFishing.Core.GameEvents.TriggerWorkerHired("transporter", activeWorkers.Count);
            return workerScript;
        }

        private void UpdateHirePadVisuals()
        {
            if (hirePad != null)
            {
                if (activeWorkers.Count >= maxTransporters)
                {
                    hirePad.SetTitle("[V] FLEET FULL");
                    hirePad.SetSubtitle($"{activeWorkers.Count}/{maxTransporters} Transporters");
                }
                else
                {
                    hirePad.SetTitle("[+] HIRE TRANSPORTER");
                    hirePad.SetSubtitle($"Current: {activeWorkers.Count}/{maxTransporters} Vehicles ({hireCost} G)");
                }
                hirePad.UpdateVisuals();
            }

            if (transporterHirePad != null)
            {
                transporterHirePad.UpdateVisuals();
            }
        }

        private static void SetObjectColor(GameObject obj, Color color)
        {
            var r = obj.GetComponent<Renderer>();
            if (r != null && r.material != null)
            {
                if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", color);
                else if (r.material.HasProperty("_Color")) r.material.color = color;
            }
        }

        private static void RemoveCollider(GameObject obj)
        {
            var col = obj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }
    }
}
