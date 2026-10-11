using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Inventory;

namespace IdleFishing.Fishing
{
    [RequireComponent(typeof(Inventory.Inventory))]
    public class BoatCargo : MonoBehaviour
    {
        [Header("Base Capacity")]
        [SerializeField] private int baseCargoCapacity = 8;

        private Inventory.Inventory cargoInventory;

        public Inventory.Inventory Inventory => cargoInventory;
        public int CurrentCount => cargoInventory != null ? cargoInventory.TotalItemCount : 0;
        public int MaxCapacity => cargoInventory != null ? cargoInventory.MaxCapacity : baseCargoCapacity;
        public bool IsFull => cargoInventory != null && cargoInventory.IsFull;

        private void Awake()
        {
            cargoInventory = GetComponent<Inventory.Inventory>();
        }

        private void Start()
        {
            UpdateCapacityFromUpgrades();
        }

        private void OnEnable()
        {
            if (cargoInventory != null)
            {
                cargoInventory.OnInventoryChanged += OnInventoryUpdated;
            }
            GameEvents.UpgradePurchased += OnUpgradePurchased;
        }

        private void OnDisable()
        {
            if (cargoInventory != null)
            {
                cargoInventory.OnInventoryChanged -= OnInventoryUpdated;
            }
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
        }

        private void OnInventoryUpdated(Inventory.Inventory inv)
        {
            GameEvents.TriggerCargoUpdated(inv.TotalItemCount, inv.MaxCapacity);
        }

        private void OnUpgradePurchased(UpgradeType type, int level)
        {
        }

        public void UpdateCapacityFromUpgrades()
        {
            cargoInventory.SetMaxCapacity(baseCargoCapacity);
        }

        public bool TryStoreFish(ItemData fishItem)
        {
            if (cargoInventory.TryAdd(fishItem, 1, out _))
            {
                return true;
            }
            return false;
        }
    }
}
