using System;
using UnityEngine;
using TMPro;

namespace IdleFishing.UI
{
    public enum FontCategory
    {
        Default,
        Header,
        Number,
        Button
    }

    public enum UIFontCategory
    {
        Default,
        Header,
        Numbers,
        Button
    }

    /// <summary>
    /// ScriptableObject managing typography and font assets across the game.
    /// Configure fonts here in the Unity Inspector to instantly update UI styling across all game views.
    /// </summary>
    [CreateAssetMenu(fileName = "UIFontConfig", menuName = "IdleFishing/UI Font Config", order = 1)]
    public class UIFontConfig : ScriptableObject
    {
        private static UIFontConfig instance;

        public static UIFontConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<UIFontConfig>("UIFontConfig");
#if UNITY_EDITOR
                    if (instance == null)
                    {
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:UIFontConfig");
                        if (guids.Length > 0)
                        {
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            instance = UnityEditor.AssetDatabase.LoadAssetAtPath<UIFontConfig>(path);
                        }
                    }
#endif
                }
                return instance;
            }
        }

        [Header("Primary Font Assets")]
        [Tooltip("Standard font used for descriptions, details, general UI labels.")]
        [SerializeField] private TMP_FontAsset defaultFont;

        [Tooltip("Bold/Stylized font used for titles, headers, world names, and banner alerts.")]
        [SerializeField] private TMP_FontAsset headerFont;

        [Tooltip("Font used for coin counters, numbers, floating damage, and prices.")]
        [SerializeField] private TMP_FontAsset numberFont;

        [Tooltip("Font used for button labels and clickable badges.")]
        [SerializeField] private TMP_FontAsset buttonFont;

        [Header("Material Presets (Optional)")]
        [SerializeField] private Material headerMaterial;
        [SerializeField] private Material numberMaterial;
        [SerializeField] private Material buttonMaterial;

        public TMP_FontAsset DefaultFont => defaultFont;
        public TMP_FontAsset HeaderFont => headerFont != null ? headerFont : defaultFont;
        public TMP_FontAsset NumberFont => numberFont != null ? numberFont : defaultFont;
        public TMP_FontAsset ButtonFont => buttonFont != null ? buttonFont : defaultFont;

        public Material HeaderMaterial => headerMaterial;
        public Material NumberMaterial => numberMaterial;
        public Material ButtonMaterial => buttonMaterial;

        /// <summary>
        /// Retrieves the configured TMP_FontAsset for a specified category.
        /// </summary>
        public TMP_FontAsset GetFont(FontCategory category)
        {
            switch (category)
            {
                case FontCategory.Header: return HeaderFont;
                case FontCategory.Number: return NumberFont;
                case FontCategory.Button: return ButtonFont;
                default: return DefaultFont;
            }
        }

        /// <summary>
        /// Applies the font and optional material preset to a TextMeshPro component.
        /// </summary>
        public void Apply(TMP_Text textComponent, FontCategory category = FontCategory.Default)
        {
            if (textComponent == null) return;

            TMP_FontAsset font = GetFont(category);
            if (font != null)
            {
                textComponent.font = font;
            }

            switch (category)
            {
                case FontCategory.Header:
                    if (headerMaterial != null) textComponent.fontSharedMaterial = headerMaterial;
                    break;
                case FontCategory.Number:
                    if (numberMaterial != null) textComponent.fontSharedMaterial = numberMaterial;
                    break;
                case FontCategory.Button:
                    if (buttonMaterial != null) textComponent.fontSharedMaterial = buttonMaterial;
                    break;
            }
        }

        /// <summary>
        /// Recursively applies the configured font to all TMP_Text components under a GameObject hierarchy.
        /// </summary>
        public void ApplyToHierarchy(GameObject root, FontCategory category = FontCategory.Default)
        {
            if (root == null) return;
            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Apply(texts[i], category);
            }
        }
    }

    /// <summary>
    /// Extension helper methods for easy font setup on any TMP_Text.
    /// </summary>
    public static class UIFontExtensions
    {
        public static void ApplyFont(this TMP_Text textComponent, FontCategory category = FontCategory.Default)
        {
            if (textComponent == null) return;
            if (UIFontConfig.Instance != null)
            {
                UIFontConfig.Instance.Apply(textComponent, category);
            }
        }

        public static void ApplyFont(this TMP_Text textComponent, UIFontCategory category)
        {
            if (textComponent == null) return;
            FontCategory mapped = category switch
            {
                UIFontCategory.Header => FontCategory.Header,
                UIFontCategory.Numbers => FontCategory.Number,
                UIFontCategory.Button => FontCategory.Button,
                _ => FontCategory.Default
            };
            if (UIFontConfig.Instance != null)
            {
                UIFontConfig.Instance.Apply(textComponent, mapped);
            }
        }
    }
}
