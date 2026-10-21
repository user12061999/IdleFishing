using System;
using UnityEngine;

namespace IdleFishing.UI
{
    /// <summary>
    /// ScriptableObject chứa toàn bộ Sprite tham chiếu từ gói GUI Pro - Casual Game (Layer Lab).
    /// Được lưu trong Assets/Resources/CasualUIThemeConfig.asset để runtime & build có thể truy cập tức thì.
    /// </summary>
    [CreateAssetMenu(fileName = "CasualUIThemeConfig", menuName = "IdleFishing/Casual UI Theme Config", order = 2)]
    public class CasualUIThemeConfig : ScriptableObject
    {
        private static CasualUIThemeConfig instance;

        public static CasualUIThemeConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<CasualUIThemeConfig>("CasualUIThemeConfig");
#if UNITY_EDITOR
                    if (instance == null)
                    {
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CasualUIThemeConfig");
                        if (guids.Length > 0)
                        {
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            instance = UnityEditor.AssetDatabase.LoadAssetAtPath<CasualUIThemeConfig>(path);
                        }
                    }
#endif
                }
                return instance;
            }
        }

        [Header("Popup & Window Frames")]
        public Sprite popupNavyBg;
        public Sprite popupWhiteBg;
        public Sprite titleFlagBlue;
        public Sprite titleRibbonYellow;
        public Sprite titleRibbonBlue;
        public Sprite titleRibbonGreen;
        public Sprite popupLine;
        public Sprite popupTitleLine;
        public Sprite dimmedBg;

        [Header("Card & Container Frames")]
        public Sprite cardRound12;
        public Sprite cardRound20;
        public Sprite borderFrameBlue;
        public Sprite itemFrameNavy;
        public Sprite resourceBarBg;

        [Header("Action Buttons")]
        public Sprite btnGreen;
        public Sprite btnBlue;
        public Sprite btnYellow;
        public Sprite btnRed;
        public Sprite btnDark;
        public Sprite btnGray;
        public Sprite btnSquareGray;
        public Sprite btnSquareWhite;
        public Sprite btnSquareNavy;
        public Sprite btnCircleNavy;
        public Sprite btnCircleWhite;
        public Sprite btnTileBlue;

        [Header("Sliders & Progress Bars")]
        public Sprite sliderBg;
        public Sprite sliderFillGreen;
        public Sprite sliderFillBlue;
        public Sprite sliderFillYellow;
        public Sprite sliderFillRed;

        [Header("Icons")]
        public Sprite iconGold;
        public Sprite iconGolds;
        public Sprite iconClose;
        public Sprite iconMap;
        public Sprite iconCompass;
        public Sprite iconContract;
        public Sprite iconFish;
        public Sprite iconAnchor;
        public Sprite iconAd;
        public Sprite iconCrown;
        public Sprite iconCheck;
        public Sprite iconArrowUp;
        public Sprite iconSound;

        [Header("Dedicated Upgrade Icons")]
        public Sprite iconUpgradeFisherman;
        public Sprite iconUpgradeWrangler;
        public Sprite iconUpgradeFishValue;
    }
}
