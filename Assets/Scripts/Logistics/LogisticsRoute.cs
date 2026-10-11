using UnityEngine;
using IdleFishing.Data;
using IdleFishing.Inventory;

namespace IdleFishing.Logistics
{
    public enum RouteUrgencyLevel
    {
        OceanExport = 0,        // 0: Hợp đồng Tàu Viễn Dương cấp bách (có thời hạn đếm ngược)
        MarketStarvation = 1,   // 1: Quầy bán chợ cạn sạch hàng (nguy cơ dừng dòng tiền)
        StationJam = 2,         // 2: Trạm sản xuất bị ứ nghẽn đầu ra (>= 75% sức chứa)
        StationStarvation = 3,  // 3: Trạm chế biến kế tiếp hết sạch nguyên liệu (<= 1)
        BatchTransfer = 4,      // 4: Gom đủ mẻ hàng lớn để chở 1 chuyến đầy xe (>= 2 món)
        Standard = 5            // 5: Vận chuyển thường nhật
    }

    [System.Serializable]
    public class LogisticsRoute
    {
        public string routeName;
        public Inventory.Inventory sourceInventory;
        public Vector3 pickupPosition;
        public Inventory.Inventory targetInventory;
        public Vector3 dropoffPosition;
        public ItemType itemType;
        public int priority = 1; // 1: Cao nhất (Thành phẩm ra chợ), 5: Thấp nhất
        public bool isExportContract = false;
        public System.Collections.Generic.List<Vector3> waypointsToDropoff = new System.Collections.Generic.List<Vector3>();
        public System.Collections.Generic.List<Vector3> waypointsToPickup = new System.Collections.Generic.List<Vector3>();

        [System.NonSerialized]
        public int reservedItemCount = 0; // Số lượng hàng các xe khác đã đặt chỗ để lấy

        public bool CanExecute()
        {
            if (sourceInventory == null || targetInventory == null) return false;
            if (targetInventory.IsFull) return false;

            int count = sourceInventory.GetCountOfType(itemType);
            return count > reservedItemCount;
        }

        public RouteUrgencyLevel GetDynamicUrgency()
        {
            if (isExportContract) return RouteUrgencyLevel.OceanExport;

            // 1. Quầy chợ cạn hàng cá tươi (Emergency Market Restock)
            if (targetInventory != null && targetInventory.TotalItemCount <= 1 &&
                itemType == ItemType.WholeFish)
            {
                return RouteUrgencyLevel.MarketStarvation;
            }

            // 2. Trạm chế biến bị ứ nghẽn đầu ra (Jam Relief)
            if (sourceInventory != null && sourceInventory.TotalItemCount >= Mathf.FloorToInt(sourceInventory.MaxCapacity * 0.75f))
            {
                return RouteUrgencyLevel.StationJam;
            }

            // 3. Trạm kế tiếp thiếu nguyên liệu (Starvation Relief)
            if (targetInventory != null && targetInventory.TotalItemCount <= 1)
            {
                return RouteUrgencyLevel.StationStarvation;
            }

            // 4. Đủ mẻ hàng gom chuyến (Batch Transfer)
            if (sourceInventory != null && sourceInventory.GetCountOfType(itemType) >= 2)
            {
                return RouteUrgencyLevel.BatchTransfer;
            }

            return RouteUrgencyLevel.Standard;
        }

        public void ReserveItems(int count)
        {
            reservedItemCount += count;
        }

        public void ReleaseReservation(int count)
        {
            reservedItemCount = Mathf.Max(0, reservedItemCount - count);
        }
    }
}
