using System;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;

namespace IdleFishing.Aquarium
{
    [Serializable]
    public class BestiaryEntry
    {
        public string speciesId;
        public string speciesName;
        public int tier;
        public bool isDiscovered;
        public int totalCaught;
        public float recordWeightKg;
        public bool hasLivedInAquarium;
        public FishChainData chainData;
    }

    public class BestiaryManager : MonoBehaviour
    {
        public static BestiaryManager Instance { get; private set; }

        [Header("Configured Species")]
        [SerializeField] private List<FishChainData> allSpeciesConfig = new List<FishChainData>();

        private readonly Dictionary<string, BestiaryEntry> entries = new Dictionary<string, BestiaryEntry>();

        public IReadOnlyCollection<BestiaryEntry> Entries => entries.Values;
        public int TotalSpeciesCount => entries.Count;
        public int DiscoveredSpeciesCount
        {
            get
            {
                int count = 0;
                foreach (var e in entries.Values)
                {
                    if (e.isDiscovered) count++;
                }
                return count;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadAllSpeciesData();
            LoadProgress();
        }

        private void LoadAllSpeciesData()
        {
            if (allSpeciesConfig == null || allSpeciesConfig.Count == 0)
            {
                allSpeciesConfig = new List<FishChainData>(Resources.LoadAll<FishChainData>(""));
            }

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:FishChainData");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<FishChainData>(path);
                if (asset != null && !allSpeciesConfig.Contains(asset))
                {
                    allSpeciesConfig.Add(asset);
                }
            }
#endif

            foreach (var chain in allSpeciesConfig)
            {
                if (chain == null || string.IsNullOrEmpty(chain.SpeciesId)) continue;
                if (!entries.ContainsKey(chain.SpeciesId))
                {
                    entries[chain.SpeciesId] = new BestiaryEntry
                    {
                        speciesId = chain.SpeciesId,
                        speciesName = chain.SpeciesName,
                        tier = chain.Tier,
                        isDiscovered = false,
                        totalCaught = 0,
                        recordWeightKg = 0f,
                        hasLivedInAquarium = false,
                        chainData = chain
                    };
                }
            }
        }

        public void RegisterCatch(FishChainData chain, float weightKg)
        {
            if (chain == null || string.IsNullOrEmpty(chain.SpeciesId)) return;

            if (!entries.TryGetValue(chain.SpeciesId, out var entry))
            {
                entry = new BestiaryEntry
                {
                    speciesId = chain.SpeciesId,
                    speciesName = chain.SpeciesName,
                    tier = chain.Tier,
                    isDiscovered = false,
                    totalCaught = 0,
                    recordWeightKg = 0f,
                    hasLivedInAquarium = false,
                    chainData = chain
                };
                entries[chain.SpeciesId] = entry;
            }

            bool wasFirstDiscovery = !entry.isDiscovered;
            entry.isDiscovered = true;
            entry.totalCaught++;
            if (weightKg > entry.recordWeightKg)
            {
                entry.recordWeightKg = weightKg;
            }

            SaveProgress();

            if (wasFirstDiscovery)
            {
                GameEvents.TriggerBestiarySpeciesDiscovered(chain);
                Debug.Log($"[Bestiary] DISCOVERED NEW SPECIES: {chain.SpeciesName}! (Total Discovered: {DiscoveredSpeciesCount}/{TotalSpeciesCount})");
            }
        }

        public void MarkLivedInAquarium(FishChainData chain)
        {
            if (chain == null) return;
            if (entries.TryGetValue(chain.SpeciesId, out var entry))
            {
                entry.hasLivedInAquarium = true;
                SaveProgress();
            }
        }

        public bool IsSpeciesDiscovered(string speciesId)
        {
            return entries.TryGetValue(speciesId, out var entry) && entry.isDiscovered;
        }

        public BestiaryEntry GetEntry(string speciesId)
        {
            entries.TryGetValue(speciesId, out var entry);
            return entry;
        }

        // ==================== MILESTONE SYNERGIES ====================
        public float GetReelSpeedMultiplier()
        {
            // Milestone 1 (>= 3 loài): +15% tốc độ cần thủ
            return DiscoveredSpeciesCount >= 3 ? 1.15f : 1.0f;
        }

        public float GetGlobalMarketPriceMultiplier()
        {
            // Milestone 2 (>= 5 loài): +20% giá bán khay cá
            return DiscoveredSpeciesCount >= 5 ? 1.20f : 1.0f;
        }

        public float GetWranglerAttackMultiplier()
        {
            // Milestone 3 (>= 7 loài): +30% sát thương thợ bắt cá
            return DiscoveredSpeciesCount >= 7 ? 1.30f : 1.0f;
        }

        public float GetAquariumTicketSynergyMultiplier()
        {
            // Milestone 3 (>= 7 loài): +50% tiền vé thủy cung
            return DiscoveredSpeciesCount >= 7 ? 1.50f : 1.0f;
        }

        // ==================== SAVE / LOAD ====================
        private void SaveProgress()
        {
            foreach (var kvp in entries)
            {
                var e = kvp.Value;
                PlayerPrefs.SetInt($"Bestiary_{e.speciesId}_Discovered", e.isDiscovered ? 1 : 0);
                PlayerPrefs.SetInt($"Bestiary_{e.speciesId}_Caught", e.totalCaught);
                PlayerPrefs.SetFloat($"Bestiary_{e.speciesId}_Weight", e.recordWeightKg);
                PlayerPrefs.SetInt($"Bestiary_{e.speciesId}_Aquarium", e.hasLivedInAquarium ? 1 : 0);
            }
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            foreach (var kvp in entries)
            {
                var e = kvp.Value;
                e.isDiscovered = PlayerPrefs.GetInt($"Bestiary_{e.speciesId}_Discovered", 0) == 1;
                e.totalCaught = PlayerPrefs.GetInt($"Bestiary_{e.speciesId}_Caught", 0);
                e.recordWeightKg = PlayerPrefs.GetFloat($"Bestiary_{e.speciesId}_Weight", 0f);
                e.hasLivedInAquarium = PlayerPrefs.GetInt($"Bestiary_{e.speciesId}_Aquarium", 0) == 1;
            }
        }
    }
}
