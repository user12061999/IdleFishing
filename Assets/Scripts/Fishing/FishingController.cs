using System;
using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;

namespace IdleFishing.Fishing
{
    public enum FishingState
    {
        Idle,
        Casting,
        WaitingForBite,
        HookedMiniGame,
        ReelingSuccess,
        CargoFull
    }

    public class FishingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FishingSpot currentSpot;
        [SerializeField] private BoatCargo boatCargo;

        [Header("Fishing Tuning")]
        [SerializeField] private float baseBiteDelay = 2.5f;
        [SerializeField] private float reelPowerPerTap = 30f;
        [SerializeField] private float reelDrainSpeed = 15f;
        [SerializeField] private bool autoFishingEnabled = false;

        [Header("State")]
        [SerializeField] private FishingState currentState = FishingState.Idle;

        // Minigame variables
        private FishChainData hookedFish;
        private float reelProgress = 0f; // 0 to 100
        private float tensionValue = 0.5f; // 0 to 1
        private float tensionSpeed = 1.5f;
        private bool tensionDirectionUp = true;
        private float greenZoneMin = 0.35f;
        private float greenZoneMax = 0.65f;

        public event Action<FishingState> OnStateChanged;
        public event Action<float, float, float> OnMinigameTick; // tensionValue, greenZoneMin, greenZoneMax
        public event Action<float> OnReelProgressChanged; // reelProgress (0..100)
        public event Action<FishChainData> OnFishHooked;
        public event Action<FishChainData> OnCatchSuccess;

        public FishingState State => currentState;
        public FishChainData HookedFish => hookedFish;
        public float ReelProgress => reelProgress;
        public float TensionValue => tensionValue;
        public float GreenZoneMin => greenZoneMin;
        public float GreenZoneMax => greenZoneMax;

        private void Update()
        {
            if (currentState == FishingState.HookedMiniGame)
            {
                UpdateMinigame(Time.deltaTime);
            }
        }

        public void SetFishingSpot(FishingSpot spot)
        {
            currentSpot = spot;
        }

        public void StartFishing()
        {
            if (boatCargo != null && boatCargo.IsFull)
            {
                SetState(FishingState.CargoFull);
                return;
            }

            if (currentSpot == null)
            {
                Debug.LogWarning("[FishingController] Cannot fish without a FishingSpot assigned!");
                return;
            }

            StartCoroutine(CastingRoutine());
        }

        public void StopFishing()
        {
            StopAllCoroutines();
            hookedFish = null;
            SetState(FishingState.Idle);
        }

        private IEnumerator CastingRoutine()
        {
            SetState(FishingState.Casting);
            yield return new WaitForSeconds(0.6f);

            SetState(FishingState.WaitingForBite);

            float efficiency = 1f;
            if (UpgradeManager.Instance != null)
            {
                efficiency = UpgradeManager.Instance.GetValue(UpgradeType.FishermanSpeed);
            }
            float waitTime = Mathf.Max(0.5f, baseBiteDelay / efficiency);

            yield return new WaitForSeconds(waitTime);

            if (boatCargo != null && boatCargo.IsFull)
            {
                SetState(FishingState.CargoFull);
                yield break;
            }

            // Hook a fish
            hookedFish = currentSpot.GetRandomFish();
            if (hookedFish != null)
            {
                reelProgress = 35f;
                tensionValue = 0.5f;
                SetState(FishingState.HookedMiniGame);
                OnFishHooked?.Invoke(hookedFish);

                if (autoFishingEnabled)
                {
                    StartCoroutine(AutoReelRoutine());
                }
            }
            else
            {
                SetState(FishingState.Idle);
            }
        }

        private void UpdateMinigame(float dt)
        {
            // Oscillate tension indicator
            float speed = tensionSpeed * (hookedFish != null ? hookedFish.CatchDifficulty : 1f);
            tensionValue += (tensionDirectionUp ? 1f : -1f) * speed * dt;

            if (tensionValue >= 1f)
            {
                tensionValue = 1f;
                tensionDirectionUp = false;
            }
            else if (tensionValue <= 0f)
            {
                tensionValue = 0f;
                tensionDirectionUp = true;
            }

            // Drain reel progress gradually if not active
            reelProgress = Mathf.Max(0f, reelProgress - reelDrainSpeed * dt);
            OnReelProgressChanged?.Invoke(reelProgress);
            OnMinigameTick?.Invoke(tensionValue, greenZoneMin, greenZoneMax);

            if (reelProgress <= 0f)
            {
                // Fish escaped
                hookedFish = null;
                SetState(FishingState.Idle);
                if (autoFishingEnabled)
                {
                    StartFishing();
                }
            }
        }

        public void TapReel()
        {
            if (currentState != FishingState.HookedMiniGame) return;

            // Check if within sweet spot zone
            bool inGreenZone = tensionValue >= greenZoneMin && tensionValue <= greenZoneMax;
            float boost = inGreenZone ? reelPowerPerTap : (reelPowerPerTap * 0.4f);

            reelProgress = Mathf.Min(100f, reelProgress + boost);
            OnReelProgressChanged?.Invoke(reelProgress);

            if (reelProgress >= 100f)
            {
                StartCoroutine(CatchSuccessRoutine());
            }
        }

        private IEnumerator AutoReelRoutine()
        {
            while (currentState == FishingState.HookedMiniGame)
            {
                yield return new WaitForSeconds(0.4f);
                if (currentState == FishingState.HookedMiniGame)
                {
                    TapReel();
                }
            }
        }

        private IEnumerator CatchSuccessRoutine()
        {
            SetState(FishingState.ReelingSuccess);

            if (hookedFish != null && hookedFish.WholeFish != null)
            {
                boatCargo.TryStoreFish(hookedFish.WholeFish);
                GameEvents.TriggerFishCaught(hookedFish.WholeFish);
                OnCatchSuccess?.Invoke(hookedFish);
            }

            yield return new WaitForSeconds(0.8f);

            if (boatCargo != null && boatCargo.IsFull)
            {
                SetState(FishingState.CargoFull);
            }
            else
            {
                SetState(FishingState.Idle);
                if (autoFishingEnabled)
                {
                    StartFishing();
                }
            }
        }

        private void SetState(FishingState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
