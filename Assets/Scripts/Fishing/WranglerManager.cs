using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Inventory;
using IdleFishing.UI;
using IdleFishing.World;

namespace IdleFishing.Fishing
{
    public class WranglerManager : MonoBehaviour
    {
        public static WranglerManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Inventory.Inventory intakeStorage;
        [SerializeField] private Vector3 restAreaCenter = new Vector3(3.0f, 0.5f, 5.5f);
        [SerializeField] private GameObject wranglerModelPrefab;

        [Header("Hire Station Pad")]
        [SerializeField] private GameObject hirePadObj;
        [SerializeField] private InWorldPurchasePad hirePad;

        [Header("Spawned Workers")]
        [SerializeField] private List<FishWranglerWorker> wranglers = new List<FishWranglerWorker>();

        public int ActiveWranglerCount => wranglers.Count;
        public IReadOnlyList<FishWranglerWorker> Wranglers => wranglers;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            RefreshWranglers(instant: true);
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
            if (type == UpgradeType.HireWrangler)
            {
                RefreshWranglers(instant: false);
            }
        }

        public void Setup(Inventory.Inventory storage, Vector3 restCenter, GameObject padObj, InWorldPurchasePad padScript, GameObject modelPrefab = null)
        {
            intakeStorage = storage;
            restAreaCenter = restCenter;
            hirePadObj = padObj;
            hirePad = padScript;
            if (modelPrefab != null) wranglerModelPrefab = modelPrefab;
        }

        public void RegisterWrangler(FishWranglerWorker wrangler)
        {
            if (wrangler != null && !wranglers.Contains(wrangler))
            {
                wranglers.Add(wrangler);
            }
        }

        public void RefreshWranglers(bool instant = false)
        {
            int targetCount = 1;
            if (UpgradeManager.Instance != null)
            {
                targetCount = Mathf.Clamp(UpgradeManager.Instance.GetLevel(UpgradeType.HireWrangler) + 1, 1, 4);
            }

            // Spawn new wranglers dynamically if needed
            while (wranglers.Count < targetCount)
            {
                int index = wranglers.Count;
                FishWranglerWorker newWorker = SpawnWrangler(index);
                wranglers.Add(newWorker);

                if (!instant)
                {
                    StartCoroutine(PopInRoutine(newWorker.transform));
                    if (FloatingDamageManager.Instance != null)
                    {
                        FloatingDamageManager.Instance.SpawnDamage(
                            newWorker.transform.position + Vector3.up * 2f,
                            0,
                            DamageStyle.GoldBonus,
                            $"[!] HIRED FISH WRANGLER #{index + 1}! [!]"
                        );
                    }
                }
            }

            // Set active states
            for (int i = 0; i < wranglers.Count; i++)
            {
                if (wranglers[i] != null)
                {
                    wranglers[i].gameObject.SetActive(i < targetCount);
                }
            }

            // Update in-world hire station pad
            if (hirePadObj != null)
            {
                hirePadObj.SetActive(true);
                if (hirePad != null)
                {
                    if (targetCount >= 4)
                    {
                        hirePad.SetTitle("[V] MAX WRANGLERS REACHED");
                        hirePad.SetSubtitle("4/4 Wranglers active");
                    }
                    else if (targetCount == 0)
                    {
                        hirePad.SetTitle("[+] HIRE FISH WRANGLER");
                        hirePad.SetSubtitle("Auto catch & collect fish!");
                    }
                    else
                    {
                        hirePad.SetTitle("[+] HIRE MORE WRANGLERS");
                        hirePad.SetSubtitle($"Current: {targetCount}/4 Wranglers");
                    }
                    hirePad.UpdateVisuals();
                }
            }
        }

        private FishWranglerWorker SpawnWrangler(int index)
        {
            int col = index % 4;
            int row = index / 4;
            Vector3 restPos = restAreaCenter + new Vector3((col - 1.5f) * 1.1f, 0, row * 1.1f);

            GameObject wranglerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            wranglerObj.name = $"Worker_FishWrangler_{index + 1}";
            wranglerObj.transform.SetParent(transform);
            wranglerObj.transform.position = new Vector3(restPos.x, 0.60f, restPos.z);
            wranglerObj.transform.localScale = new Vector3(0.8f, 1.0f, 0.8f);
            var wrangCol = wranglerObj.GetComponent<CapsuleCollider>();
            if (wrangCol != null) wrangCol.isTrigger = true;

            // Visual Model
            GameObject modelPrefab = wranglerModelPrefab;
#if UNITY_EDITOR
            if (modelPrefab == null)
            {
                modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Stickman_Wrangler.prefab");
            }
#endif
            GameObject weaponObj = null;
            GameObject carryPoint = null;

            if (modelPrefab != null)
            {
                var vis = Instantiate(modelPrefab, wranglerObj.transform);
                vis.name = "Model_Wrangler";
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localRotation = Quaternion.identity;
                vis.transform.localScale = Vector3.one;
                if (wranglerObj.TryGetComponent<MeshRenderer>(out var mr)) mr.enabled = false;

                // Find bat & carry point inside stickman prefab if present
                var foundBat = vis.transform.Find("Model_Stickman/RightHand/Socket_RightHand/Wrangler_Bat") ??
                               vis.transform.Find("Wrangler_Bat");
                if (foundBat == null)
                {
                    foreach (var t in vis.GetComponentsInChildren<Transform>())
                    {
                        if (t.name == "Wrangler_Bat") { foundBat = t; break; }
                    }
                }
                if (foundBat != null) weaponObj = foundBat.gameObject;

                var foundCarry = vis.transform.Find("CarryPoint_Back");
                if (foundCarry == null)
                {
                    foreach (var t in vis.GetComponentsInChildren<Transform>())
                    {
                        if (t.name == "CarryPoint_Back") { foundCarry = t; break; }
                    }
                }
                if (foundCarry != null) carryPoint = foundCarry.gameObject;
            }

            // Fallback Weapon if not in prefab
            if (weaponObj == null)
            {
                weaponObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                weaponObj.name = "Wrangler_Bat";
                weaponObj.transform.SetParent(wranglerObj.transform);
                weaponObj.transform.localPosition = new Vector3(0.5f, 0.1f, 0.5f);
                weaponObj.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
                weaponObj.transform.localScale = new Vector3(0.12f, 0.6f, 0.12f);
            }

            // Fallback Carry Point on back if not in prefab
            if (carryPoint == null)
            {
                carryPoint = new GameObject("CarryPoint_Back");
                carryPoint.transform.SetParent(wranglerObj.transform);
                carryPoint.transform.localPosition = new Vector3(0f, 0.4f, -0.5f);
            }

            // Rest Waypoint
            GameObject restPointObj = new GameObject($"Wrangler_RestPoint_{index + 1}");
            restPointObj.transform.SetParent(transform);
            restPointObj.transform.position = restPos;

            var worker = wranglerObj.AddComponent<FishWranglerWorker>();
            worker.SetupRuntime(intakeStorage, carryPoint.transform, restPointObj.transform, weaponObj.transform);

            return worker;
        }

        private IEnumerator PopInRoutine(Transform target)
        {
            Vector3 originalScale = target.localScale;
            target.localScale = Vector3.zero;

            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float overshoot = Mathf.Sin(t * Mathf.PI * 0.75f) * 1.25f;
                target.localScale = originalScale * overshoot;
                yield return null;
            }

            if (target != null) target.localScale = originalScale;
        }
    }
}
