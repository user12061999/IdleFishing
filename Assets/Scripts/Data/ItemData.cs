using UnityEngine;

namespace IdleFishing.Data
{
    public enum ItemType
    {
        WholeFish       // Cá nguyên con 3D (Salmon, Cod, Herring, Tuna, Snow Crab, Clownfish, Pufferfish...)
    }

    [CreateAssetMenu(fileName = "ItemData", menuName = "IdleFishing/Item Data", order = 1)]
    public class ItemData : ScriptableObject
    {
        [Header("Item Info")]
        [SerializeField] private string itemId;
        [SerializeField] private string itemName;
        [SerializeField] private ItemType itemType;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Economics")]
        [SerializeField] private int basePrice = 10;

        [Header("Visuals")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite icon;

        public string ItemId => itemId;
        public string ItemName => itemName;
        public ItemType Type => itemType;
        public string Description => description;
        public int BasePrice => basePrice;
        public GameObject Prefab => prefab;
        public Sprite Icon => icon;

        public void Initialize(string id, string name, ItemType type, int price, GameObject prefabAsset)
        {
            itemId = id;
            itemName = name;
            itemType = type;
            basePrice = price;
            prefab = prefabAsset;
        }
    }
}
