using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Fishing;
using IdleFishing.UI;

namespace IdleFishing.Market
{
    public class ContractManager : MonoBehaviour
    {
        public static ContractManager Instance { get; private set; }

        private const string PREF_FAME = "IdleFishing_HarborFame";
        private const string PREF_LEVEL = "IdleFishing_HarborLevel";
        private const int MAX_ACTIVE_CONTRACTS = 3;

        [Header("Harbor Progression")]
        [SerializeField] private int currentFame = 0;
        [SerializeField] private int harborLevel = 1;

        [Header("Contract Fallbacks")]
        [SerializeField] private List<FishChainData> speciesPool = new List<FishChainData>();

        [Header("Runtime State")]
        [SerializeField] private List<HarborContract> activeContracts = new List<HarborContract>();

        public int CurrentFame => currentFame;
        public int HarborLevel => harborLevel;
        public IReadOnlyList<HarborContract> ActiveContracts => activeContracts;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadProgression();
        }

        private void Start()
        {
            // Initial contract generation
            EnsureActiveContracts();
        }

        private void OnEnable()
        {
            GameEvents.WorldChanged += OnWorldChanged;
        }

        private void OnDisable()
        {
            GameEvents.WorldChanged -= OnWorldChanged;
        }

        private void OnWorldChanged(WorldData newWorld)
        {
            // Refresh contracts for new world
            activeContracts.Clear();
            EnsureActiveContracts();
        }

        private void Update()
        {
            bool anyChanged = false;
            for (int i = activeContracts.Count - 1; i >= 0; i--)
            {
                var c = activeContracts[i];
                if (c == null) continue;

                if (!c.isCompleted && c.durationSeconds > 0)
                {
                    c.timeRemaining -= Time.deltaTime;
                    if (c.timeRemaining <= 0)
                    {
                        // Contract expired, replace with new one
                        activeContracts[i] = GenerateRandomContract();
                        anyChanged = true;
                    }
                }
            }

            if (anyChanged)
            {
                GameEvents.TriggerContractsRefreshed();
            }
        }

        public void SetSpeciesPool(List<FishChainData> pool)
        {
            speciesPool = pool;
            EnsureActiveContracts();
        }

        public void EnsureActiveContracts()
        {
            while (activeContracts.Count < MAX_ACTIVE_CONTRACTS)
            {
                var newContract = GenerateRandomContract();
                if (newContract != null)
                {
                    activeContracts.Add(newContract);
                }
                else
                {
                    break;
                }
            }
            GameEvents.TriggerContractsRefreshed();
        }

        public HarborContract GenerateRandomContract()
        {
            List<FishChainData> candidates = GetAvailableSpecies();
            if (candidates == null || candidates.Count == 0) return null;

            FishChainData species = candidates[Random.Range(0, candidates.Count)];
            if (species == null) return null;

            // Pick a client flavor
            int clientType = Random.Range(0, 4);
            string clientName = "Tokyo Sushi Restaurant";
            string clientIcon = "[SUSHI]";
            string title = "";
            string desc = "";
            ItemData targetItem = null;
            int count = 10;

            targetItem = species.WholeFish;
            if (targetItem == null) return null;

            switch (clientType)
            {
                case 0: // Seafood Auction
                    clientName = "Tokyo Seafood Auction";
                    clientIcon = "[AUCTION]";
                    count = Random.Range(6, 12);
                    title = $"Premium {species.SpeciesName} Order";
                    desc = $"Master buyer needs {count} fresh whole {species.SpeciesName} for the harbor auction.";
                    break;

                case 1: // Fresh Seafood Market
                    clientName = "Fresh Mart Harbor";
                    clientIcon = "[MART]";
                    count = Random.Range(8, 15);
                    title = $"Fresh {species.SpeciesName} Supply";
                    desc = $"Supply {count} whole {species.SpeciesName} on ice for coastal shoppers.";
                    break;

                case 2: // Marine Collector
                    clientName = "Royal Marine Collector";
                    clientIcon = "[VIP]";
                    count = Random.Range(5, 10);
                    title = $"Prize {species.SpeciesName} Collection";
                    desc = $"Collector requests {count} pristine {species.SpeciesName} specimens.";
                    break;

                case 3: // Ocean Export Fleet
                    clientName = "Pacific Ocean Export";
                    clientIcon = "[EXPORT]";
                    count = Random.Range(10, 18);
                    title = $"Fresh {species.SpeciesName} Export Contract";
                    desc = $"Cargo fleet requires {count} whole {species.SpeciesName} for overseas shipping.";
                    break;
            }

            // Economics: 2.5x to 3.2x wholesale payout!
            float wholesaleMultiplier = Random.Range(2.5f, 3.2f);
            int rewardGold = Mathf.Max(50, Mathf.RoundToInt(targetItem.BasePrice * count * wholesaleMultiplier));
            int rewardFame = Mathf.RoundToInt(count * 7);
            float frenzyBonus = 25f; // +25% frenzy charge
            float duration = 180f;   // 3 minutes

            string id = "contract_" + System.Guid.NewGuid().ToString().Substring(0, 8);
            return new HarborContract(id, clientName, clientIcon, title, desc, targetItem, count, rewardGold, rewardFame, frenzyBonus, duration);
        }

        private List<FishChainData> GetAvailableSpecies()
        {
            if (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null &&
                WorldManager.Instance.CurrentWorld.nativeSpecies.Count > 0)
            {
                return WorldManager.Instance.CurrentWorld.nativeSpecies;
            }

            return speciesPool;
        }

        public int GetAvailableItemCount(ItemData targetItem)
        {
            if (targetItem == null) return 0;

            int count = 0;
            var allInventories = FindObjectsByType<Inventory.Inventory>(FindObjectsSortMode.None);
            for (int i = 0; i < allInventories.Length; i++)
            {
                var inv = allInventories[i];
                if (inv == null) continue;

                // Do not count items inside Station input buffers to avoid stalling in-progress work
                if (inv.gameObject.name.Contains("_Input")) continue;

                count += inv.GetItemCount(targetItem);
            }

            return count;
        }

        public bool CanFulfillContract(HarborContract contract)
        {
            if (contract == null || contract.isCompleted || contract.targetItem == null) return false;
            return GetAvailableItemCount(contract.targetItem) >= contract.requiredCount;
        }

        public bool TryFulfillContract(HarborContract contract)
        {
            if (!CanFulfillContract(contract)) return false;

            // 1. Consume items from inventories (Prioritize shelves, station outputs, intake storage)
            int needed = contract.requiredCount;
            var allInventories = FindObjectsByType<Inventory.Inventory>(FindObjectsSortMode.None);

            for (int i = 0; i < allInventories.Length; i++)
            {
                var inv = allInventories[i];
                if (inv == null || inv.gameObject.name.Contains("_Input")) continue;

                int has = inv.GetItemCount(contract.targetItem);
                if (has > 0)
                {
                    int take = Mathf.Min(has, needed);
                    inv.TryRemove(contract.targetItem, take);
                    needed -= take;
                    if (needed <= 0) break;
                }
            }

            // 2. Grant rewards
            int actualGold = contract.rewardGold;
            if (IdleFishing.Economy.PrestigeManager.Instance != null)
            {
                actualGold = Mathf.RoundToInt(actualGold * IdleFishing.Economy.PrestigeManager.Instance.BossAndContractBonusMultiplier);
            }
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(actualGold);
            }

            AddFame(contract.rewardFame);

            if (FishingFrenzyManager.Instance != null)
            {
                FishingFrenzyManager.Instance.AddCharge(contract.frenzyChargeBonus);
            }

            // 3. Mark completed
            contract.isCompleted = true;
            GameEvents.TriggerContractCompleted(contract);

            // 4. Celebratory Visual Juice
            Vector3 effectPos = Vector3.up * 2f;
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                effectPos = player.transform.position + Vector3.up * 1.5f;
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(effectPos, contract.rewardGold, DamageStyle.GoldBonus, $"CONTRACT COMPLETED! +{NumberFormatter.Format(contract.rewardGold)} G +{contract.rewardFame} FAME");
            }
            if (FlyingCoinManager.Instance != null)
            {
                FlyingCoinManager.Instance.PlayFlyingCoins(effectPos, 5);
            }

            // 5. Replace completed contract slot after 1.5 seconds
            StartCoroutine(ReplaceContractSlotRoutine(contract));

            return true;
        }

        private IEnumerator ReplaceContractSlotRoutine(HarborContract completedContract)
        {
            yield return new WaitForSeconds(1.5f);
            int idx = activeContracts.IndexOf(completedContract);
            if (idx >= 0)
            {
                activeContracts[idx] = GenerateRandomContract();
                GameEvents.TriggerContractsRefreshed();
            }
        }

        // ================= PROGRESSION & FAME =================
        public void AddFame(int amount)
        {
            if (amount <= 0) return;
            currentFame += amount;

            int oldLevel = harborLevel;
            CheckLevelUp();

            SaveProgression();
            GameEvents.TriggerHarborFameChanged(currentFame, harborLevel);

            if (harborLevel > oldLevel)
            {
                Debug.Log($"[ContractManager] [PROMOTION] HARBOR PROMOTED TO LEVEL {harborLevel}: {GetLevelTitle(harborLevel)}! Sell price & processing speed permanently increased!");
            }
        }

        private void CheckLevelUp()
        {
            if (currentFame >= 1200) harborLevel = 4;
            else if (currentFame >= 600) harborLevel = 3;
            else if (currentFame >= 200) harborLevel = 2;
            else harborLevel = 1;
        }

        public string GetLevelTitle(int level)
        {
            switch (level)
            {
                case 4: return "Royal Harbor Empire";
                case 3: return "Global Seafood Hub";
                case 2: return "Prosperous Trading Port";
                default: return "Novice Fishing Pier";
            }
        }

        public int GetFameTargetForNextLevel()
        {
            switch (harborLevel)
            {
                case 1: return 200;
                case 2: return 600;
                case 3: return 1200;
                default: return 2000;
            }
        }

        public int GetFameBaseForCurrentLevel()
        {
            switch (harborLevel)
            {
                case 1: return 0;
                case 2: return 200;
                case 3: return 600;
                default: return 1200;
            }
        }

        public float GetGlobalPriceMultiplier()
        {
            switch (harborLevel)
            {
                case 4: return 1.50f; // +50% global price!
                case 3: return 1.30f; // +30% global price!
                case 2: return 1.15f; // +15% global price!
                default: return 1.0f;
            }
        }

        public float GetGlobalStationSpeedMultiplier()
        {
            switch (harborLevel)
            {
                case 4: return 1.40f; // +40% processing speed!
                case 3: return 1.25f; // +25% processing speed!
                case 2: return 1.15f; // +15% processing speed!
                default: return 1.0f;
            }
        }

        private void LoadProgression()
        {
            currentFame = PlayerPrefs.GetInt(PREF_FAME, 0);
            harborLevel = PlayerPrefs.GetInt(PREF_LEVEL, 1);
            CheckLevelUp();
        }

        public void ResetAllContracts()
        {
            currentFame = 0;
            harborLevel = 1;
            PlayerPrefs.DeleteKey(PREF_FAME);
            PlayerPrefs.DeleteKey(PREF_LEVEL);
            PlayerPrefs.Save();

            activeContracts.Clear();
            EnsureActiveContracts();
            Debug.Log("[ContractManager] VOYAGE RESET: Contracts and Harbor Fame reset.");
        }

        private void SaveProgression()
        {
            PlayerPrefs.SetInt(PREF_FAME, currentFame);
            PlayerPrefs.SetInt(PREF_LEVEL, harborLevel);
            PlayerPrefs.Save();
        }
    }
}
