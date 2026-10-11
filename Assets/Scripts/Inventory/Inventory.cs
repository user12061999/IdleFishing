using System;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Data;

namespace IdleFishing.Inventory
{
    public class Inventory : MonoBehaviour
    {
        [Header("Inventory Capacity")]
        [SerializeField] private int maxCapacity = 20;
        [SerializeField] private bool unlimitedCapacity = false;

        [Header("Allowed Item Filter (Optional)")]
        [SerializeField] private bool filterByType = false;
        [SerializeField] private ItemType[] allowedTypes;

        [Header("Runtime Stacks")]
        [SerializeField] private List<ItemStack> stacks = new List<ItemStack>();

        public event Action<Inventory> OnInventoryChanged;

        public int MaxCapacity => maxCapacity;
        public bool IsUnlimited => unlimitedCapacity;
        public IReadOnlyList<ItemStack> Stacks => stacks;

        public int TotalItemCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < stacks.Count; i++)
                {
                    count += stacks[i].Quantity;
                }
                return count;
            }
        }

        public bool IsFull => !unlimitedCapacity && TotalItemCount >= maxCapacity;
        public int RemainingSpace => unlimitedCapacity ? int.MaxValue : Mathf.Max(0, maxCapacity - TotalItemCount);

        public void SetMaxCapacity(int newCapacity)
        {
            maxCapacity = Mathf.Max(1, newCapacity);
            OnInventoryChanged?.Invoke(this);
        }

        public bool IsTypeAllowed(ItemType type)
        {
            if (!filterByType || allowedTypes == null || allowedTypes.Length == 0) return true;
            for (int i = 0; i < allowedTypes.Length; i++)
            {
                if (allowedTypes[i] == type) return true;
            }
            return false;
        }

        public bool CanAccept(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;
            if (!IsTypeAllowed(item.Type)) return false;
            if (unlimitedCapacity) return true;
            return RemainingSpace >= amount;
        }

        public bool TryAdd(ItemData item, int amount, out int actuallyAdded)
        {
            actuallyAdded = 0;
            if (item == null || amount <= 0) return false;
            if (!IsTypeAllowed(item.Type)) return false;

            int toAdd = unlimitedCapacity ? amount : Mathf.Min(amount, RemainingSpace);
            if (toAdd <= 0) return false;

            ItemStack existing = stacks.Find(s => s.Item == item);
            if (existing != null)
            {
                existing.Add(toAdd);
            }
            else
            {
                stacks.Add(new ItemStack(item, toAdd));
            }

            actuallyAdded = toAdd;
            OnInventoryChanged?.Invoke(this);
            return true;
        }

        public bool TryRemove(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;
            ItemStack existing = stacks.Find(s => s.Item == item);
            if (existing == null || existing.Quantity < amount) return false;

            existing.Remove(amount);
            if (existing.IsEmpty)
            {
                stacks.Remove(existing);
            }

            OnInventoryChanged?.Invoke(this);
            return true;
        }

        public bool TryPopFirst(out ItemData item)
        {
            item = null;
            if (stacks.Count == 0) return false;

            ItemStack topStack = stacks[stacks.Count - 1];
            item = topStack.Item;
            topStack.Remove(1);
            if (topStack.IsEmpty)
            {
                stacks.RemoveAt(stacks.Count - 1);
            }

            OnInventoryChanged?.Invoke(this);
            return true;
        }

        public int GetItemCount(ItemData item)
        {
            if (item == null) return 0;
            ItemStack existing = stacks.Find(s => s.Item == item);
            return existing != null ? existing.Quantity : 0;
        }

        public bool HasItemOfType(ItemType type)
        {
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i] != null && stacks[i].Item != null && stacks[i].Item.Type == type && stacks[i].Quantity > 0)
                    return true;
            }
            return false;
        }

        public int GetCountOfType(ItemType type)
        {
            int count = 0;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i] != null && stacks[i].Item != null && stacks[i].Item.Type == type)
                {
                    count += stacks[i].Quantity;
                }
            }
            return count;
        }

        public bool TryGetFirstItemOfType(ItemType type, out ItemData foundItem)
        {
            foundItem = null;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i] != null && stacks[i].Item != null && stacks[i].Item.Type == type && stacks[i].Quantity > 0)
                {
                    foundItem = stacks[i].Item;
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            stacks.Clear();
            OnInventoryChanged?.Invoke(this);
        }
    }
}
