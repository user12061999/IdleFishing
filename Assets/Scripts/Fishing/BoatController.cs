using System;
using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Inventory;

namespace IdleFishing.Fishing
{
    public enum BoatLocation
    {
        AtDock,
        SailingToFishingSpot,
        AtFishingSpot,
        SailingToDock
    }

    public class BoatController : MonoBehaviour
    {
        [Header("Waypoints")]
        [SerializeField] private Transform harborDockWaypoint;
        [SerializeField] private Transform fishingSpotWaypoint;

        [Header("References")]
        [SerializeField] private BoatCargo cargo;
        [SerializeField] private FishingController fishingController;
        [SerializeField] private Inventory.Inventory harborStorage;

        [Header("Motion Tuning")]
        [SerializeField] private float baseMoveSpeed = 6.0f;
        [SerializeField] private float rotationSpeed = 5.0f;
        [SerializeField] private float unloadItemInterval = 0.25f;

        [Header("State")]
        [SerializeField] private BoatLocation currentLocation = BoatLocation.AtFishingSpot;

        public event Action<BoatLocation> OnLocationChanged;
        public BoatLocation Location => currentLocation;

        private void Start()
        {
            if (fishingController != null && currentLocation == BoatLocation.AtFishingSpot)
            {
                fishingController.StartFishing();
            }
        }

        private void OnEnable()
        {
            if (fishingController != null)
            {
                fishingController.OnStateChanged += OnFishingStateChanged;
            }
        }

        private void OnDisable()
        {
            if (fishingController != null)
            {
                fishingController.OnStateChanged -= OnFishingStateChanged;
            }
        }

        private void OnFishingStateChanged(FishingState state)
        {
            if (state == FishingState.CargoFull && currentLocation == BoatLocation.AtFishingSpot)
            {
                SailToDock();
            }
        }

        public void SailToDock()
        {
            if (currentLocation == BoatLocation.SailingToDock || currentLocation == BoatLocation.AtDock) return;

            if (fishingController != null)
            {
                fishingController.StopFishing();
            }

            StartCoroutine(SailRoutine(harborDockWaypoint, BoatLocation.SailingToDock, BoatLocation.AtDock, OnArrivedAtDock));
        }

        public void SailToFishingSpot()
        {
            if (currentLocation == BoatLocation.SailingToFishingSpot || currentLocation == BoatLocation.AtFishingSpot) return;

            StartCoroutine(SailRoutine(fishingSpotWaypoint, BoatLocation.SailingToFishingSpot, BoatLocation.AtFishingSpot, OnArrivedAtFishingSpot));
        }

        private IEnumerator SailRoutine(Transform target, BoatLocation sailingState, BoatLocation destinationState, Action onArrived)
        {
            if (target == null)
            {
                SetLocation(destinationState);
                onArrived?.Invoke();
                yield break;
            }

            SetLocation(sailingState);

            float speedMultiplier = 1f;
            float actualSpeed = baseMoveSpeed * speedMultiplier;

            while (Vector3.Distance(transform.position, target.position) > 0.3f)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                direction.y = 0;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                }

                transform.position = Vector3.MoveTowards(transform.position, target.position, actualSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = target.position;
            SetLocation(destinationState);
            onArrived?.Invoke();
        }

        private void OnArrivedAtDock()
        {
            GameEvents.TriggerBoatDocked();
            StartCoroutine(UnloadCargoRoutine());
        }

        private IEnumerator UnloadCargoRoutine()
        {
            if (cargo != null && cargo.Inventory != null && harborStorage != null)
            {
                while (cargo.Inventory.TotalItemCount > 0 && !harborStorage.IsFull)
                {
                    if (cargo.Inventory.TryPopFirst(out var item))
                    {
                        harborStorage.TryAdd(item, 1, out _);
                    }
                    yield return new WaitForSeconds(unloadItemInterval);
                }
            }

            yield return new WaitForSeconds(0.5f);

            // Once unloaded, automatically sail back to continue fishing loop (or wait for player trigger)
            SailToFishingSpot();
        }

        private void OnArrivedAtFishingSpot()
        {
            GameEvents.TriggerBoatDeparted();
            if (fishingController != null)
            {
                fishingController.StartFishing();
            }
        }

        private void SetLocation(BoatLocation newLoc)
        {
            currentLocation = newLoc;
            OnLocationChanged?.Invoke(newLoc);
        }
    }
}
