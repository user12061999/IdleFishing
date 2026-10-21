using UnityEngine;
using TMPro;

namespace IdleFishing.UI
{
    /// <summary>
    /// Component that automatically applies fonts configured in the UIFontConfig ScriptableObject.
    /// Attach this component to individual TextMeshPro objects or to parent UI containers/canvases.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class UIFontApplier : MonoBehaviour
    {
        [Tooltip("Font category to apply from UIFontConfig.")]
        [SerializeField] private FontCategory category = FontCategory.Default;

        [Tooltip("If true, applies the font to all child TMP_Text elements in this hierarchy.")]
        [SerializeField] private bool applyToChildren = false;

        private void Awake()
        {
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        public void Apply()
        {
            if (UIFontConfig.Instance == null) return;

            if (applyToChildren)
            {
                var texts = GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    UIFontConfig.Instance.Apply(texts[i], category);
                }
            }
            else
            {
                var text = GetComponent<TMP_Text>();
                if (text != null)
                {
                    UIFontConfig.Instance.Apply(text, category);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Apply();
        }
#endif
    }
}
