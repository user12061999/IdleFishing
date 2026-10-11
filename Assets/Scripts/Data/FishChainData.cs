using UnityEngine;

namespace IdleFishing.Data
{
    [CreateAssetMenu(fileName = "FishChainData", menuName = "IdleFishing/Fish Chain Data", order = 2)]
    public class FishChainData : ScriptableObject
    {
        [Header("Species Info")]
        [SerializeField] private string speciesId;
        [SerializeField] private string speciesName;
        [Range(1, 5)]
        [SerializeField] private int tier = 1;

        [Header("Catch Parameters")]
        [SerializeField] private float catchDifficulty = 1.0f; // Độ khó kéo cá
        [SerializeField] private float spawnWeight = 100f;      // Tỉ lệ xuất hiện
        [SerializeField] private float minWeightKg = 1.5f;
        [SerializeField] private float maxWeightKg = 4.0f;

        [Header("Boss Classification")]
        [SerializeField] private bool isApexBoss = false;
        [SerializeField] private string bossTitle = "";

        [Header("3D Whole Fish Item")]
        [Tooltip("Whole 3D fish model item")]
        [SerializeField] private ItemData wholeFish;

        public string SpeciesId => speciesId;
        public string SpeciesName => speciesName;
        public int Tier => tier;
        public float CatchDifficulty => catchDifficulty;
        public float SpawnWeight => spawnWeight;
        public float MinWeightKg => minWeightKg;
        public float MaxWeightKg => maxWeightKg;
        public bool IsApexBoss => isApexBoss;
        public string BossTitle => string.IsNullOrEmpty(bossTitle) ? $"APEX {speciesName.ToUpper()}" : bossTitle;

        public ItemData WholeFish => wholeFish;

        public void Initialize(
            string id,
            string name,
            ItemData whole,
            int fishTier = 1,
            float difficulty = 1.0f,
            float weight = 100f,
            float minKg = 1.5f,
            float maxKg = 4.0f,
            bool apexBoss = false,
            string customBossTitle = "")
        {
            speciesId = id;
            speciesName = name;
            wholeFish = whole;
            tier = Mathf.Clamp(fishTier, 1, 5);
            catchDifficulty = difficulty;
            spawnWeight = weight;
            minWeightKg = minKg;
            maxWeightKg = maxKg;
            isApexBoss = apexBoss;
            bossTitle = customBossTitle;
        }
    }
}
