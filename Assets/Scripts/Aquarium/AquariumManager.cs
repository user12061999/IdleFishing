using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Inventory;

namespace IdleFishing.Aquarium
{
    [Serializable]
    public class AquariumFishInstance
    {
        public string instanceId;
        public string speciesId;
        public string speciesName;
        public int tier;
        public float weightKg;
        public bool isBoss;
        public float baseIncomeRate; // Vàng / giây cơ bản
    }

    [Serializable]
    public class AquariumSaveData
    {
        public int maxCapacity = 6;
        public int decorLevel = 1;
        public float accumulatedRevenue = 0f;
        public List<AquariumFishInstance> fishList = new List<AquariumFishInstance>();
        public bool autoTransferNew = true;
        public bool autoTransferBoss = true;
    }

    public class AquariumManager : MonoBehaviour
    {
        public static AquariumManager Instance { get; private set; }

        [Header("Tuning")]
        [SerializeField] private int baseCapacity = 6;
        [SerializeField] private int maxCapacity = 6;
        [SerializeField] private int tankDecorLevel = 1; // 1 to 4
        [SerializeField] private float accumulatedRevenue = 0f;

        [Header("Automation / Dilemma Policies")]
        [SerializeField] private bool autoTransferNewSpecies = true;
        [SerializeField] private bool autoTransferBoss = true;

        [Header("Storage Linking")]
        [SerializeField] private Inventory.Inventory intakeStorage;

        private readonly List<AquariumFishInstance> captiveFish = new List<AquariumFishInstance>();

        public IReadOnlyList<AquariumFishInstance> CaptiveFish => captiveFish;
        public int CurrentFishCount => captiveFish.Count;
        public int MaxCapacity => maxCapacity;
        public int TankDecorLevel => tankDecorLevel;
        public float AccumulatedRevenue => accumulatedRevenue;
        public bool AutoTransferNewSpecies { get => autoTransferNewSpecies; set => autoTransferNewSpecies = value; }
        public bool AutoTransferBoss { get => autoTransferBoss; set => autoTransferBoss = value; }

        public event Action OnAquariumUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadAquariumState();
        }

        private void Start()
        {
            if (intakeStorage == null)
            {
                var intakeObj = GameObject.Find("Harbor_IntakeStorage");
                if (intakeObj != null)
                {
                    intakeStorage = intakeObj.GetComponent<Inventory.Inventory>();
                }
            }
            StartCoroutine(RevenueTickLoop());
        }

        private IEnumerator RevenueTickLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);
                float rate = GetCurrentIncomeRatePerSecond();
                if (rate > 0f)
                {
                    accumulatedRevenue += rate;
                    OnAquariumUpdated?.Invoke();
                }
            }
        }

        public float GetDecorMultiplier()
        {
            switch (tankDecorLevel)
            {
                case 1: return 1.0f; // Bể kính căn bản
                case 2: return 1.35f; // Đèn LED San hô (+35%)
                case 3: return 1.80f; // Thủy cung Nhiệt đới (+80%)
                case 4: return 2.50f; // Lâu đài Ngọc trai (+150%)
                default: return 1.0f + (tankDecorLevel - 1) * 0.45f;
            }
        }

        public string GetDecorName(int level)
        {
            switch (level)
            {
                case 1: return "Standard Glass Tank";
                case 2: return "Coral LED Lighting (+35% Income)";
                case 3: return "Tropical Coral Exhibit (+80% Income)";
                case 4: return "Pearl Palace (+150% Income)";
                default: return $"Aquarium Lv. {level}";
            }
        }

        public float GetCurrentIncomeRatePerSecond()
        {
            if (captiveFish.Count == 0) return 0f;

            float totalBase = 0f;
            for (int i = 0; i < captiveFish.Count; i++)
            {
                totalBase += captiveFish[i].baseIncomeRate;
            }

            float decorMult = GetDecorMultiplier();
            float bestiaryMult = BestiaryManager.Instance != null ? BestiaryManager.Instance.GetAquariumTicketSynergyMultiplier() : 1.0f;
            float prestigeMult = IdleFishing.Economy.PrestigeManager.Instance != null ? IdleFishing.Economy.PrestigeManager.Instance.AquariumBonusMultiplier : 1.0f;

            return totalBase * decorMult * bestiaryMult * prestigeMult;
        }

        public bool ShouldAutoTransferToAquarium(FishChainData species, bool isBoss)
        {
            if (species == null) return false;
            if (captiveFish.Count >= maxCapacity) return false;

            if (isBoss && autoTransferBoss) return true;

            if (autoTransferNewSpecies)
            {
                // Nếu loài này chưa từng có trong Thủy Cung -> ưu tiên nuôi!
                bool alreadyInTank = false;
                for (int i = 0; i < captiveFish.Count; i++)
                {
                    if (captiveFish[i].speciesId == species.SpeciesId)
                    {
                        alreadyInTank = true;
                        break;
                    }
                }
                if (!alreadyInTank) return true;
            }

            return false;
        }

        public bool TryAddFish(FishChainData species, float weightKg, bool isBoss, out string reason)
        {
            if (species == null)
            {
                reason = "Species data not found.";
                return false;
            }

            if (captiveFish.Count >= maxCapacity)
            {
                reason = "Aquarium tank is full! Upgrade tank capacity.";
                return false;
            }

            // Tính toán doanh thu vé của con cá dựa trên Tier và Weight
            float baseRate = (species.Tier * 1.5f) + (weightKg * 0.35f);
            if (isBoss) baseRate += 12.0f; // Boss tạo sức hút cực lớn cho khách du lịch

            var instance = new AquariumFishInstance
            {
                instanceId = Guid.NewGuid().ToString().Substring(0, 8),
                speciesId = species.SpeciesId,
                speciesName = species.SpeciesName,
                tier = species.Tier,
                weightKg = weightKg,
                isBoss = isBoss,
                baseIncomeRate = Mathf.Max(0.5f, baseRate)
            };

            captiveFish.Add(instance);

            // Register with Bestiary
            if (BestiaryManager.Instance != null)
            {
                BestiaryManager.Instance.MarkLivedInAquarium(species);
                BestiaryManager.Instance.RegisterCatch(species, weightKg);
            }

            GameEvents.TriggerAquariumFishAdded(species, isBoss);
            SaveAquariumState();
            OnAquariumUpdated?.Invoke();

            reason = "Fish successfully placed into Aquarium!";
            Debug.Log($"[AquariumManager] Added {species.SpeciesName} ({weightKg:0.0}kg) to Aquarium! (+{instance.baseIncomeRate:0.0} G/s)");
            return true;
        }

        public bool TryRemoveFishToKitchen(string instanceId)
        {
            int index = captiveFish.FindIndex(f => f.instanceId == instanceId);
            if (index < 0) return false;

            var fish = captiveFish[index];
            captiveFish.RemoveAt(index);

            // Tìm ItemData cá nguyên con 3D để chuyển ra kho bến cảng / chợ
            if (intakeStorage != null)
            {
                ItemData wholeFishItem = null;
                if (BestiaryManager.Instance != null)
                {
                    var entry = BestiaryManager.Instance.GetEntry(fish.speciesId);
                    if (entry != null && entry.chainData != null)
                    {
                        wholeFishItem = entry.chainData.WholeFish;
                    }
                }

                if (wholeFishItem != null)
                {
                    intakeStorage.TryAdd(wholeFishItem, 1, out _);
                }
            }

            SaveAquariumState();
            OnAquariumUpdated?.Invoke();
            Debug.Log($"[AquariumManager] Transferred {fish.speciesName} from Aquarium to Harbor Market!");
            return true;
        }

        public int CollectTicketRevenue()
        {
            int earned = Mathf.FloorToInt(accumulatedRevenue);
            if (earned <= 0) return 0;

            accumulatedRevenue -= earned;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(earned);
            }

            GameEvents.TriggerAquariumTicketClaimed(earned);
            SaveAquariumState();
            OnAquariumUpdated?.Invoke();

            return earned;
        }

        public void AddOfflineTicketRevenue(float seconds)
        {
            float rate = GetCurrentIncomeRatePerSecond();
            if (rate > 0f)
            {
                float offlineEarned = seconds * rate;
                accumulatedRevenue += offlineEarned;
                SaveAquariumState();
                OnAquariumUpdated?.Invoke();
                Debug.Log($"[AquariumManager] Aquarium accumulated +{offlineEarned:0} G in ticket revenue while away!");
            }
        }

        // ==================== UPGRADES ====================
        public int GetCapacityUpgradeCost()
        {
            int level = (maxCapacity - baseCapacity) / 2;
            return Mathf.RoundToInt(150f * Mathf.Pow(1.9f, level));
        }

        public bool UpgradeTankCapacity()
        {
            int cost = GetCapacityUpgradeCost();
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendCoins(cost))
            {
                return false;
            }

            maxCapacity += 2;
            GameEvents.TriggerAquariumUpgraded(maxCapacity, tankDecorLevel);
            SaveAquariumState();
            OnAquariumUpdated?.Invoke();
            return true;
        }

        public int GetDecorUpgradeCost()
        {
            return Mathf.RoundToInt(300f * Mathf.Pow(2.4f, tankDecorLevel - 1));
        }

        public bool UpgradeDecoration()
        {
            if (tankDecorLevel >= 4) return false;

            int cost = GetDecorUpgradeCost();
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendCoins(cost))
            {
                return false;
            }

            tankDecorLevel++;
            GameEvents.TriggerAquariumUpgraded(maxCapacity, tankDecorLevel);
            SaveAquariumState();
            OnAquariumUpdated?.Invoke();
            return true;
        }

        // ==================== SAVE / LOAD ====================
        private void SaveAquariumState()
        {
            var data = new AquariumSaveData
            {
                maxCapacity = maxCapacity,
                decorLevel = tankDecorLevel,
                accumulatedRevenue = accumulatedRevenue,
                fishList = new List<AquariumFishInstance>(captiveFish),
                autoTransferNew = autoTransferNewSpecies,
                autoTransferBoss = autoTransferBoss
            };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("Aquarium_SaveData", json);
            PlayerPrefs.Save();
        }

        private void LoadAquariumState()
        {
            string json = PlayerPrefs.GetString("Aquarium_SaveData", string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<AquariumSaveData>(json);
                    if (data != null)
                    {
                        maxCapacity = Mathf.Max(baseCapacity, data.maxCapacity);
                        tankDecorLevel = Mathf.Clamp(data.decorLevel, 1, 4);
                        accumulatedRevenue = data.accumulatedRevenue;
                        autoTransferNewSpecies = data.autoTransferNew;
                        autoTransferBoss = data.autoTransferBoss;
                        captiveFish.Clear();
                        if (data.fishList != null)
                        {
                            captiveFish.AddRange(data.fishList);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AquariumManager] Failed to load save: {e.Message}");
                }
            }
        }
    }
}
