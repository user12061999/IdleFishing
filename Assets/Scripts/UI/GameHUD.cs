using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;

namespace IdleFishing.UI
{
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }

        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI coinsText;

        [Header("Level & Land Display")]
        [SerializeField] private Button levelBadgeButton;
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private TextMeshProUGUI landNameText;
        [SerializeField] private GameObject nextLevelAlertBadge;
        [SerializeField] private TextMeshProUGUI nextLevelAlertText;

        private Coroutine punchRoutine;

        public RectTransform CoinsRect => coinsText != null ? coinsText.rectTransform : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (levelBadgeButton != null)
            {
                levelBadgeButton.onClick.AddListener(OnLevelBadgeClicked);
            }
        }

        private void OnEnable()
        {
            GameEvents.CoinsChanged += UpdateCoins;
            GameEvents.WorldChanged += OnWorldChanged;
            GameEvents.WorldUnlocked += OnWorldUnlocked;
        }

        private void OnDisable()
        {
            GameEvents.CoinsChanged -= UpdateCoins;
            GameEvents.WorldChanged -= OnWorldChanged;
            GameEvents.WorldUnlocked -= OnWorldUnlocked;
        }

        private void Start()
        {
            if (EconomyManager.Instance != null)
            {
                UpdateCoins(EconomyManager.Instance.Coins);
            }
            UpdateLevelDisplay();
        }

        public void SetupLevelBadge(
            Button badgeBtn,
            TextMeshProUGUI lvlText,
            TextMeshProUGUI landText,
            GameObject nextLvlBadge = null,
            TextMeshProUGUI nextLvlText = null)
        {
            levelBadgeButton = badgeBtn;
            levelTitleText = lvlText;
            landNameText = landText;
            nextLevelAlertBadge = nextLvlBadge;
            nextLevelAlertText = nextLvlText;

            if (levelBadgeButton != null)
            {
                levelBadgeButton.onClick.RemoveAllListeners();
                levelBadgeButton.onClick.AddListener(OnLevelBadgeClicked);
            }

            UpdateLevelDisplay();
        }

        private void OnLevelBadgeClicked()
        {
            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayUIClick();
            }

            if (WorldSelectionPopupUI.Instance != null)
            {
                WorldSelectionPopupUI.Instance.Open();
            }
        }

        private void OnWorldChanged(WorldData newWorld)
        {
            UpdateLevelDisplay();
        }

        private void OnWorldUnlocked(WorldData unlockedWorld)
        {
            UpdateLevelDisplay();
        }

        public void UpdateLevelDisplay()
        {
            WorldData curWorld = WorldManager.Instance != null ? WorldManager.Instance.CurrentWorld : null;
            if (curWorld != null)
            {
                if (levelTitleText != null)
                {
                    levelTitleText.text = $"LEVEL {curWorld.worldOrder}";
                }
                if (landNameText != null)
                {
                    landNameText.text = curWorld.worldName;
                }
            }

            CheckNextLevelReady();
        }

        public void CheckNextLevelReady()
        {
            if (nextLevelAlertBadge == null || WorldManager.Instance == null) return;

            var worlds = WorldManager.Instance.Worlds;
            int curIndex = WorldManager.Instance.ActiveWorldIndex;
            int nextIndex = curIndex + 1;

            bool canUnlockNext = false;
            if (nextIndex < worlds.Count)
            {
                WorldData nextWorld = worlds[nextIndex];
                if (!WorldManager.Instance.IsWorldUnlocked(nextWorld))
                {
                    canUnlockNext = WorldManager.Instance.CanUnlockWorld(nextWorld, out _);
                }
            }

            nextLevelAlertBadge.SetActive(canUnlockNext);
            if (nextLevelAlertText != null && canUnlockNext)
            {
                nextLevelAlertText.text = "LEVEL UP READY!";
            }
        }

        public void PunchCoinsCounter()
        {
            if (coinsText == null) return;
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(PunchCoinsRoutine());
        }

        private System.Collections.IEnumerator PunchCoinsRoutine()
        {
            Transform t = coinsText.transform;
            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.28f;
                t.localScale = originalScale * scale;
                yield return null;
            }

            t.localScale = originalScale;
        }

        public void UpdateCoins(int coins)
        {
            if (coinsText != null)
            {
                coinsText.text = $"{coins:N0} G";
            }
            CheckNextLevelReady();
        }
    }
}
