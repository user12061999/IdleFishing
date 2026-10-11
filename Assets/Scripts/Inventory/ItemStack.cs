using System;
using UnityEngine;
using IdleFishing.Data;

namespace IdleFishing.Inventory
{
    [Serializable]
    public class ItemStack
    {
        [SerializeField] private ItemData itemData;
        [SerializeField] private int quantity;

        public ItemData Item => itemData;
        public int Quantity => quantity;

        public ItemStack(ItemData item, int initialQty)
        {
            itemData = item;
            quantity = initialQty;
        }

        public void Add(int amount)
        {
            quantity += amount;
        }

        public int Remove(int amount)
        {
            int removed = Mathf.Min(quantity, amount);
            quantity -= removed;
            return removed;
        }

        public bool IsEmpty => quantity <= 0;
    }
}
