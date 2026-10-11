using UnityEngine;
using IdleFishing.Data;
using IdleFishing.Inventory;

namespace IdleFishing.Market
{
    [RequireComponent(typeof(Inventory.Inventory))]
    public class DisplayShelf : MonoBehaviour
    {
        [Header("Shelf Settings")]
        [SerializeField] private string shelfName = "Seafood Display Shelf";
        [SerializeField] private Transform customerStandPoint;

        private Inventory.Inventory inventory;

        public string ShelfName => shelfName;
        public Inventory.Inventory Inventory => inventory;
        public Transform StandPoint => customerStandPoint != null ? customerStandPoint : transform;
        public bool HasGoods => inventory != null && inventory.TotalItemCount > 0;

        private void Awake()
        {
            inventory = GetComponent<Inventory.Inventory>();
        }

        private readonly System.Collections.Generic.List<CustomerNPC> queuedCustomers = new System.Collections.Generic.List<CustomerNPC>();

        public Vector3 RegisterAndGetStandPosition(CustomerNPC customer)
        {
            queuedCustomers.RemoveAll(c => c == null);
            if (!queuedCustomers.Contains(customer))
            {
                queuedCustomers.Add(customer);
            }
            int index = queuedCustomers.IndexOf(customer);
            float spacing = 0.95f;
            float offsetX = ((index % 3) - 1f) * spacing;
            float offsetZ = (index / 3) * 0.9f;
            Vector3 basePoint = customerStandPoint != null ? customerStandPoint.position : (transform.position + new Vector3(0, 0, -1.8f));
            return basePoint + new Vector3(offsetX, 0, -offsetZ);
        }

        public void UnregisterCustomer(CustomerNPC customer)
        {
            queuedCustomers.Remove(customer);
        }

        public bool TryBuyOne(out ItemData boughtItem)
        {
            boughtItem = null;
            if (inventory == null || inventory.TotalItemCount == 0) return false;

            return inventory.TryPopFirst(out boughtItem);
        }
    }
}
