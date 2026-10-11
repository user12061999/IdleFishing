using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Inventory;
using IdleFishing.UI;

namespace IdleFishing.Fishing
{
    public class FishingPierManager : MonoBehaviour
    {
        public static FishingPierManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Inventory.Inventory intakeStorage;
        [SerializeField] private Transform landingZoneCenter;
        [SerializeField] private float landingZoneRadius = 2.0f;

        [Header("Slots Setup")]
        [SerializeField] private List<FishingSlot> slots = new List<FishingSlot>();

        [Header("Tuning")]
        [SerializeField] private int basePendingFishThreshold = 3;

        private readonly List<LiveFish> allLivingFish = new List<LiveFish>();
        private readonly List<LiveFish> allDefeatedFish = new List<LiveFish>();

        public Inventory.Inventory IntakeStorage => intakeStorage;
        public Transform LandingZoneCenter => landingZoneCenter;
        public float LandingZoneRadius => landingZoneRadius;
        public int TotalLivingFishCount => allLivingFish.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterSlot(FishingSlot slot)
        {
            if (!slots.Contains(slot))
            {
                slots.Add(slot);
            }
        }

        public int GetActiveSlotsCount()
        {
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && slots[i].IsUnlocked) count++;
            }
            return Mathf.Max(1, count);
        }

        public bool IsPierCrowded()
        {
            int maxAllowed = basePendingFishThreshold + (GetActiveSlotsCount() - 1) * 2;
            return allLivingFish.Count >= maxAllowed;
        }

        public void RegisterLiveFish(LiveFish fish)
        {
            if (fish == null) return;
            if (!allLivingFish.Contains(fish))
            {
                allLivingFish.Add(fish);
            }
        }

        public void UnregisterLiveFish(LiveFish fish)
        {
            allLivingFish.Remove(fish);
            allDefeatedFish.Remove(fish);
        }

        public void OnFishDefeated(LiveFish fish)
        {
            allLivingFish.Remove(fish);
            if (fish != null && !allDefeatedFish.Contains(fish))
            {
                allDefeatedFish.Add(fish);
            }
        }

        public LiveFish GetNearestLivingFish(Vector3 fromPos, FishWranglerWorker requestingWorker = null)
        {
            allLivingFish.RemoveAll(f => f == null || f.IsDefeated || f.IsPickedUp);

            LiveFish best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < allLivingFish.Count; i++)
            {
                LiveFish fish = allLivingFish[i];
                if (fish == null || fish.IsDefeated || fish.IsPickedUp) continue;

                float dist = Vector3.Distance(fromPos, fish.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = fish;
                }
            }
            return best;
        }

        public LiveFish GetNearestDefeatedFish(Vector3 fromPos, FishWranglerWorker requestingWorker = null)
        {
            allDefeatedFish.RemoveAll(f => f == null || f.IsPickedUp);

            LiveFish best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < allDefeatedFish.Count; i++)
            {
                LiveFish fish = allDefeatedFish[i];
                if (fish == null || fish.IsPickedUp) continue;

                float dist = Vector3.Distance(fromPos, fish.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = fish;
                }
            }
            return best;
        }

        public void DeliverFishToStorage(LiveFish fish)
        {
            if (fish == null || fish.IsPickedUp) return;
            allLivingFish.Remove(fish);
            allDefeatedFish.Remove(fish);
            StartCoroutine(DeliverFishRoutine(fish));
        }

        private IEnumerator DeliverFishRoutine(LiveFish fish)
        {
            if (fish == null) yield break;
            fish.SetManualDeliveryInProgress();

            Vector3 startPos = fish.transform.position;
            Vector3 targetPos = intakeStorage != null ? intakeStorage.transform.position + Vector3.up * 0.8f : startPos + Vector3.forward * 3f;

            float duration = 0.55f;
            float elapsed = 0f;

            while (elapsed < duration && fish != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 current = Vector3.Lerp(startPos, targetPos, t);
                float arc = Mathf.Sin(t * Mathf.PI) * 2.2f;
                current.y += arc;

                fish.transform.position = current;
                fish.transform.Rotate(Vector3.up, 360f * Time.deltaTime * 2f);
                yield return null;
            }

            if (fish != null)
            {
                bool sentToAquarium = false;
                if (IdleFishing.Aquarium.AquariumManager.Instance != null && fish.SpeciesData != null)
                {
                    if (IdleFishing.Aquarium.AquariumManager.Instance.ShouldAutoTransferToAquarium(fish.SpeciesData, fish.IsBossFish))
                    {
                        float weight = UnityEngine.Random.Range(fish.SpeciesData.MinWeightKg, fish.SpeciesData.MaxWeightKg);
                        if (fish.IsBossFish) weight *= 2.2f;

                        if (IdleFishing.Aquarium.AquariumManager.Instance.TryAddFish(fish.SpeciesData, weight, fish.IsBossFish, out _))
                        {
                            sentToAquarium = true;
                            if (FloatingDamageManager.Instance != null)
                            {
                                FloatingDamageManager.Instance.SpawnDamage(
                                    targetPos,
                                    0,
                                    DamageStyle.GoldBonus,
                                    $"[+] AQUARIUM EXHIBIT: {fish.SpeciesData.SpeciesName}"
                                );
                            }
                        }
                    }
                }

                if (!sentToAquarium)
                {
                    if (intakeStorage != null && fish.WholeFishItem != null)
                    {
                        intakeStorage.TryAdd(fish.WholeFishItem, 1, out _);
                    }

                    if (FloatingDamageManager.Instance != null)
                    {
                        FloatingDamageManager.Instance.SpawnDamage(
                            targetPos,
                            1,
                            DamageStyle.GoldBonus,
                            $"+1 {fish.SpeciesData?.SpeciesName ?? "Fish"} To Storage"
                        );
                    }
                }

                if (FlyingCoinManager.Instance != null)
                {
                    FlyingCoinManager.Instance.PlayFlyingCoins(targetPos, 3);
                }

                Destroy(fish.gameObject);
            }
        }
    }
}
