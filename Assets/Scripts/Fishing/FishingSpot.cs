using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Data;

namespace IdleFishing.Fishing
{
    public class FishingSpot : MonoBehaviour
    {
        [Header("Spot Info")]
        [SerializeField] private string spotName = "Deep Ocean Reef";
        [SerializeField] private List<FishChainData> availableSpecies = new List<FishChainData>();

        public string SpotName => spotName;
        public IReadOnlyList<FishChainData> AvailableSpecies => availableSpecies;

        public void AddSpecies(FishChainData fish)
        {
            if (fish != null && !availableSpecies.Contains(fish))
            {
                availableSpecies.Add(fish);
            }
        }

        public void ClearAndSetSpecies(IEnumerable<FishChainData> newSpecies)
        {
            availableSpecies.Clear();
            if (newSpecies != null)
            {
                foreach (var s in newSpecies)
                {
                    if (s != null && !availableSpecies.Contains(s))
                    {
                        availableSpecies.Add(s);
                    }
                }
            }
        }

        public FishChainData GetRandomFish()
        {
            if (availableSpecies == null || availableSpecies.Count == 0) return null;

            float totalWeight = 0f;
            for (int i = 0; i < availableSpecies.Count; i++)
            {
                if (availableSpecies[i] != null && !availableSpecies[i].IsApexBoss)
                {
                    totalWeight += Mathf.Max(0.1f, availableSpecies[i].SpawnWeight);
                }
            }

            if (totalWeight <= 0f)
            {
                return availableSpecies[0];
            }

            float roll = Random.Range(0f, totalWeight);
            float currentSum = 0f;

            for (int i = 0; i < availableSpecies.Count; i++)
            {
                if (availableSpecies[i] != null && !availableSpecies[i].IsApexBoss)
                {
                    currentSum += Mathf.Max(0.1f, availableSpecies[i].SpawnWeight);
                    if (roll <= currentSum)
                    {
                        return availableSpecies[i];
                    }
                }
            }

            for (int i = 0; i < availableSpecies.Count; i++)
            {
                if (availableSpecies[i] != null && !availableSpecies[i].IsApexBoss)
                    return availableSpecies[i];
            }
            return availableSpecies[0];
        }

        public FishChainData GetRandomBossFish()
        {
            List<FishChainData> bossPool = new List<FishChainData>();
            if (availableSpecies != null)
            {
                for (int i = 0; i < availableSpecies.Count; i++)
                {
                    if (availableSpecies[i] != null && availableSpecies[i].IsApexBoss)
                    {
                        bossPool.Add(availableSpecies[i]);
                    }
                }
            }

            // Fallback: check BestiaryManager for any configured Apex Boss species
            if (bossPool.Count == 0 && IdleFishing.Aquarium.BestiaryManager.Instance != null)
            {
                foreach (var entry in IdleFishing.Aquarium.BestiaryManager.Instance.Entries)
                {
                    if (entry?.chainData != null && entry.chainData.IsApexBoss)
                    {
                        bossPool.Add(entry.chainData);
                    }
                }
            }

#if UNITY_EDITOR
            if (bossPool.Count == 0)
            {
                string[] bossIds = new[] { "reef_shark", "eagle_ray", "thresher_shark", "manta_ray", "great_white_shark" };
                foreach (var id in bossIds)
                {
                    var chain = UnityEditor.AssetDatabase.LoadAssetAtPath<FishChainData>($"Assets/GameData/FishChains/{id}_chain.asset");
                    if (chain != null) bossPool.Add(chain);
                }
            }
#endif

            if (bossPool.Count > 0)
            {
                float totalWeight = 0f;
                for (int i = 0; i < bossPool.Count; i++)
                {
                    totalWeight += Mathf.Max(1f, bossPool[i].SpawnWeight);
                }
                float roll = Random.Range(0f, totalWeight);
                float sum = 0f;
                for (int i = 0; i < bossPool.Count; i++)
                {
                    sum += Mathf.Max(1f, bossPool[i].SpawnWeight);
                    if (roll <= sum) return bossPool[i];
                }
                return bossPool[Random.Range(0, bossPool.Count)];
            }

            return GetRandomFish();
        }
    }
}
