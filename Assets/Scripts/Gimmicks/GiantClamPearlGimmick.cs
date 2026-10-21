using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Economy;
using IdleFishing.UI;
using IdleFishing.VFX;

namespace IdleFishing.Gimmicks
{
    /// <summary>
    /// Cơ chế Level 2 (Đảo San Hô): Sò Khổng Lồ Săn Ngọc Trai (Giant Clam & Pearl Frenzy).
    /// - Định kỳ Sò Khổng Lồ mở vỏ hé lộ Ngọc Trai Vàng trong 12s.
    /// - Người chơi chạm vào Sò để thu thập Ngọc Trai.
    /// - Gom đủ 3 Ngọc Trai kích hoạt PEARL FRENZY: Toàn bộ cá bán ra được nhân x3 TIỀN VÀNG trong 20 giây!
    /// </summary>
    public class GiantClamPearlGimmick : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float openCooldown = 32f; // Thời gian chờ mở sò
        [SerializeField] private float openDuration = 12f; // Thời gian sò mở miệng
        [SerializeField] private float frenzyDuration = 20f; // Thời gian hiệu lực x3 tiền

        [Header("3D Transforms")]
        [SerializeField] private Transform topShellTransform;
        [SerializeField] private Transform pearlTransform;
        [SerializeField] private Light pearlLight;

        [Header("State")]
        private int currentPearls = 0;
        private bool isClamOpen = false;
        private bool isPearlFrenzyActive = false;
        private float frenzyTimer = 0f;
        private float nextOpenTimer = 15f; // Lần đầu mở sớm để người chơi trải nghiệm ngay
        private float openRemainingTimer = 0f;

        public int CurrentPearls => currentPearls;
        public int RequiredPearls => 3;
        public bool IsClamOpen => isClamOpen;
        public bool IsPearlFrenzyActive => isPearlFrenzyActive;
        public float FrenzyRemainingTime => frenzyTimer;
        public float NextOpenTimer => nextOpenTimer;

        private Quaternion closedRot = Quaternion.identity;
        private Quaternion openRot = Quaternion.Euler(-50f, 0f, 0f);
        private Vector3 pearlBaseLocalPos = new Vector3(0f, 0.12f, 0f);

        private void Awake()
        {
            if (topShellTransform != null)
            {
                closedRot = topShellTransform.localRotation;
                openRot = closedRot * Quaternion.Euler(-55f, 0f, 0f);
            }
            if (pearlTransform != null)
            {
                pearlBaseLocalPos = pearlTransform.localPosition;
            }
        }

        private void OnEnable()
        {
            nextOpenTimer = 12f;
            isClamOpen = false;
            CloseClamImmediate();
        }

        private void Update()
        {
            // 1. Quản lý trạng thái Pearl Frenzy
            if (isPearlFrenzyActive)
            {
                frenzyTimer -= Time.deltaTime;
                if (frenzyTimer <= 0f)
                {
                    EndPearlFrenzy();
                }
            }

            // 2. Quản lý đếm ngược mở Sò
            if (!isClamOpen && !isPearlFrenzyActive)
            {
                nextOpenTimer -= Time.deltaTime;
                if (nextOpenTimer <= 0f)
                {
                    StartCoroutine(OpenClamRoutine());
                }
            }
            else if (isClamOpen)
            {
                openRemainingTimer -= Time.deltaTime;
                // Hiệu ứng ngọc trai xoay tròn và nhấp nhô
                if (pearlTransform != null)
                {
                    pearlTransform.Rotate(Vector3.up * 75f * Time.deltaTime, Space.World);
                    float bob = Mathf.Sin(Time.time * 3.5f) * 0.04f;
                    pearlTransform.localPosition = pearlBaseLocalPos + new Vector3(0f, bob + 0.12f, 0f);
                }

                if (openRemainingTimer <= 0f)
                {
                    StartCoroutine(CloseClamRoutine());
                }
            }
        }

        private IEnumerator OpenClamRoutine()
        {
            // Rung lắc cảnh báo trước khi mở
            Vector3 basePos = transform.position;
            float shakeTime = 1.0f;
            float elapsed = 0f;
            while (elapsed < shakeTime)
            {
                elapsed += Time.deltaTime;
                float ox = Random.Range(-0.04f, 0.04f);
                transform.position = basePos + new Vector3(ox, 0, 0);
                yield return null;
            }
            transform.position = basePos;

            isClamOpen = true;
            openRemainingTimer = openDuration;

            if (pearlTransform != null) pearlTransform.gameObject.SetActive(true);
            if (pearlLight != null) pearlLight.enabled = true;

            // Mở nắp vỏ sò mượt mà
            float t = 0f;
            float openAnimTime = 0.6f;
            while (t < openAnimTime)
            {
                t += Time.deltaTime;
                if (topShellTransform != null)
                {
                    topShellTransform.localRotation = Quaternion.Slerp(closedRot, openRot, t / openAnimTime);
                }
                yield return null;
            }
            if (topShellTransform != null) topShellTransform.localRotation = openRot;

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 1.6f,
                    0,
                    DamageStyle.GoldBonus,
                    "[SHELL] CLAM OPENED! TAP TO COLLECT PEARL!"
                );
            }

            WorldGimmickManager.Instance?.NotifyStateChanged();
        }

        private IEnumerator CloseClamRoutine()
        {
            isClamOpen = false;
            nextOpenTimer = openCooldown;

            float t = 0f;
            float closeAnimTime = 0.35f;
            while (t < closeAnimTime)
            {
                t += Time.deltaTime;
                if (topShellTransform != null)
                {
                    topShellTransform.localRotation = Quaternion.Slerp(openRot, closedRot, t / closeAnimTime);
                }
                yield return null;
            }
            if (topShellTransform != null) topShellTransform.localRotation = closedRot;

            if (pearlTransform != null) pearlTransform.gameObject.SetActive(false);
            if (pearlLight != null) pearlLight.enabled = false;

            WorldGimmickManager.Instance?.NotifyStateChanged();
        }

        private void CloseClamImmediate()
        {
            if (topShellTransform != null) topShellTransform.localRotation = closedRot;
            if (pearlTransform != null) pearlTransform.gameObject.SetActive(false);
            if (pearlLight != null) pearlLight.enabled = false;
        }

        private void OnMouseDown()
        {
            TryCollectPearl();
        }

        public void TryCollectPearl()
        {
            if (!isClamOpen) return;

            // Thu thập ngọc thành công!
            currentPearls++;
            isClamOpen = false;
            nextOpenTimer = openCooldown;

            // Âm thanh & hiệu ứng
            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayPearlHarvestClack(transform.position);
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 2.0f,
                    0,
                    DamageStyle.GoldBonus,
                    $"[PEARL] +1 GOLDEN PEARL! ({currentPearls}/3)"
                );
            }

            // Đóng vỏ nhanh
            StartCoroutine(CloseClamRoutine());

            // Đủ 3 ngọc -> Bùng nổ Pearl Frenzy!
            if (currentPearls >= RequiredPearls)
            {
                currentPearls = 0;
                TriggerPearlFrenzy();
            }

            WorldGimmickManager.Instance?.NotifyStateChanged();
        }

        private void TriggerPearlFrenzy()
        {
            isPearlFrenzyActive = true;
            frenzyTimer = frenzyDuration;

            // Hiệu ứng pháo hoa rực rỡ
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnConfetti(transform.position + Vector3.up * 2f, 2.0f);
                VFXManager.Instance.SpawnGoldBurst(transform.position + Vector3.up * 1.5f, 40);
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    new Vector3(0f, 2.5f, 1.5f),
                    0,
                    DamageStyle.GoldBonus,
                    "[*] PEARL FRENZY! 3x FISH VALUE FOR 20S! [*]"
                );
            }

            WorldGimmickManager.Instance?.NotifyStateChanged();
            Debug.Log("[GiantClamPearlGimmick] PEARL FRENZY ACTIVATED! 3x Gold Multiplier!");
        }

        private void EndPearlFrenzy()
        {
            isPearlFrenzyActive = false;
            frenzyTimer = 0f;
            nextOpenTimer = openCooldown;
            WorldGimmickManager.Instance?.NotifyStateChanged();
            Debug.Log("[GiantClamPearlGimmick] Pearl Frenzy ended.");
        }

        public void SetupShellComponents(Transform topShell, Transform pearl, Light pLight)
        {
            topShellTransform = topShell;
            pearlTransform = pearl;
            pearlLight = pLight;

            if (topShellTransform != null)
            {
                closedRot = topShellTransform.localRotation;
                openRot = closedRot * Quaternion.Euler(-55f, 0f, 0f);
            }
            if (pearlTransform != null)
            {
                pearlBaseLocalPos = pearlTransform.localPosition;
            }
        }
    }
}
