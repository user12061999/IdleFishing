using UnityEngine;

namespace IdleFishing.Data
{
    public enum UpgradeType
    {
        FishermanSlots,         // Số lượng slot cần thủ câu cá (Mở rộng từ 1 -> 5 slot)
        FishermanSpeed,         // Tốc độ câu của cần thủ
        HireWrangler,           // Thuê thợ bắt cá (Tối đa 4 thợ)
        WranglerPower,          // Trang bị & sức mạnh thợ bắt cá (Tăng ATK, HP, Tốc chạy & Tap cá)
        KitchenSpeed,           // Dây chuyền chế biến (Tăng tốc độ trạm phi lê, thái lát, đóng khay)
        MarketValueMultiplier   // Thương hiệu hải sản (Tăng giá bán thành phẩm tại quầy hàng)
    }

    [System.Serializable]
    public class UpgradeDataEntry
    {
        public UpgradeType type;
        public string title;
        [TextArea(1, 3)]
        public string description;
        public int baseCost = 50;
        public float costMultiplier = 1.6f;
        public float baseValue = 1.0f;
        public float valueIncreasePerLevel = 0.25f;
        public int maxLevel = 10;
        public Sprite icon;

        public int GetCostForLevel(int currentLevel)
        {
            if (currentLevel >= maxLevel) return int.MaxValue;
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel));
        }

        public float GetValueForLevel(int currentLevel)
        {
            return baseValue + (currentLevel * valueIncreasePerLevel);
        }
    }

    [CreateAssetMenu(fileName = "UpgradeConfig", menuName = "IdleFishing/Upgrade Config", order = 4)]
    public class UpgradeConfig : ScriptableObject
    {
        [SerializeField] private UpgradeDataEntry[] upgrades;

        public UpgradeDataEntry[] Upgrades => upgrades;

        public UpgradeDataEntry GetEntry(UpgradeType type)
        {
            if (upgrades == null) return null;
            for (int i = 0; i < upgrades.Length; i++)
            {
                if (upgrades[i].type == type) return upgrades[i];
            }
            return null;
        }
    }
}
