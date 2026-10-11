using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Economy;
using IdleFishing.UI;

namespace IdleFishing.World
{
    public enum DayPhase
    {
        Dawn,   // 05:00 - 08:00
        Day,    // 08:00 - 17:00
        Sunset, // 17:00 - 20:00
        Night   // 20:00 - 05:00
    }

    public class DayNightCycleManager : MonoBehaviour
    {
        public static DayNightCycleManager Instance { get; private set; }

        [Header("Time Simulation")]
        [Range(0f, 24f)]
        [SerializeField] private float timeOfDay = 12f; // Bắt đầu ở 12:00 trưa
        [SerializeField] private float dayDurationSeconds = 180f; // 1 chu kỳ ngày đêm = 3 phút
        [SerializeField] private bool autoAdvanceTime = true;

        [Header("Lighting References")]
        [SerializeField] private Light sunLight;
        [SerializeField] private List<Light> streetLamps = new List<Light>();
        [SerializeField] private List<Renderer> streetLanterns = new List<Renderer>();
        [SerializeField] private Light tankNeonLight;
        [SerializeField] private LighthouseController lighthouse;

        [Header("Phase State")]
        [SerializeField] private DayPhase currentPhase = DayPhase.Day;
        [SerializeField] private bool isNight = false;

        [Header("UI Widget")]
        [SerializeField] private TextMeshProUGUI clockText;
        [SerializeField] private Image phaseIconImage;
        [SerializeField] private Button timeToggleButton;

        public float TimeOfDay => timeOfDay;
        public DayPhase CurrentPhase => currentPhase;
        public bool IsNight => isNight;

        public event Action<DayPhase> OnPhaseChanged;
        public event Action<bool> OnNightToggled; // isNight

        private float phaseCheckTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            FindSceneLighting();
            CreateUIClockWidget();
            ApplyLighting(immediate: true);
        }

        public void FindSceneLighting()
        {
            // 1. Sun / Moon Directional Light
            if (sunLight == null)
            {
                var lightObj = GameObject.Find("Directional Light");
                if (lightObj != null)
                {
                    sunLight = lightObj.GetComponent<Light>();
                }
            }

            // 2. Street Lamps
            streetLamps.Clear();
            streetLanterns.Clear();
            var lamps = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in lamps)
            {
                if (t.name.StartsWith("Lamp_"))
                {
                    var l = t.GetComponentInChildren<Light>();
                    if (l != null && !streetLamps.Contains(l))
                    {
                        streetLamps.Add(l);
                    }
                    var r = t.Find("Lantern")?.GetComponent<Renderer>();
                    if (r != null && !streetLanterns.Contains(r))
                    {
                        streetLanterns.Add(r);
                    }
                }
            }

            // 3. Tank Neon Light
            if (tankNeonLight == null)
            {
                var tObj = GameObject.Find("Tank_NeonLight");
                if (tObj != null) tankNeonLight = tObj.GetComponent<Light>();
            }

            // 4. Lighthouse
            if (lighthouse == null)
            {
                lighthouse = FindFirstObjectByType<LighthouseController>();
            }
        }

        private void Update()
        {
            if (autoAdvanceTime)
            {
                float hourSpeed = 24f / Mathf.Max(10f, dayDurationSeconds);
                timeOfDay = (timeOfDay + hourSpeed * Time.deltaTime) % 24f;
            }

            ApplyLighting(immediate: false);
            UpdateClockUI();

            phaseCheckTimer += Time.deltaTime;
            if (phaseCheckTimer >= 0.5f)
            {
                phaseCheckTimer = 0f;
                CheckPhaseTransition();
            }
        }

        private void CheckPhaseTransition()
        {
            DayPhase newPhase;
            if (timeOfDay >= 5f && timeOfDay < 8f) newPhase = DayPhase.Dawn;
            else if (timeOfDay >= 8f && timeOfDay < 17f) newPhase = DayPhase.Day;
            else if (timeOfDay >= 17f && timeOfDay < 20f) newPhase = DayPhase.Sunset;
            else newPhase = DayPhase.Night;

            bool newIsNight = (newPhase == DayPhase.Night || newPhase == DayPhase.Sunset && timeOfDay >= 18.5f || newPhase == DayPhase.Dawn && timeOfDay < 6.0f);

            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                OnPhaseChanged?.Invoke(currentPhase);
                Debug.Log($"[DayNightCycle] Transition to phase: {currentPhase} (At {timeOfDay:00.0}h)");
            }

            if (newIsNight != isNight)
            {
                isNight = newIsNight;
                OnNightToggled?.Invoke(isNight);
                ApplyNightState(isNight);
            }
        }

        public void SetTimeOfDay(float hour)
        {
            timeOfDay = Mathf.Clamp(hour, 0f, 24f) % 24f;
            CheckPhaseTransition();
            ApplyLighting(immediate: true);
            UpdateClockUI();
        }

        public void ToggleDayNight()
        {
            if (isNight)
            {
                SetTimeOfDay(12f); // Chuyển sang giữa trưa
            }
            else
            {
                SetTimeOfDay(21f); // Chuyển sang ban đêm
            }
        }

        private void ApplyLighting(bool immediate)
        {
            if (sunLight == null) return;

            // Tính toán màu sắc, góc nghiêng và cường độ ánh sáng mặt trời/mặt trăng
            Vector3 targetRot;
            Color targetLightColor;
            float targetIntensity;
            Color targetAmbient;

            if (timeOfDay >= 5f && timeOfDay < 8f)
            {
                // Rạng đông (Dawn: 5h - 8h)
                float t = (timeOfDay - 5f) / 3f;
                targetRot = Vector3.Lerp(new Vector3(10f, 60f, 0f), new Vector3(50f, -30f, 0f), t);
                targetLightColor = Color.Lerp(new Color(1f, 0.75f, 0.55f), new Color(1f, 0.98f, 0.92f), t);
                targetIntensity = Mathf.Lerp(0.6f, 1.15f, t);
                targetAmbient = Color.Lerp(new Color(0.25f, 0.3f, 0.45f), new Color(0.55f, 0.60f, 0.68f), t);
            }
            else if (timeOfDay >= 8f && timeOfDay < 17f)
            {
                // Ban ngày (Day: 8h - 17h)
                float t = (timeOfDay - 8f) / 9f;
                targetRot = Vector3.Lerp(new Vector3(50f, -30f, 0f), new Vector3(45f, -50f, 0f), t);
                targetLightColor = new Color(1f, 0.98f, 0.92f);
                targetIntensity = 1.15f;
                targetAmbient = new Color(0.55f, 0.60f, 0.68f);
            }
            else if (timeOfDay >= 17f && timeOfDay < 20f)
            {
                // Hoàng hôn (Sunset: 17h - 20h)
                float t = (timeOfDay - 17f) / 3f;
                targetRot = Vector3.Lerp(new Vector3(45f, -50f, 0f), new Vector3(15f, -80f, 0f), t);
                targetLightColor = Color.Lerp(new Color(1f, 0.75f, 0.45f), new Color(1f, 0.45f, 0.22f), t);
                targetIntensity = Mathf.Lerp(1.15f, 0.45f, t);
                targetAmbient = Color.Lerp(new Color(0.55f, 0.60f, 0.68f), new Color(0.48f, 0.32f, 0.40f), t);
            }
            else
            {
                // Ban đêm (Night: 20h - 5h)
                targetRot = new Vector3(42f, 110f, 0f); // Ánh trăng chiếu chếch
                targetLightColor = new Color(0.32f, 0.52f, 0.88f); // Ánh trăng xanh lam dịu
                targetIntensity = 0.32f;
                targetAmbient = new Color(0.14f, 0.18f, 0.32f);
            }

            float lerpFactor = immediate ? 1f : Time.deltaTime * 3.5f;
            sunLight.transform.rotation = Quaternion.Slerp(sunLight.transform.rotation, Quaternion.Euler(targetRot), lerpFactor);
            sunLight.color = Color.Lerp(sunLight.color, targetLightColor, lerpFactor);
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetIntensity, lerpFactor);
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, lerpFactor);
        }

        private void ApplyNightState(bool night)
        {
            // 1. Street Lamps
            foreach (var lamp in streetLamps)
            {
                if (lamp != null)
                {
                    lamp.enabled = night;
                    lamp.intensity = night ? 2.2f : 0f;
                }
            }

            // 2. Street Lantern Emissive Glow
            foreach (var r in streetLanterns)
            {
                if (r != null && r.material != null)
                {
                    if (night)
                    {
                        r.material.EnableKeyword("_EMISSION");
                        if (r.material.HasProperty("_EmissionColor"))
                        {
                            r.material.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.4f) * 2.2f);
                        }
                    }
                    else
                    {
                        r.material.DisableKeyword("_EMISSION");
                        if (r.material.HasProperty("_EmissionColor"))
                        {
                            r.material.SetColor("_EmissionColor", Color.black);
                        }
                    }
                }
            }

            // 3. Tank Neon Light
            if (tankNeonLight != null)
            {
                tankNeonLight.intensity = night ? 3.2f : 1.5f;
            }

            // 4. Night Market Announcement
            if (night)
            {
                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        new Vector3(0f, 2.5f, 0f),
                        0,
                        DamageStyle.GoldBonus,
                        "HARBOR NIGHT MARKET: RARE FISH & +15% PRICES!"
                    );
                }
            }
        }

        // ==================== NIGHT GAMEPLAY BUFFS ====================
        public float GetNightPriceMultiplier()
        {
            // Ban đêm tăng +15% giá bán cá
            return isNight ? 1.15f : 1.0f;
        }

        public float GetNightRareCatchMultiplier()
        {
            // Ban đêm tăng +30% tỷ lệ bắt cá đêm / quý hiếm
            return isNight ? 1.30f : 1.0f;
        }

        // ==================== UI CLOCK WIDGET ====================
        private void CreateUIClockWidget()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject clockRoot = new GameObject("UI_DayNightClock");
            clockRoot.transform.SetParent(canvas.transform, false);

            var rt = clockRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -125f);
            rt.sizeDelta = new Vector2(170f, 36f);

            var bgImg = clockRoot.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.14f, 0.22f, 0.85f);

            // Nút bấm chuyển đổi ngày / đêm
            timeToggleButton = clockRoot.AddComponent<Button>();
            timeToggleButton.onClick.AddListener(ToggleDayNight);

            // Text hiển thị giờ
            GameObject txtObj = new GameObject("TimeText");
            txtObj.transform.SetParent(clockRoot.transform, false);
            clockText = txtObj.AddComponent<TextMeshProUGUI>();
            clockText.fontSize = 17;
            clockText.fontStyle = FontStyles.Bold;
            clockText.alignment = TextAlignmentOptions.Center;
            clockText.color = new Color(1f, 0.95f, 0.8f);

            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(8f, 0f);
            txtRt.offsetMax = new Vector2(-8f, 0f);

            UpdateClockUI();
        }

        private void UpdateClockUI()
        {
            if (clockText == null) return;

            int hours = Mathf.FloorToInt(timeOfDay);
            int minutes = Mathf.FloorToInt((timeOfDay - hours) * 60f);

            string phaseName = isNight ? "Night" : (currentPhase == DayPhase.Sunset ? "Sunset" : (currentPhase == DayPhase.Dawn ? "Dawn" : "Day"));
            clockText.text = $"{hours:00}:{minutes:00} <size=13>({phaseName})</size>";
        }
    }
}
