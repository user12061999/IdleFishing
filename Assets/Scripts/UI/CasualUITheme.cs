using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IdleFishing.UI
{
    public enum CasualBtnStyle
    {
        Green,
        Blue,
        Yellow,
        Red,
        Dark,
        Gray,
        SquareGray,
        SquareNavy,
        CircleNavy,
        TileBlue
    }

    public enum CasualSliderStyle
    {
        Green,
        Blue,
        Yellow,
        Red
    }

    /// <summary>
    /// Static Helper & Theme Provider cho GUI Pro - Casual Game.
    /// Cho phép mọi UI script áp dụng sprites, sliced frames, styles chuẩn commercial casual một cách nhanh gọn.
    /// </summary>
    public static class CasualUITheme
    {
        private static CasualUIThemeConfig Config => CasualUIThemeConfig.Instance;

        // Frames
        public static Sprite PopupNavyBg => Config != null ? Config.popupNavyBg : null;
        public static Sprite PopupWhiteBg => Config != null ? Config.popupWhiteBg : null;
        public static Sprite TitleFlagBlue => Config != null ? Config.titleFlagBlue : null;
        public static Sprite TitleRibbonYellow => Config != null ? Config.titleRibbonYellow : null;
        public static Sprite TitleRibbonBlue => Config != null ? Config.titleRibbonBlue : null;
        public static Sprite TitleRibbonGreen => Config != null ? Config.titleRibbonGreen : null;
        public static Sprite PopupLine => Config != null ? Config.popupLine : null;
        public static Sprite PopupTitleLine => Config != null ? Config.popupTitleLine : null;
        public static Sprite DimmedBg => Config != null ? Config.dimmedBg : null;

        // Cards
        public static Sprite CardRound12 => Config != null ? Config.cardRound12 : null;
        public static Sprite CardRound20 => Config != null ? Config.cardRound20 : null;
        public static Sprite BorderFrameBlue => Config != null ? Config.borderFrameBlue : null;
        public static Sprite ItemFrameNavy => Config != null ? Config.itemFrameNavy : null;
        public static Sprite ResourceBarBg => Config != null ? Config.resourceBarBg : null;

        // Buttons
        public static Sprite BtnGreen => Config != null ? Config.btnGreen : null;
        public static Sprite BtnBlue => Config != null ? Config.btnBlue : null;
        public static Sprite BtnYellow => Config != null ? Config.btnYellow : null;
        public static Sprite BtnRed => Config != null ? Config.btnRed : null;
        public static Sprite BtnDark => Config != null ? Config.btnDark : null;
        public static Sprite BtnGray => Config != null ? Config.btnGray : null;
        public static Sprite BtnSquareGray => Config != null ? Config.btnSquareGray : null;
        public static Sprite BtnSquareWhite => Config != null ? Config.btnSquareWhite : null;
        public static Sprite BtnSquareNavy => Config != null ? Config.btnSquareNavy : null;
        public static Sprite BtnCircleNavy => Config != null ? Config.btnCircleNavy : null;
        public static Sprite BtnCircleWhite => Config != null ? Config.btnCircleWhite : null;
        public static Sprite BtnTileBlue => Config != null && Config.btnTileBlue != null ? Config.btnTileBlue : BtnBlue;

        // Sliders
        public static Sprite SliderBg => Config != null ? Config.sliderBg : null;
        public static Sprite SliderFillGreen => Config != null ? Config.sliderFillGreen : null;
        public static Sprite SliderFillBlue => Config != null ? Config.sliderFillBlue : null;
        public static Sprite SliderFillYellow => Config != null ? Config.sliderFillYellow : null;
        public static Sprite SliderFillRed => Config != null ? Config.sliderFillRed : null;

        // Icons
        public static Sprite IconGold => Config != null ? Config.iconGold : null;
        public static Sprite IconGolds => Config != null ? Config.iconGolds : null;
        public static Sprite IconClose => Config != null ? Config.iconClose : null;
        public static Sprite IconMap => Config != null ? Config.iconMap : null;
        public static Sprite IconCompass => Config != null ? Config.iconCompass : null;
        public static Sprite IconContract => Config != null ? Config.iconContract : null;
        public static Sprite IconFish => Config != null ? Config.iconFish : null;
        public static Sprite IconAnchor => Config != null ? Config.iconAnchor : null;
        public static Sprite IconAd => Config != null ? Config.iconAd : null;
        public static Sprite IconCrown => Config != null ? Config.iconCrown : null;
        public static Sprite IconCheck => Config != null ? Config.iconCheck : null;
        public static Sprite IconArrowUp => Config != null ? Config.iconArrowUp : null;
        public static Sprite IconSound => Config != null ? Config.iconSound : null;
        public static Sprite IconUpgradeFisherman => Config != null ? Config.iconUpgradeFisherman : null;
        public static Sprite IconUpgradeWrangler => Config != null ? Config.iconUpgradeWrangler : null;
        public static Sprite IconUpgradeFishValue => Config != null ? Config.iconUpgradeFishValue : null;

        /// <summary>
        /// Áp dụng style Button chuẩn casual: Sliced Image, màu trắng (tint nguyên bản của texture), transition color tinh tế.
        /// </summary>
        public static void StyleButton(Button btn, Image img, CasualBtnStyle style)
        {
            if (img == null) return;

            Sprite targetSprite = null;
            switch (style)
            {
                case CasualBtnStyle.Green: targetSprite = BtnGreen; break;
                case CasualBtnStyle.Blue: targetSprite = BtnBlue; break;
                case CasualBtnStyle.Yellow: targetSprite = BtnYellow; break;
                case CasualBtnStyle.Red: targetSprite = BtnRed; break;
                case CasualBtnStyle.Dark: targetSprite = BtnDark; break;
                case CasualBtnStyle.Gray: targetSprite = BtnGray; break;
                case CasualBtnStyle.SquareGray: targetSprite = BtnSquareGray; break;
                case CasualBtnStyle.SquareNavy: targetSprite = BtnSquareNavy; break;
                case CasualBtnStyle.CircleNavy: targetSprite = BtnCircleNavy; break;
                case CasualBtnStyle.TileBlue: targetSprite = BtnTileBlue; break;
            }

            if (targetSprite != null)
            {
                img.sprite = targetSprite;
                img.type = (targetSprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
                img.color = Color.white;
            }

            if (btn != null)
            {
                btn.targetGraphic = img;
                btn.transition = Selectable.Transition.ColorTint;
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
                cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
                cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
                btn.colors = cb;
            }
        }

        /// <summary>
        /// Chuẩn hóa Button dạng Tile (Icon lớn ở trung tâm trên, Text ở dưới đáy button) chuẩn Casual Game.
        /// </summary>
        public static void StyleTileButton(
            GameObject buttonObj,
            Sprite iconSprite,
            string labelText,
            CasualBtnStyle style = CasualBtnStyle.TileBlue,
            Color? textColor = null,
            Vector2? buttonSize = null,
            Vector2? iconSize = null)
        {
            if (buttonObj == null) return;

            Vector2 btnSize = buttonSize ?? new Vector2(74f, 80f);
            Vector2 icSize = iconSize ?? new Vector2(38f, 38f);

            RectTransform btnRt = buttonObj.GetComponent<RectTransform>();
            if (btnRt != null)
            {
                btnRt.sizeDelta = btnSize;
            }

            Image btnImg = buttonObj.GetComponent<Image>();
            if (btnImg == null) btnImg = buttonObj.AddComponent<Image>();
            Button btn = buttonObj.GetComponent<Button>();
            if (btn == null) btn = buttonObj.AddComponent<Button>();
            StyleButton(btn, btnImg, style);

            // 1. Icon (Inside, Center/Top)
            Transform iconTrans = buttonObj.transform.Find("Icon");
            GameObject iconObj = iconTrans != null ? iconTrans.gameObject : null;
            if (iconObj == null)
            {
                iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(buttonObj.transform, false);
            }
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            if (iconRt == null) iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(0f, btnSize.y >= 100f ? 15f : 11f);
            iconRt.sizeDelta = icSize;

            Image iconImg = iconObj.GetComponent<Image>();
            if (iconImg == null) iconImg = iconObj.AddComponent<Image>();
            if (iconSprite != null) iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = Color.white;

            // 2. Text (At Bottom of button)
            Transform textTrans = buttonObj.transform.Find("Text");
            GameObject textObj = textTrans != null ? textTrans.gameObject : null;
            if (textObj == null)
            {
                textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
            }
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            if (textRt == null) textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 0f);
            textRt.pivot = new Vector2(0.5f, 0f);
            textRt.anchoredPosition = new Vector2(0f, 5f);
            textRt.sizeDelta = new Vector2(0f, btnSize.y >= 100f ? 30f : 22f);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor ?? Color.white;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 9f;
            tmp.fontSizeMax = btnSize.y >= 100f ? 20f : 12.5f;
            tmp.fontSize = btnSize.y >= 100f ? 18f : 12f;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            tmp.ApplyFont(UIFontCategory.Button);
        }

        /// <summary>
        /// Áp dụng style Card container bo góc chuẩn Casual.
        /// </summary>
        public static void StyleCard(Image img, Color? tint = null)
        {
            if (img == null) return;
            Sprite sp = CardRound12;
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Sliced;
                img.color = tint ?? new Color(0.10f, 0.14f, 0.22f, 0.95f);
            }
        }

        /// <summary>
        /// Áp dụng style Dialog Popup nền Navy bo góc.
        /// </summary>
        public static void StylePopupDialog(Image img)
        {
            if (img == null) return;
            Sprite sp = PopupNavyBg ?? PopupWhiteBg;
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
        }

        /// <summary>
        /// Áp dụng style Progress/Health slider.
        /// </summary>
        public static void StyleSlider(Image bg, Image fill, CasualSliderStyle style)
        {
            if (bg != null && SliderBg != null)
            {
                bg.sprite = SliderBg;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }

            if (fill != null)
            {
                Sprite fillSp = null;
                switch (style)
                {
                    case CasualSliderStyle.Green: fillSp = SliderFillGreen; break;
                    case CasualSliderStyle.Blue: fillSp = SliderFillBlue; break;
                    case CasualSliderStyle.Yellow: fillSp = SliderFillYellow; break;
                    case CasualSliderStyle.Red: fillSp = SliderFillRed; break;
                }

                if (fillSp != null)
                {
                    fill.sprite = fillSp;
                    fill.type = Image.Type.Sliced;
                    fill.color = Color.white;
                }
            }
        }
    }
}
