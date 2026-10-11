using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;

namespace IdleFishing.Market
{
    public class MarketTrendManager : MonoBehaviour
    {
        public static MarketTrendManager Instance { get; private set; }

        [Header("Runtime State")]
        [SerializeField] private MarketTrend currentTrend;
        [SerializeField] private float timeRemaining = 0f;
        [SerializeField] private float trendCycleDuration = 120.0f; // 2 phút mỗi chu kỳ xu hướng

        public MarketTrend CurrentTrend => currentTrend;
        public float TimeRemaining => timeRemaining;
        public float TrendCycleDuration => trendCycleDuration;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            PickNewTrend();
        }

        private void OnEnable()
        {
            GameEvents.WorldChanged += OnWorldChanged;
        }

        private void OnDisable()
        {
            GameEvents.WorldChanged -= OnWorldChanged;
        }

        private void OnWorldChanged(WorldData newWorld)
        {
            PickNewTrend();
        }

        private void Update()
        {
            if (timeRemaining > 0f)
            {
                timeRemaining -= Time.deltaTime;
                if (timeRemaining <= 0f)
                {
                    PickNewTrend();
                }
            }
        }

        public void PickNewTrend()
        {
            string worldId = WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null
                ? WorldManager.Instance.CurrentWorld.worldId
                : "world_1";

            List<MarketTrend> pool = GetTrendPoolForWorld(worldId);
            if (pool.Count == 0) return;

            // Pick a trend different from current
            MarketTrend selected = pool[Random.Range(0, pool.Count)];
            if (currentTrend != null && pool.Count > 1 && selected.trendId == currentTrend.trendId)
            {
                selected = pool[(pool.IndexOf(selected) + 1) % pool.Count];
            }

            currentTrend = selected;
            timeRemaining = currentTrend.durationSeconds > 0 ? currentTrend.durationSeconds : trendCycleDuration;

            GameEvents.TriggerMarketTrendChanged(currentTrend);
            Debug.Log($"[MarketTrendManager] New market trend: {currentTrend.trendTitle} ({currentTrend.trendDescription})");
        }

        private List<MarketTrend> GetTrendPoolForWorld(string worldId)
        {
            var list = new List<MarketTrend>();

            switch (worldId)
            {
                case "world_2":
                    list.Add(new MarketTrend("trend_tuna", "Arctic Tuna Rush", "Fresh Arctic Tuna sell prices surge +60%!", "arctic_tuna", null, 1.6f, trendCycleDuration, "[HOT]"));
                    list.Add(new MarketTrend("trend_crab", "Royal Snow Crab Season", "Traders buy whole Snow Crab at +75% price!", "snow_crab", null, 1.75f, trendCycleDuration, "[CRAB]"));
                    list.Add(new MarketTrend("trend_fresh_w2", "Arctic Fresh Catch Gala", "All fresh whole fish sell for +50% price!", null, ItemType.WholeFish, 1.5f, trendCycleDuration, "[FISH]"));
                    break;

                case "world_3":
                    list.Add(new MarketTrend("trend_clown", "Tropical Clownfish Frenzy", "Collectors scramble for Clownfish at +70% price!", "clownfish", null, 1.7f, trendCycleDuration, "[HOT]"));
                    list.Add(new MarketTrend("trend_puffer", "Exotic Pufferfish Mania", "VIP buyers seek rare Pufferfish at +85% price!", "pufferfish", null, 1.85f, trendCycleDuration, "[PUFFER]"));
                    list.Add(new MarketTrend("trend_fresh_w3", "Tropical Harbor Festival", "All fresh whole fish get +50% price!", null, ItemType.WholeFish, 1.5f, trendCycleDuration, "[FISH]"));
                    break;

                default: // world_1
                    list.Add(new MarketTrend("trend_salmon", "Wild Salmon Season", "Fresh whole Salmon sell prices surge +50%!", "salmon", null, 1.5f, trendCycleDuration, "[HOT]"));
                    list.Add(new MarketTrend("trend_cod", "Harbor Cod Festival", "Merchants buy whole Cod at +60% price!", "cod", null, 1.6f, trendCycleDuration, "[COD]"));
                    list.Add(new MarketTrend("trend_herring", "Silver Herring Boom", "High demand for fresh Herring, sell price +45%!", "herring", null, 1.45f, trendCycleDuration, "[HERRING]"));
                    list.Add(new MarketTrend("trend_fresh_w1", "Harbor Fresh Catch Gala", "All fresh whole fish sell for +40% price!", null, ItemType.WholeFish, 1.4f, trendCycleDuration, "[FISH]"));
                    break;
            }

            return list;
        }

        public float GetPriceMultiplier(ItemData item, out bool isHotTrend)
        {
            isHotTrend = false;
            if (item == null || currentTrend == null) return 1.0f;

            if (currentTrend.Matches(item))
            {
                isHotTrend = true;
                return currentTrend.priceMultiplier;
            }

            return 1.0f;
        }
    }
}
