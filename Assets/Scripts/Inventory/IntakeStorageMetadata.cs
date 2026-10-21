using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Data;

namespace IdleFishing.Inventory
{
    public class IntakeStorageMetadata : MonoBehaviour
    {
        private readonly Queue<CarriedFishPayload> storedFishQueue = new Queue<CarriedFishPayload>();

        public void StoreFish(ItemData item, FishSizeCategory size, FishQualityTier quality)
        {
            storedFishQueue.Enqueue(new CarriedFishPayload(item, size, quality));
        }

        public bool TryRetrieveFish(out CarriedFishPayload payload)
        {
            if (storedFishQueue.Count > 0)
            {
                payload = storedFishQueue.Dequeue();
                return true;
            }

            payload = default;
            return false;
        }

        public void Clear()
        {
            storedFishQueue.Clear();
        }
    }
}
