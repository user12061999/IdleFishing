using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Economy;
using IdleFishing.UI;
using IdleFishing.VFX;

namespace IdleFishing.Gimmicks
{
    /// <summary>
    /// Cơ chế Level 3 (Vịnh Rừng Phong): Lò Nướng Đặc Sản Bến Cảng (Autumn Smoked Fish BBQ Station).
    /// - Tự động nướng mẻ cá mùa thu mỗi 14 giây.
    /// - Players tap repeatedly on the smoker to fan the flames and speed up cooking.
    /// - Khi nướng chín: Bán ngay cho khách du lịch ngắm lá phong với giá x4 TIỀN VÀNG!
    /// </summary>
    public class AutumnSmokerBBQGimmick : MonoBehaviour
    {
        [Header("Grill Tuning")]
        [SerializeField] private float baseBakeDuration = 12f; // Thời gian nướng bình thường
        [SerializeField] private float tapBoostSeconds = 3.0f;  // Mỗi cú tap rút ngắn 3s
        [SerializeField] private int baseBakeReward = 320;      // Vàng cơ bản mỗi mẻ cá nướng (x4)

        [Header("Visual References")]
        [SerializeField] private Transform grillVisualRoot;
        [SerializeField] private Light fireLight;

        [Header("Runtime State")]
        private float currentProgress = 0f; // 0.0 -> 1.0
        private int totalBakesCompleted = 0;
        private bool isBaking = true;

        public float CurrentProgress => currentProgress;
        public int TotalBakesCompleted => totalBakesCompleted;

        private Vector3 originalScale = Vector3.one;

        private void Awake()
        {
            if (grillVisualRoot != null)
            {
                originalScale = grillVisualRoot.localScale;
            }
        }

        private void OnEnable()
        {
            currentProgress = 0f;
            isBaking = true;
        }

        private void Update()
        {
            if (isBaking)
            {
                currentProgress += Time.deltaTime / baseBakeDuration;

                // Than hồng lập lòe
                if (fireLight != null)
                {
                    fireLight.intensity = 1.4f + Mathf.Sin(Time.time * 8f) * 0.4f;
                }

                if (currentProgress >= 1.0f)
                {
                    CompleteBake();
                }
            }
        }

        private void OnMouseDown()
        {
            TapStokeFire();
        }

        /// <summary>
        /// Người chơi tap vào lò nướng để quạt than tăng tốc
        /// </summary>
        public void TapStokeFire()
        {
            if (!isBaking) return;

            // Rút ngắn thời gian nướng
            currentProgress += tapBoostSeconds / baseBakeDuration;
            currentProgress = Mathf.Min(0.99f, currentProgress);

            // Juice: Spring bounce animation
            StartCoroutine(BounceRoutine());

            // Âm thanh & tia lửa than hồng
            if (fireLight != null)
            {
                fireLight.intensity = 3.5f;
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 1.6f,
                    0,
                    DamageStyle.GoldBonus,
                    "[FIRE] FAN FLAMES! (+30% COOK)"
                );
            }

            WorldGimmickManager.Instance?.NotifyStateChanged();
        }

        private void CompleteBake()
        {
            currentProgress = 0f;
            totalBakesCompleted++;

            // Tính tiền thưởng (quy đổi theo kinh tế vùng)
            float valueMult = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetValue(Data.UpgradeType.FishValue) : 1.0f;
            int finalReward = Mathf.RoundToInt(baseBakeReward * valueMult);

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(finalReward);
            }

            // Hiệu ứng pháo hoa mini & nổ tiền
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnGoldBurst(transform.position + Vector3.up * 1.5f, 25);
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 2.2f,
                    finalReward,
                    DamageStyle.GoldBonus,
                    $"[BBQ] SMOKED AUTUMN FISH! +{NumberFormatter.Format(finalReward)} G (4x Value)"
                );
            }

            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayBBQSizzle(transform.position);
            }

            WorldGimmickManager.Instance?.NotifyStateChanged();
            Debug.Log($"[AutumnSmokerBBQGimmick] Baked delicious smoked fish! Earned {finalReward} G!");
        }

        private IEnumerator BounceRoutine()
        {
            if (grillVisualRoot == null) yield break;

            float elapsed = 0f;
            float dur = 0.18f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                float punch = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
                grillVisualRoot.localScale = originalScale * punch;
                yield return null;
            }
            grillVisualRoot.localScale = originalScale;
        }

        public void SetupVisuals(Transform visualRoot, Light lightComp)
        {
            grillVisualRoot = visualRoot;
            fireLight = lightComp;
            if (grillVisualRoot != null) originalScale = grillVisualRoot.localScale;
        }
    }
}
