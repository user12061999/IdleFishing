using System;
using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.UI;

namespace IdleFishing.Fishing
{
    public class SunkenTreasureChest : MonoBehaviour
    {
        public static int ActiveChestCount { get; private set; } = 0;

        [Header("State")]
        [SerializeField] private int requiredTaps = 5;
        [SerializeField] private int currentTaps = 0;
        [SerializeField] private bool isOpened = false;

        [Header("Visual Transform")]
        [SerializeField] private Transform visualRoot;

        private FloatingHealthBarUI tapBarUI;
        private Vector3 groundPos;
        private bool countTracked = false;

        public bool IsOpened => isOpened;

        public void Initialize(Vector3 landingPos, int tapsToOpen = 5)
        {
            groundPos = landingPos;
            requiredTaps = tapsToOpen;
            currentTaps = 0;
            isOpened = false;

            if (!countTracked)
            {
                ActiveChestCount++;
                countTracked = true;
            }

            // Setup Floating Badge
            GameObject barObj = new GameObject("Chest_TapBar");
            tapBarUI = barObj.AddComponent<FloatingHealthBarUI>();
            tapBarUI.Setup(transform, "TREASURE CHEST", new Vector3(0, 1.4f, 0), new Color(1f, 0.85f, 0.2f));
            tapBarUI.UpdateHealth(requiredTaps - currentTaps, requiredTaps);

            // Arc flight to ground
            StartCoroutine(LandingRoutine(landingPos));
            StartCoroutine(AutoOpenRoutine(25f));
        }

        public SunkenTreasureChest SetVisualRoot(Transform root)
        {
            visualRoot = root;
            return this;
        }

        private IEnumerator LandingRoutine(Vector3 target)
        {
            Vector3 start = transform.position;
            float duration = 0.75f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 current = Vector3.Lerp(start, target, t);
                float arc = Mathf.Sin(t * Mathf.PI) * 2.0f;
                current.y += arc;

                transform.position = current;
                transform.Rotate(Vector3.up, 360f * Time.deltaTime * 1.5f);
                yield return null;
            }

            transform.position = target;
            transform.rotation = Quaternion.identity;
        }

        public void OnMouseDown()
        {
            if (isOpened) return;
            TapChest(isPlayer: true);
        }

        public void TapChest(bool isPlayer = true)
        {
            if (isOpened) return;

            currentTaps++;
            int remaining = Mathf.Max(0, requiredTaps - currentTaps);

            if (tapBarUI != null)
            {
                tapBarUI.UpdateHealth(remaining, requiredTaps);
            }

            // Visual Juice: Squash & Stretch
            StartCoroutine(SquashReaction());

            // Floating feedback text
            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 1.0f,
                    remaining,
                    DamageStyle.PlayerTap
                );
            }

            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayWhack(transform.position);
            }
            if (IdleFishing.VFX.VFXManager.Instance != null)
            {
                IdleFishing.VFX.VFXManager.Instance.SpawnHitImpact(transform.position + Vector3.up * 0.8f);
            }

            // Frenzy micro-boost
            if (FishingFrenzyManager.Instance != null)
            {
                FishingFrenzyManager.Instance.AddCharge(2.0f);
            }

            if (currentTaps >= requiredTaps)
            {
                OpenChest();
            }
        }

        private IEnumerator SquashReaction()
        {
            if (visualRoot == null) yield break;

            Vector3 origScale = Vector3.one;
            visualRoot.localScale = new Vector3(origScale.x * 1.25f, origScale.y * 0.7f, origScale.z * 1.25f);
            yield return new WaitForSeconds(0.08f);
            visualRoot.localScale = origScale;
        }

        private void OpenChest()
        {
            isOpened = true;

            int minGold = 250;
            int maxGold = 500;
            if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
            {
                minGold = GameManager.Instance.BalanceConfig.treasureChestMinGold;
                maxGold = GameManager.Instance.BalanceConfig.treasureChestMaxGold;
            }

            int rewardGold = UnityEngine.Random.Range(minGold, maxGold + 1);

            // Reward Gold
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(rewardGold);
            }

            // Flying Coins to HUD
            if (FlyingCoinManager.Instance != null)
            {
                FlyingCoinManager.Instance.PlayFlyingCoins(transform.position + Vector3.up * 1.2f, 8);
            }

            // Floating Gold Popup
            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 1.6f,
                    rewardGold,
                    DamageStyle.GoldBonus
                );
            }

            GameEvents.TriggerTreasureChestOpened(rewardGold);
            Debug.Log($"[SunkenTreasureChest] Treasure chest unlocked! Rewarded {rewardGold} Gold and charged Frenzy!");

            if (tapBarUI != null)
            {
                Destroy(tapBarUI.gameObject);
                tapBarUI = null;
            }

            StartCoroutine(OpenBurstRoutine());
        }

        private IEnumerator AutoOpenRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!isOpened && this != null)
            {
                OpenChest();
            }
        }

        private IEnumerator OpenBurstRoutine()
        {
            float elapsed = 0f;
            Vector3 startScale = visualRoot != null ? visualRoot.localScale : Vector3.one;

            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.6f;
                if (visualRoot != null)
                {
                    visualRoot.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                    transform.position += Vector3.up * Time.deltaTime * 0.8f;
                }
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (countTracked)
            {
                ActiveChestCount = Mathf.Max(0, ActiveChestCount - 1);
                countTracked = false;
            }

            if (tapBarUI != null)
            {
                Destroy(tapBarUI.gameObject);
                tapBarUI = null;
            }
        }
    }
}
