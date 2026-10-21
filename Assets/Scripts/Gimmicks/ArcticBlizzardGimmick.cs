using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Economy;
using IdleFishing.UI;
using IdleFishing.VFX;

namespace IdleFishing.Gimmicks
{
    /// <summary>
    /// Cơ chế Level 4 (Vịnh Băng Bắc Cực): Lỗ Băng & Bão Tuyết Sinh Tồn (Blizzard Frenzy).
    /// - Định kỳ xuất hiện Bão Tuyết làm đóng băng một phần bến cảng, làm chậm cần thủ 50%.
    /// - Một Khối Băng Báu Vật xuất hiện giữa bến.
    /// - Người chơi chạm (tap) liên tục để đập vỡ khối băng: Giải cứu bến cảng, nhận Mưa Vàng và buff x2 Tốc Độ Câu!
    /// </summary>
    public class ArcticBlizzardGimmick : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float blizzardInterval = 55f; // Chu kỳ bão tuyết xuất hiện
        [SerializeField] private float blizzardDuration = 16f; // Thời gian tồn tại của đợt bão
        [SerializeField] private float rescueBuffDuration = 15f; // Thời gian buff x2 tốc độ

        [Header("Ice Block References")]
        [SerializeField] private Transform iceBlockTransform;
        [SerializeField] private int iceMaxHp = 8;
        [SerializeField] private int breakRewardCoins = 850;

        [Header("Runtime State")]
        private bool isBlizzardActive = false;
        private bool isRescueBuffActive = false;
        private float nextBlizzardTimer = 20f;
        private float blizzardRemainingTimer = 0f;
        private float rescueBuffTimer = 0f;
        private int iceCurrentHp = 8;

        public bool IsBlizzardActive => isBlizzardActive;
        public bool IsRescueBuffActive => isRescueBuffActive;
        public float NextBlizzardTimer => nextBlizzardTimer;
        public float BlizzardRemainingTimer => blizzardRemainingTimer;
        public float RescueBuffTimer => rescueBuffTimer;
        public int IceCurrentHp => iceCurrentHp;
        public int IceMaxHp => iceMaxHp;

        public float FishingSpeedMultiplier
        {
            get
            {
                if (isRescueBuffActive) return 2.0f; // Buff x2 sau khi phá băng
                if (isBlizzardActive) return 0.5f;   // Giảm 50% khi bão tuyết hoành hành
                return 1.0f;
            }
        }

        private Vector3 iceOriginalScale = Vector3.one;

        private void Awake()
        {
            if (iceBlockTransform != null)
            {
                iceOriginalScale = iceBlockTransform.localScale;
            }
        }

        private void OnEnable()
        {
            nextBlizzardTimer = 18f;
            isBlizzardActive = false;
            isRescueBuffActive = false;
            if (iceBlockTransform != null) iceBlockTransform.gameObject.SetActive(false);
        }

        private void Update()
        {
            // 1. Quản lý Rescue Buff sau khi phá băng
            if (isRescueBuffActive)
            {
                rescueBuffTimer -= Time.deltaTime;
                if (rescueBuffTimer <= 0f)
                {
                    isRescueBuffActive = false;
                    WorldGimmickManager.Instance?.NotifyStateChanged();
                }
            }

            // 2. Quản lý chu kỳ Bão Tuyết
            if (!isBlizzardActive)
            {
                nextBlizzardTimer -= Time.deltaTime;
                if (nextBlizzardTimer <= 0f)
                {
                    StartBlizzard();
                }
            }
            else
            {
                blizzardRemainingTimer -= Time.deltaTime;
                if (blizzardRemainingTimer <= 0f)
                {
                    EndBlizzardWithoutBreak();
                }
            }
        }

        private void StartBlizzard()
        {
            isBlizzardActive = true;
            blizzardRemainingTimer = blizzardDuration;
            iceCurrentHp = iceMaxHp;

            if (iceBlockTransform != null)
            {
                iceBlockTransform.gameObject.SetActive(true);
                iceBlockTransform.localScale = iceOriginalScale;
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    new Vector3(0f, 2.5f, 1.5f),
                    0,
                    DamageStyle.GoldBonus,
                    "[BLIZZARD] ARCTIC BLIZZARD! BREAK ICE TO RESCUE DOCK!"
                );
            }

            WorldGimmickManager.Instance?.NotifyStateChanged();
            Debug.Log("[ArcticBlizzardGimmick] Blizzard started! Ice block spawned!");
        }

        private void OnMouseDown()
        {
            TapBreakIce();
        }

        public void TapBreakIce()
        {
            if (!isBlizzardActive) return;

            iceCurrentHp--;

            // Juice: Rung nảy khối băng
            StartCoroutine(IceShakeRoutine());

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 1.8f,
                    0,
                    DamageStyle.FishAttack,
                    $"[ICE] BREAK ICE! ({iceCurrentHp}/{iceMaxHp})"
                );
            }

            if (iceCurrentHp <= 0)
            {
                BreakIceSuccess();
            }

            WorldGimmickManager.Instance?.NotifyStateChanged();
        }

        private void BreakIceSuccess()
        {
            isBlizzardActive = false;
            isRescueBuffActive = true;
            rescueBuffTimer = rescueBuffDuration;
            nextBlizzardTimer = blizzardInterval;

            if (iceBlockTransform != null)
            {
                iceBlockTransform.gameObject.SetActive(false);
            }

            // Thưởng vàng
            float valueMult = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetValue(Data.UpgradeType.FishValue) : 1.0f;
            int finalReward = Mathf.RoundToInt(breakRewardCoins * valueMult);

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(finalReward);
            }

            // Hiệu ứng pháo hoa rực rỡ & nổ vàng
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnConfetti(transform.position + Vector3.up * 2f, 2.5f);
                VFXManager.Instance.SpawnGoldBurst(transform.position + Vector3.up * 1.5f, 50);
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 2.4f,
                    finalReward,
                    DamageStyle.GoldBonus,
                    $"[*] RESCUE SUCCESS! +{NumberFormatter.Format(finalReward)} G & 2x FISHING SPEED!"
                );
            }

            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayIceCrack(transform.position);
            }

            WorldGimmickManager.Instance?.NotifyStateChanged();
            Debug.Log($"[ArcticBlizzardGimmick] Ice shattered! Rescued port with {finalReward} G!");
        }

        private void EndBlizzardWithoutBreak()
        {
            isBlizzardActive = false;
            nextBlizzardTimer = blizzardInterval;
            if (iceBlockTransform != null) iceBlockTransform.gameObject.SetActive(false);
            WorldGimmickManager.Instance?.NotifyStateChanged();
            Debug.Log("[ArcticBlizzardGimmick] Blizzard ended without break.");
        }

        private IEnumerator IceShakeRoutine()
        {
            if (iceBlockTransform == null) yield break;

            Vector3 basePos = iceBlockTransform.localPosition;
            float dur = 0.15f;
            float el = 0f;
            while (el < dur)
            {
                el += Time.deltaTime;
                float ox = Random.Range(-0.06f, 0.06f);
                float oz = Random.Range(-0.06f, 0.06f);
                iceBlockTransform.localPosition = basePos + new Vector3(ox, 0, oz);
                yield return null;
            }
            iceBlockTransform.localPosition = basePos;
        }

        public void SetupVisuals(Transform iceTransform)
        {
            iceBlockTransform = iceTransform;
            if (iceBlockTransform != null) iceOriginalScale = iceBlockTransform.localScale;
        }
    }
}
