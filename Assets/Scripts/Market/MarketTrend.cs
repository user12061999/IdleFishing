using System;
using UnityEngine;
using IdleFishing.Data;

namespace IdleFishing.Market
{
    [Serializable]
    public class MarketTrend
    {
        public string trendId;
        public string trendTitle;
        public string trendDescription;
        public string targetSpeciesId; // e.g. "salmon", "cod", "herring", or "" for any
        public ItemType? targetItemType; // e.g. WholeFish, or null for all
        public float priceMultiplier; // e.g. 1.5f (+50%), 2.0f (+100%)
        public float durationSeconds; // e.g. 120s
        public string iconBadge; // e.g. "🍣", "🔥", "🦴", "🐟"

        public MarketTrend(string id, string title, string desc, string speciesId, ItemType? itemType, float multiplier, float duration, string icon = "[HOT]")
        {
            trendId = id;
            trendTitle = title;
            trendDescription = desc;
            targetSpeciesId = speciesId;
            targetItemType = itemType;
            priceMultiplier = multiplier;
            durationSeconds = duration;
            iconBadge = icon;
        }

        public bool Matches(ItemData item)
        {
            if (item == null) return false;

            if (!string.IsNullOrEmpty(targetSpeciesId))
            {
                string id = !string.IsNullOrEmpty(item.ItemId) ? item.ItemId : item.name;
                if (!id.StartsWith(targetSpeciesId, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (targetItemType.HasValue)
            {
                if (item.Type != targetItemType.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
