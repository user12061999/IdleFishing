using System;
using UnityEngine;
using IdleFishing.Data;
using IdleFishing.Market;
using IdleFishing.Fishing;
using IdleFishing.Progression;

namespace IdleFishing.Core
{
    public static class GameEvents
    {
        // Fishing
        public static event Action<ItemData> FishCaught;
        public static void TriggerFishCaught(ItemData fish) => FishCaught?.Invoke(fish);

        // Boat & Cargo
        public static event Action<int, int> CargoUpdated; // current, max
        public static void TriggerCargoUpdated(int current, int max) => CargoUpdated?.Invoke(current, max);

        public static event Action BoatDocked;
        public static void TriggerBoatDocked() => BoatDocked?.Invoke();

        public static event Action BoatDeparted;
        public static void TriggerBoatDeparted() => BoatDeparted?.Invoke();

        // Economy & Sales
        public static event Action<ItemData, int> ItemSold; // item, price
        public static void TriggerItemSold(ItemData item, int price) => ItemSold?.Invoke(item, price);

        public static event Action<int> CoinsChanged; // newBalance
        public static void TriggerCoinsChanged(int newBalance) => CoinsChanged?.Invoke(newBalance);

        // Upgrades
        public static event Action<UpgradeType, int> UpgradePurchased; // type, newLevel
        public static void TriggerUpgradePurchased(UpgradeType type, int newLevel) => UpgradePurchased?.Invoke(type, newLevel);

        // Boss Events
        public static event Action<FishChainData> BossFishSpawned;
        public static void TriggerBossFishSpawned(FishChainData species) => BossFishSpawned?.Invoke(species);

        public static event Action<FishChainData, int> BossFishDefeated; // species, bonusCoins
        public static void TriggerBossFishDefeated(FishChainData species, int bonusCoins) => BossFishDefeated?.Invoke(species, bonusCoins);

        // Offline Earnings
        public static event Action<int> OfflineEarningsClaimed;
        public static void TriggerOfflineEarningsClaimed(int amount) => OfflineEarningsClaimed?.Invoke(amount);

        // Multi-World Progression
        public static event Action<WorldData> WorldChanged;
        public static void TriggerWorldChanged(WorldData world) => WorldChanged?.Invoke(world);

        public static event Action<WorldData> WorldUnlocked;
        public static void TriggerWorldUnlocked(WorldData world) => WorldUnlocked?.Invoke(world);

        // Fishing Frenzy Mode
        public static event Action<float> FrenzyChargeChanged; // currentCharge 0..100
        public static void TriggerFrenzyChargeChanged(float currentCharge) => FrenzyChargeChanged?.Invoke(currentCharge);

        public static event Action FrenzyStarted;
        public static void TriggerFrenzyStarted() => FrenzyStarted?.Invoke();

        public static event Action FrenzyEnded;
        public static void TriggerFrenzyEnded() => FrenzyEnded?.Invoke();

        // Sunken Treasure
        public static event Action TreasureChestSpawned;
        public static void TriggerTreasureChestSpawned() => TreasureChestSpawned?.Invoke();

        public static event Action<int> TreasureChestOpened; // bonusGold
        public static void TriggerTreasureChestOpened(int bonusGold) => TreasureChestOpened?.Invoke(bonusGold);

        // Harbor Commercial Contracts & Fame
        public static event Action<HarborContract> ContractCompleted;
        public static void TriggerContractCompleted(HarborContract contract) => ContractCompleted?.Invoke(contract);

        public static event Action ContractsRefreshed;
        public static void TriggerContractsRefreshed() => ContractsRefreshed?.Invoke();

        public static event Action<int, int> HarborFameChanged; // currentFame, harborLevel
        public static void TriggerHarborFameChanged(int currentFame, int harborLevel) => HarborFameChanged?.Invoke(currentFame, harborLevel);

        // Market Trends
        public static event Action<MarketTrend> MarketTrendChanged;
        public static void TriggerMarketTrendChanged(MarketTrend trend) => MarketTrendChanged?.Invoke(trend);

        // Aquarium & Bestiary (Phase 2)
        public static event Action<FishChainData, bool> AquariumFishAdded; // species, isBoss
        public static void TriggerAquariumFishAdded(FishChainData species, bool isBoss) => AquariumFishAdded?.Invoke(species, isBoss);

        public static event Action<FishChainData> AquariumFishRemoved;
        public static void TriggerAquariumFishRemoved(FishChainData species) => AquariumFishRemoved?.Invoke(species);

        public static event Action<int> AquariumTicketClaimed; // amount
        public static void TriggerAquariumTicketClaimed(int amount) => AquariumTicketClaimed?.Invoke(amount);

        public static event Action<FishChainData> BestiarySpeciesDiscovered;
        public static void TriggerBestiarySpeciesDiscovered(FishChainData species) => BestiarySpeciesDiscovered?.Invoke(species);

        public static event Action<int, int> AquariumUpgraded; // capacity, decorLevel
        public static void TriggerAquariumUpgraded(int capacity, int decorLevel) => AquariumUpgraded?.Invoke(capacity, decorLevel);

        // Prestige & Harbor Medals
        public static event Action<int> PrestigeCompleted; // medalsGained
        public static void TriggerPrestigeCompleted(int medalsGained) => PrestigeCompleted?.Invoke(medalsGained);

        // Quests & FTUE Milestones
        public static event Action<QuestStep, int, int> QuestProgressChanged; // quest, current, target
        public static void TriggerQuestProgressChanged(QuestStep quest, int current, int target) => QuestProgressChanged?.Invoke(quest, current, target);

        public static event Action<QuestStep> QuestCompleted;
        public static void TriggerQuestCompleted(QuestStep quest) => QuestCompleted?.Invoke(quest);

        public static event Action<QuestStep, int> QuestClaimed; // quest, rewardCoins
        public static void TriggerQuestClaimed(QuestStep quest, int rewardCoins) => QuestClaimed?.Invoke(quest, rewardCoins);

        public static event Action<QuestStep> ActiveQuestChanged;
        public static void TriggerActiveQuestChanged(QuestStep newQuest) => ActiveQuestChanged?.Invoke(newQuest);

        // Fish Defeated on Deck
        public static event Action<FishChainData, LiveFish> FishDefeated;
        public static void TriggerFishDefeated(FishChainData species, LiveFish liveFish) => FishDefeated?.Invoke(species, liveFish);

        // Worker & Logistics Operations
        public static event Action<string, int> WorkerHired; // role ("wrangler", "transporter"), newCount
        public static void TriggerWorkerHired(string role, int newCount) => WorkerHired?.Invoke(role, newCount);

        public static event Action<int> PayloadTransported; // payload count
        public static void TriggerPayloadTransported(int count) => PayloadTransported?.Invoke(count);

        // 30-Stage Progression
        public static event Action<StageData> StageStarted;
        public static void TriggerStageStarted(StageData stage) => StageStarted?.Invoke(stage);

        public static event Action<StageData, int, int, int> StageGoalProgressChanged; // stage, goalIndex, current, target
        public static void TriggerStageGoalProgressChanged(StageData stage, int goalIndex, int current, int target) => StageGoalProgressChanged?.Invoke(stage, goalIndex, current, target);

        public static event Action<StageData> StageCompleted;
        public static void TriggerStageCompleted(StageData stage) => StageCompleted?.Invoke(stage);
    }
}
