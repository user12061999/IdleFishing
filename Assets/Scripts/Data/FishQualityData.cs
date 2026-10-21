using System;
using UnityEngine;

namespace IdleFishing.Data
{
    public enum FishSizeCategory
    {
        Standard = 0,   // Tiêu chuẩn (x1.0 scale, x1.0 giá, x1.0 HP)
        Big = 1,        // To / Nặng cân (x1.22 scale, x1.5 giá, x1.25 HP)
        Colossal = 2    // Khổng lồ / Thủy quái mini (x1.45 scale, x2.2 giá, x1.6 HP)
    }

    public enum FishQualityTier
    {
        Regular = 0,    // Cá tươi bình thường (x1.0 giá, màu toon nguyên bản)
        Silver = 1,     // Bạc lấp lánh (x1.5 giá, ánh bạc kim loại)
        Golden = 2,     // Hoàng Kim (x3.0 giá, phủ vàng chói lọi + bụi vàng)
        Rainbow = 3     // Cầu Vồng Thần Thoại (x5.0 giá, đổi màu đa sắc liên tục)
    }

    public struct CarriedFishPayload
    {
        public ItemData item;
        public FishSizeCategory size;
        public FishQualityTier quality;

        public CarriedFishPayload(ItemData item, FishSizeCategory size, FishQualityTier quality)
        {
            this.item = item;
            this.size = size;
            this.quality = quality;
        }
    }

    public static class FishQualityData
    {
        public static FishSizeCategory RollRandomSize()
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.07f) return FishSizeCategory.Colossal; // 7% Khổng lồ
            if (roll < 0.25f) return FishSizeCategory.Big;      // 18% To lớn
            return FishSizeCategory.Standard;                   // 75% Bình thường
        }

        public static FishQualityTier RollRandomQuality(float bonusLuck = 0f)
        {
            float roll = UnityEngine.Random.value;
            // Tỷ lệ xuất hiện phẩm chất đặc biệt
            float rainbowThreshold = 0.01f + bonusLuck * 0.01f;
            float goldenThreshold = 0.06f + bonusLuck * 0.03f;
            float silverThreshold = 0.20f + bonusLuck * 0.05f;

            if (roll < rainbowThreshold) return FishQualityTier.Rainbow; // 1% Cầu Vồng
            if (roll < goldenThreshold) return FishQualityTier.Golden;   // 5% Hoàng Kim
            if (roll < silverThreshold) return FishQualityTier.Silver;   // 14% Bạc Lấp Lánh
            return FishQualityTier.Regular;                             // 80% Bình thường
        }

        public static float GetSizeScale(FishSizeCategory size)
        {
            switch (size)
            {
                case FishSizeCategory.Big: return 1.30f;
                case FishSizeCategory.Colossal: return 1.65f;
                default: return 1.0f;
            }
        }

        public static float GetSizeHPMultiplier(FishSizeCategory size)
        {
            switch (size)
            {
                case FishSizeCategory.Big: return 1.25f;
                case FishSizeCategory.Colossal: return 1.60f;
                default: return 1.0f;
            }
        }

        public static float GetSizePriceMultiplier(FishSizeCategory size)
        {
            switch (size)
            {
                case FishSizeCategory.Big: return 1.50f;
                case FishSizeCategory.Colossal: return 2.20f;
                default: return 1.0f;
            }
        }

        public static float GetQualityPriceMultiplier(FishQualityTier quality)
        {
            switch (quality)
            {
                case FishQualityTier.Silver: return 1.50f;
                case FishQualityTier.Golden: return 3.0f;
                case FishQualityTier.Rainbow: return 5.0f;
                default: return 1.0f;
            }
        }

        public static Color GetQualityColor(FishQualityTier quality)
        {
            switch (quality)
            {
                case FishQualityTier.Silver: return new Color(0.92f, 0.95f, 1.0f);
                case FishQualityTier.Golden: return new Color(1.0f, 0.82f, 0.15f);
                case FishQualityTier.Rainbow: return new Color(1.0f, 0.45f, 0.85f);
                default: return Color.white;
            }
        }

        public static string GetQualityPrefix(FishQualityTier quality, FishSizeCategory size)
        {
            string qStr = "";
            switch (quality)
            {
                case FishQualityTier.Rainbow: qStr = "[RAINBOW] "; break;
                case FishQualityTier.Golden: qStr = "[GOLDEN] "; break;
                case FishQualityTier.Silver: qStr = "[SILVER] "; break;
            }

            string sStr = "";
            switch (size)
            {
                case FishSizeCategory.Colossal: sStr = "COLOSSAL "; break;
                case FishSizeCategory.Big: sStr = "GIANT "; break;
            }

            return qStr + sStr;
        }
    }
}
