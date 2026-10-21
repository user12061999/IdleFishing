using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.World;

namespace IdleFishing.Audio
{
    /// <summary>
    /// Quản lý Âm Thanh & Nhạc Nền Độc Quyền Từng Đảo (Island Audio System):
    /// - Nhạc nền (BGM) riêng biệt, mang đậm bản sắc cho 5 Vùng Đất:
    ///   • Level 1 (Temperate Bay): Giai điệu mộc mạc thư giãn, guitar acoustic & marimba.
    ///   • Level 2 (Tropical Coral Atoll): Giai điệu Calypso nhiệt đới vui nhộn, tiếng trống thép đảo Hawaii.
    ///   • Level 3 (Golden Autumn Cove): Nhạc đồng quê ấm áp, tiếng đàn dân dã & gõ gỗ mùa thu.
    ///   • Level 4 (Arctic Frost Haven): Giai điệu tuyết trắng huyền bí, tiếng chuông celesta & gió bão cực.
    ///   • Level 5 (Sunken Oasis Desert): Giai điệu Ả Rập Hijaz kỳ bí, tiếng đàn Oud & đoàn lạc đà sa mạc.
    /// - Âm thanh môi trường (Island Ambience) và Hiệu ứng chuyển cảnh Crossfade mượt mà không ngắt quãng.
    /// - Tích hợp chu kỳ Ngày/Đêm (Day/Night) tự động hòa âm dế đêm & sóng biển êm đềm khi màn đêm buông xuống.
    /// - Âm thanh đặc quyền cho từng cơ chế Gimmick (Vỏ sò ngọc trai, Lò BBQ xèo xèo, Đập vỡ tảng băng, Chuông lạc đà).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string PREF_BGM_VOL = "IdleFishing_BGM_Volume";
        private const string PREF_SFX_VOL = "IdleFishing_SFX_Volume";
        private const string PREF_AMBIENT_VOL = "IdleFishing_Ambient_Volume";
        private const string PREF_MUTED = "IdleFishing_Muted";

        [Header("Audio Sources")]
        private AudioSource sfxSource;
        private AudioSource bgmSourceA;
        private AudioSource bgmSourceB;
        private AudioSource ambientSource;
        private AudioSource ambientNightSource;

        [Header("Volume Controls")]
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.45f;
        [Range(0f, 1f)] [SerializeField] private float ambientVolume = 0.28f;
        [SerializeField] private bool isMuted = false;

        // Active State
        private bool isUsingSourceA = true;
        private Coroutine activeCrossfadeRoutine;
        private int currentWorldOrder = 1;
        private bool isNightActive = false;

        // Core SFX Clips
        [Header("SFX Clips")]
        [SerializeField] private AudioClip splashClip;
        [SerializeField] private AudioClip whackClip;
        [SerializeField] private AudioClip sliceClip;
        [SerializeField] private AudioClip coinClip;
        [SerializeField] private AudioClip chestOpenClip;
        [SerializeField] private AudioClip frenzyStartClip;
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip bubblesClip;
        [SerializeField] private AudioClip stunClip;
        [SerializeField] private AudioClip confettiClip;
        [SerializeField] private AudioClip heartClip;
        [SerializeField] private AudioClip rodCastWhooshClip;
        [SerializeField] private AudioClip reelCrankClip;
        [SerializeField] private AudioClip waterPlopClip;
        [SerializeField] private AudioClip fishDockDropClip;
        [SerializeField] private AudioClip wranglerCritWhackClip;
        [SerializeField] private AudioClip contractFanfareClip;
        [SerializeField] private AudioClip shipHornClip;

        // Island Signature Gimmick SFX
        [Header("Island Signature SFX")]
        [SerializeField] private AudioClip pearlHarvestClackClip;
        [SerializeField] private AudioClip bbqSizzleClip;
        [SerializeField] private AudioClip iceCrackClip;
        [SerializeField] private AudioClip caravanBellsClip;

        // Island BGM Clips (Level 1 to 5)
        [Header("Island BGM Themes")]
        [SerializeField] private AudioClip bgmLevel1Temperate;
        [SerializeField] private AudioClip bgmLevel2Coral;
        [SerializeField] private AudioClip bgmLevel3Autumn;
        [SerializeField] private AudioClip bgmLevel4Arctic;
        [SerializeField] private AudioClip bgmLevel5Desert;

        // Island Ambient Clips (Level 1 to 5)
        [Header("Island Ambience Themes")]
        [SerializeField] private AudioClip ambientLevel1Temperate;
        [SerializeField] private AudioClip ambientLevel2Coral;
        [SerializeField] private AudioClip ambientLevel3Autumn;
        [SerializeField] private AudioClip ambientLevel4Arctic;
        [SerializeField] private AudioClip ambientLevel5Desert;
        [SerializeField] private AudioClip ambientNightLayer;

        public float SFXVolume => sfxVolume;
        public float BGMVolume => bgmVolume;
        public float AmbientVolume => ambientVolume;
        public bool IsMuted => isMuted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            LoadVolumePreferences();
            InitializeAudioSources();
            LoadOrGenerateAudioClips();
        }

        private void Start()
        {
            RegisterEvents();

            // Khởi động nhạc nền & âm thanh môi trường theo thế giới hiện tại
            int order = 1;
            WorldData currentWorld = null;
            if (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null)
            {
                order = WorldManager.Instance.CurrentWorld.worldOrder;
                currentWorld = WorldManager.Instance.CurrentWorld;
            }

            PlayIslandThemeImmediate(order, currentWorld);
        }

        private void OnDestroy()
        {
            UnregisterEvents();
        }

        private void LoadVolumePreferences()
        {
            bgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOL, 0.45f);
            sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.85f);
            ambientVolume = PlayerPrefs.GetFloat(PREF_AMBIENT_VOL, 0.28f);
            isMuted = PlayerPrefs.GetInt(PREF_MUTED, 0) == 1;
        }

        private void SaveVolumePreferences()
        {
            PlayerPrefs.SetFloat(PREF_BGM_VOL, bgmVolume);
            PlayerPrefs.SetFloat(PREF_SFX_VOL, sfxVolume);
            PlayerPrefs.SetFloat(PREF_AMBIENT_VOL, ambientVolume);
            PlayerPrefs.SetInt(PREF_MUTED, isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void InitializeAudioSources()
        {
            // SFX Source
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            // BGM Dual Sources (For Crossfading)
            bgmSourceA = gameObject.AddComponent<AudioSource>();
            bgmSourceA.playOnAwake = false;
            bgmSourceA.loop = true;
            bgmSourceA.spatialBlend = 0f;
            bgmSourceA.volume = isMuted ? 0f : bgmVolume;

            bgmSourceB = gameObject.AddComponent<AudioSource>();
            bgmSourceB.playOnAwake = false;
            bgmSourceB.loop = true;
            bgmSourceB.spatialBlend = 0f;
            bgmSourceB.volume = 0f;

            // Ambient Day Source
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
            ambientSource.volume = isMuted ? 0f : ambientVolume;

            // Ambient Night Layer Source
            ambientNightSource = gameObject.AddComponent<AudioSource>();
            ambientNightSource.playOnAwake = false;
            ambientNightSource.loop = true;
            ambientNightSource.spatialBlend = 0f;
            ambientNightSource.volume = 0f;
        }

        private void RegisterEvents()
        {
            GameEvents.WorldChanged += OnWorldChanged;
            GameEvents.FrenzyStarted += OnFrenzyStarted;
            GameEvents.TreasureChestOpened += OnChestOpened;
            GameEvents.BossFishSpawned += OnBossSpawned;
            GameEvents.PrestigeCompleted += OnPrestigeCompleted;

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.OnNightToggled += OnNightToggled;
            }
        }

        private void UnregisterEvents()
        {
            GameEvents.WorldChanged -= OnWorldChanged;
            GameEvents.FrenzyStarted -= OnFrenzyStarted;
            GameEvents.TreasureChestOpened -= OnChestOpened;
            GameEvents.BossFishSpawned -= OnBossSpawned;
            GameEvents.PrestigeCompleted -= OnPrestigeCompleted;

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.OnNightToggled -= OnNightToggled;
            }
        }

        private void LoadOrGenerateAudioClips()
        {
#if UNITY_EDITOR
            // Tải các SFX đã có sẵn trong Project
            if (splashClip == null) splashClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_water_splash.wav");
            if (whackClip == null) whackClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_fish_hit.wav");
            if (sliceClip == null) sliceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_slice.wav");
            if (coinClip == null) coinClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_coin_collect.wav");
            if (chestOpenClip == null) chestOpenClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_level_up.wav");
            if (uiClickClip == null) uiClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_ui_click.wav");
            if (bubblesClip == null) bubblesClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_bubbles.wav");
            if (stunClip == null) stunClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_stun.wav");
            if (confettiClip == null) confettiClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_confetti.wav");
            if (heartClip == null) heartClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_heart.wav");
            if (rodCastWhooshClip == null) rodCastWhooshClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_rod_cast_whoosh.wav");
            if (reelCrankClip == null) reelCrankClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_reel_crank.wav");
            if (waterPlopClip == null) waterPlopClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_water_plop.wav");
            if (fishDockDropClip == null) fishDockDropClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_fish_dock_drop.wav");
            if (wranglerCritWhackClip == null) wranglerCritWhackClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_wrangler_crit_whack.wav");
            if (contractFanfareClip == null) contractFanfareClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_contract_fanfare.wav");
#endif

            // Fallback SFX Procedural
            if (splashClip == null) splashClip = CreateSplashSound();
            if (whackClip == null) whackClip = CreateWhackSound();
            if (sliceClip == null) sliceClip = CreateSliceSound();
            if (coinClip == null) coinClip = CreateCoinSound();
            if (chestOpenClip == null) chestOpenClip = CreateChestOpenSound();
            if (frenzyStartClip == null) frenzyStartClip = CreateFrenzyStartSound();
            if (uiClickClip == null) uiClickClip = CreateUIClickSound();
            if (shipHornClip == null) shipHornClip = CreateShipHornSound();

            // Island Gimmick Signature SFX Procedural
            if (pearlHarvestClackClip == null) pearlHarvestClackClip = CreatePearlHarvestClackSound();
            if (bbqSizzleClip == null) bbqSizzleClip = CreateBBQSizzleSound();
            if (iceCrackClip == null) iceCrackClip = CreateIceCrackSound();
            if (caravanBellsClip == null) caravanBellsClip = CreateCaravanBellsSound();

            // Island BGM Themes (Level 1 to 5)
            if (bgmLevel1Temperate == null) bgmLevel1Temperate = CreateTemperateBayBGM();
            if (bgmLevel2Coral == null) bgmLevel2Coral = CreateTropicalCoralAtollBGM();
            if (bgmLevel3Autumn == null) bgmLevel3Autumn = CreateAutumnCoveBGM();
            if (bgmLevel4Arctic == null) bgmLevel4Arctic = CreateArcticFrostHavenBGM();
            if (bgmLevel5Desert == null) bgmLevel5Desert = CreateDesertOasisBGM();

            // Island Ambience Themes (Level 1 to 5)
            if (ambientLevel1Temperate == null) ambientLevel1Temperate = CreateTemperateBayAmbience();
            if (ambientLevel2Coral == null) ambientLevel2Coral = CreateTropicalAmbience();
            if (ambientLevel3Autumn == null) ambientLevel3Autumn = CreateAutumnAmbience();
            if (ambientLevel4Arctic == null) ambientLevel4Arctic = CreateArcticAmbience();
            if (ambientLevel5Desert == null) ambientLevel5Desert = CreateDesertAmbience();
            if (ambientNightLayer == null) ambientNightLayer = CreateNightAmbience();
        }

        #region Island Theme Switcher & Crossfading
        private void OnWorldChanged(WorldData newWorld)
        {
            if (newWorld == null) return;
            CrossfadeToIslandTheme(newWorld.worldOrder, newWorld, 1.2f);
        }

        public void CrossfadeToIslandTheme(int worldOrder, WorldData worldData = null, float duration = 1.2f)
        {
            currentWorldOrder = Mathf.Clamp(worldOrder, 1, 5);

            AudioClip targetBgm = GetBgmForWorld(currentWorldOrder, worldData);
            AudioClip targetAmbient = GetAmbienceForWorld(currentWorldOrder, worldData);

            if (activeCrossfadeRoutine != null) StopCoroutine(activeCrossfadeRoutine);
            activeCrossfadeRoutine = StartCoroutine(CrossfadeIslandRoutine(targetBgm, targetAmbient, duration));
        }

        private void PlayIslandThemeImmediate(int worldOrder, WorldData worldData = null)
        {
            currentWorldOrder = Mathf.Clamp(worldOrder, 1, 5);

            AudioClip targetBgm = GetBgmForWorld(currentWorldOrder, worldData);
            AudioClip targetAmbient = GetAmbienceForWorld(currentWorldOrder, worldData);

            bgmSourceA.clip = targetBgm;
            bgmSourceA.volume = isMuted ? 0f : (isNightActive ? bgmVolume * 0.7f : bgmVolume);
            bgmSourceA.Play();

            bgmSourceB.Stop();
            bgmSourceB.volume = 0f;
            isUsingSourceA = true;

            ambientSource.clip = targetAmbient;
            ambientSource.volume = isMuted ? 0f : ambientVolume;
            ambientSource.Play();

            if (ambientNightLayer != null)
            {
                ambientNightSource.clip = ambientNightLayer;
                ambientNightSource.volume = (isNightActive && !isMuted) ? ambientVolume * 0.4f : 0f;
                ambientNightSource.Play();
            }
        }

        private IEnumerator CrossfadeIslandRoutine(AudioClip newBgm, AudioClip newAmbient, float duration)
        {
            AudioSource activeBgm = isUsingSourceA ? bgmSourceA : bgmSourceB;
            AudioSource incomingBgm = isUsingSourceA ? bgmSourceB : bgmSourceA;

            incomingBgm.clip = newBgm;
            incomingBgm.volume = 0f;
            incomingBgm.Play();

            float targetMaxBgm = isNightActive ? bgmVolume * 0.7f : bgmVolume;
            float elapsed = 0f;

            // Smooth cosine / hermite crossfade
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (!isMuted)
                {
                    activeBgm.volume = Mathf.Lerp(targetMaxBgm, 0f, t);
                    incomingBgm.volume = Mathf.Lerp(0f, targetMaxBgm, t);
                }

                yield return null;
            }

            activeBgm.Stop();
            activeBgm.volume = 0f;
            if (!isMuted) incomingBgm.volume = targetMaxBgm;

            isUsingSourceA = !isUsingSourceA;

            // Crossfade Ambient
            if (ambientSource != null && newAmbient != null && ambientSource.clip != newAmbient)
            {
                ambientSource.clip = newAmbient;
                ambientSource.volume = isMuted ? 0f : ambientVolume;
                ambientSource.Play();
            }
        }

        private bool IsClipValid(AudioClip clip)
        {
            try
            {
                return clip != null && clip.length > 0;
            }
            catch
            {
                return false;
            }
        }

        public AudioClip GetBgmForWorld(int worldOrder, WorldData worldData = null)
        {
            if (worldData != null && IsClipValid(worldData.customBgmClip))
            {
                return worldData.customBgmClip;
            }

            if (!IsClipValid(bgmLevel1Temperate))
            {
                LoadOrGenerateAudioClips();
            }

            switch (worldOrder)
            {
                case 1: return bgmLevel1Temperate;
                case 2: return bgmLevel2Coral;
                case 3: return bgmLevel3Autumn;
                case 4: return bgmLevel4Arctic;
                case 5: return bgmLevel5Desert;
                default: return bgmLevel1Temperate;
            }
        }

        public AudioClip GetAmbienceForWorld(int worldOrder, WorldData worldData = null)
        {
            if (worldData != null && IsClipValid(worldData.customAmbienceClip))
            {
                return worldData.customAmbienceClip;
            }

            if (!IsClipValid(ambientLevel1Temperate))
            {
                LoadOrGenerateAudioClips();
            }

            switch (worldOrder)
            {
                case 1: return ambientLevel1Temperate;
                case 2: return ambientLevel2Coral;
                case 3: return ambientLevel3Autumn;
                case 4: return ambientLevel4Arctic;
                case 5: return ambientLevel5Desert;
                default: return ambientLevel1Temperate;
            }
        }
        #endregion

        #region Day/Night Adaptation
        private void OnNightToggled(bool isNight)
        {
            isNightActive = isNight;
            StartCoroutine(FadeNightAmbienceRoutine(isNight, 1.5f));
        }

        private IEnumerator FadeNightAmbienceRoutine(bool isNight, float duration)
        {
            float startNightVol = ambientNightSource.volume;
            float targetNightVol = (isNight && !isMuted) ? ambientVolume * 0.45f : 0f;

            AudioSource activeBgm = isUsingSourceA ? bgmSourceA : bgmSourceB;
            float startBgmVol = activeBgm.volume;
            float targetBgmVol = (isMuted) ? 0f : (isNight ? bgmVolume * 0.7f : bgmVolume);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                ambientNightSource.volume = Mathf.Lerp(startNightVol, targetNightVol, t);
                activeBgm.volume = Mathf.Lerp(startBgmVol, targetBgmVol, t);
                yield return null;
            }

            ambientNightSource.volume = targetNightVol;
            activeBgm.volume = targetBgmVol;
        }
        #endregion

        #region Procedural Island BGM Synthesizers (Level 1 to 5)
        // ------------------ LEVEL 1: TEMPERATE BAY (C MAJOR ACOUSTIC/MARIMBA) ------------------
        private AudioClip CreateTemperateBayBGM()
        {
            int sampleRate = 22050;
            float duration = 8.0f; // 8s seamless looping nautical melody
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            // C major pentatonic notes (C4, E4, G4, A4, C5)
            float[] melodyNotes = new float[] { 261.63f, 329.63f, 392.00f, 440.00f, 523.25f, 392.00f, 329.63f, 261.63f };
            // Bass line (C3, G2, A2, F2)
            float[] bassNotes = new float[] { 130.81f, 98.00f, 110.00f, 87.31f };

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;

                // 1. Melody Note
                int melIdx = Mathf.FloorToInt((timeSec / duration) * melodyNotes.Length) % melodyNotes.Length;
                float melStepTime = duration / melodyNotes.Length;
                float melLocalT = (timeSec % melStepTime) / melStepTime;
                float melEnv = Mathf.Exp(-melLocalT * 4.5f);
                float fMel = melodyNotes[melIdx];
                float melSine = Mathf.Sin(2f * Mathf.PI * fMel * timeSec) +
                                Mathf.Sin(2f * Mathf.PI * fMel * 2f * timeSec) * 0.35f +
                                Mathf.Sin(2f * Mathf.PI * fMel * 3f * timeSec) * 0.15f;

                // 2. Warm Bass
                int bassIdx = Mathf.FloorToInt((timeSec / duration) * bassNotes.Length) % bassNotes.Length;
                float fBass = bassNotes[bassIdx];
                float bassSine = Mathf.Sin(2f * Mathf.PI * fBass * timeSec) * 0.5f;

                data[i] = (melSine * melEnv * 0.45f + bassSine * 0.35f) * 0.65f;
            }

            AudioClip clip = AudioClip.Create("BGM_TemperateBay_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------ LEVEL 2: TROPICAL CORAL ATOLL (F MAJOR CALYPSO STEEL DRUM) ------------------
        private AudioClip CreateTropicalCoralAtollBGM()
        {
            int sampleRate = 22050;
            float duration = 8.0f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            // Calypso syncopated steel drum notes (F4, A4, C5, D5, C5, A4)
            float[] calypsoNotes = new float[] { 349.23f, 440.00f, 523.25f, 587.33f, 440.00f, 523.25f, 349.23f, 523.25f };

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;

                // 16-step rhythmic division with syncopated skip
                int step = Mathf.FloorToInt((timeSec / duration) * 16f) % 16;
                float stepT = (timeSec % (duration / 16f)) / (duration / 16f);

                // Syncopated active hits on steps 0, 2, 5, 8, 10, 13
                bool hit = (step == 0 || step == 2 || step == 5 || step == 8 || step == 10 || step == 13);
                float env = hit ? Mathf.Exp(-stepT * 6.0f) : 0f;

                float f = calypsoNotes[step % calypsoNotes.Length];
                // Steel drum inharmonic timbre: f, 2.76f, 5.4f
                float steelSine = Mathf.Sin(2f * Mathf.PI * f * timeSec) +
                                 Mathf.Sin(2f * Mathf.PI * (f * 2.76f) * timeSec) * 0.35f +
                                 Mathf.Sin(2f * Mathf.PI * (f * 5.40f) * timeSec) * 0.12f;

                // Island bass bounce (F2 = 87.31Hz, C3 = 130.81Hz)
                float fBass = (step < 8) ? 87.31f : 130.81f;
                float bass = Mathf.Sin(2f * Mathf.PI * fBass * timeSec) * 0.35f;

                data[i] = (steelSine * env * 0.5f + bass * 0.25f) * 0.65f;
            }

            AudioClip clip = AudioClip.Create("BGM_TropicalCoral_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------ LEVEL 3: GOLDEN AUTUMN COVE (G MAJOR FOLK GUITAR & WOODBLOCK) ------------------
        private AudioClip CreateAutumnCoveBGM()
        {
            int sampleRate = 22050;
            float duration = 8.0f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            // G - Em - C - D warm autumnal progression
            float[] guitarRoots = new float[] { 196.00f, 164.81f, 130.81f, 146.83f }; // G3, E3, C3, D3

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;

                int bar = Mathf.FloorToInt((timeSec / duration) * 4f) % 4;
                float root = guitarRoots[bar];
                float barTime = (timeSec % (duration / 4f));

                // Fingerpicking arpeggio (Root, Third, Fifth, Octave)
                int arpStep = Mathf.FloorToInt(barTime / (duration / 16f)) % 4;
                float mult = (arpStep == 0) ? 1.0f : (arpStep == 1) ? 1.2599f : (arpStep == 2) ? 1.4983f : 2.0f;
                float fNote = root * mult;

                float noteLocalT = (timeSec % (duration / 16f)) / (duration / 16f);
                float env = Mathf.Exp(-noteLocalT * 5.0f);
                float acousticGuitar = Mathf.Sin(2f * Mathf.PI * fNote * timeSec) +
                                      Mathf.Sin(2f * Mathf.PI * fNote * 2f * timeSec) * 0.25f +
                                      Mathf.Sin(2f * Mathf.PI * fNote * 3f * timeSec) * 0.08f;

                // Subtle woodblock tap on beats 1 and 3
                bool woodblockHit = (Mathf.FloorToInt(timeSec * 2f) % 2 == 0) && (noteLocalT < 0.15f);
                float woodblock = woodblockHit ? (Mathf.Sin(2f * Mathf.PI * 780f * timeSec) * (1f - noteLocalT / 0.15f) * 0.25f) : 0f;

                data[i] = (acousticGuitar * env * 0.5f + woodblock) * 0.65f;
            }

            AudioClip clip = AudioClip.Create("BGM_GoldenAutumn_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------ LEVEL 4: ARCTIC FROST HAVEN (D MINOR CRYSTALLINE CELESTA & PAD) ------------------
        private AudioClip CreateArcticFrostHavenBGM()
        {
            int sampleRate = 22050;
            float duration = 8.0f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            // D minor crystalline notes (D5 = 587.33, F5 = 698.46, A5 = 880.00, C6 = 1046.50)
            float[] celestaNotes = new float[] { 587.33f, 698.46f, 880.00f, 1046.50f, 880.00f, 698.46f };

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;

                // 1. Crystalline Music Box / Celesta Chime
                int noteIdx = Mathf.FloorToInt((timeSec / duration) * celestaNotes.Length) % celestaNotes.Length;
                float stepTime = duration / celestaNotes.Length;
                float localT = (timeSec % stepTime) / stepTime;
                float env = Mathf.Exp(-localT * 3.2f);
                float fCel = celestaNotes[noteIdx];
                float celSine = Mathf.Sin(2f * Mathf.PI * fCel * timeSec) +
                                Mathf.Sin(2f * Mathf.PI * (fCel * 2.4f) * timeSec) * 0.22f; // Inharmonic bell sparkle

                // 2. Sub-zero Polar Drone Pad (D2 = 73.42Hz with slow chorus drift)
                float lfo = Mathf.Sin(2f * Mathf.PI * 0.2f * timeSec) * 1.5f;
                float polarPad = Mathf.Sin(2f * Mathf.PI * (73.42f + lfo) * timeSec) * 0.35f;

                data[i] = (celSine * env * 0.45f + polarPad * 0.35f) * 0.60f;
            }

            AudioClip clip = AudioClip.Create("BGM_ArcticFrost_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------ LEVEL 5: SUNKEN OASIS DESERT (E HIJAZ / PHRYGIAN OUD MELODY) ------------------
        private AudioClip CreateDesertOasisBGM()
        {
            int sampleRate = 22050;
            float duration = 8.0f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            // E Hijaz / Phrygian Dominant scale: E4 (329.63), F4 (349.23), G#4 (415.30), A4 (440.00), B4 (493.88), C5 (523.25), D5 (587.33)
            float[] hijazMelody = new float[] { 329.63f, 349.23f, 415.30f, 440.00f, 415.30f, 349.23f, 329.63f, 493.88f };

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;

                // 1. Plucked Oud Melody (chorus detuned dual string)
                int noteIdx = Mathf.FloorToInt((timeSec / duration) * hijazMelody.Length) % hijazMelody.Length;
                float stepTime = duration / hijazMelody.Length;
                float localT = (timeSec % stepTime) / stepTime;
                float env = Mathf.Exp(-localT * 4.2f);
                float fOud = hijazMelody[noteIdx];
                float oudSine = Mathf.Sin(2f * Mathf.PI * fOud * timeSec) * 0.6f +
                               Mathf.Sin(2f * Mathf.PI * (fOud * 1.008f) * timeSec) * 0.4f;

                // 2. Darbuka / Percussive Caravan Step (E2 = 82.41Hz low thump)
                float pulseT = (timeSec % (duration / 8f)) / (duration / 8f);
                float kick = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 50f, pulseT) * timeSec) * Mathf.Exp(-pulseT * 8f) * 0.35f;

                data[i] = (oudSine * env * 0.5f + kick) * 0.65f;
            }

            AudioClip clip = AudioClip.Create("BGM_DesertOasis_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion

        #region Procedural Island Ambience Synthesizers
        private AudioClip CreateTemperateBayAmbience()
        {
            return CreateOceanAmbientSound(); // Rolling waves & coastal breeze
        }

        private AudioClip CreateTropicalAmbience()
        {
            int sampleRate = 22050;
            float duration = 4.0f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                float waveLfo = (Mathf.Sin(2f * Mathf.PI * 0.3f * timeSec) + 1f) * 0.5f;

                // Gentle turquoise surf
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.08f;
                // Underwater bubbling resonance modulation
                float bubbleLfo = Mathf.Sin(2f * Mathf.PI * 180f * timeSec) * Mathf.Sin(2f * Mathf.PI * 3f * timeSec) * 0.04f;

                data[i] = (noise * (0.4f + waveLfo * 0.6f) + bubbleLfo) * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Ambient_TropicalCoral_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateAutumnAmbience()
        {
            int sampleRate = 22050;
            float duration = 4.0f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                // Swirling wind gust
                float gust = Mathf.Pow((Mathf.Sin(2f * Mathf.PI * 0.25f * timeSec) + 1f) * 0.5f, 2.0f);
                float wind = (UnityEngine.Random.value * 2f - 1f) * (0.05f + gust * 0.08f);

                // Leaf rustle crackles
                float crackle = (UnityEngine.Random.value > 0.985f) ? (UnityEngine.Random.value * 0.15f) : 0f;

                data[i] = (wind + crackle) * 0.42f;
            }

            AudioClip clip = AudioClip.Create("Ambient_GoldenAutumn_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateArcticAmbience()
        {
            int sampleRate = 22050;
            float duration = 4.0f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                // Sub-zero whistling polar wind
                float whistleFreq = 620f + Mathf.Sin(2f * Mathf.PI * 0.4f * timeSec) * 180f;
                float whistle = Mathf.Sin(2f * Mathf.PI * whistleFreq * timeSec) * 0.04f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.07f;

                // Creaking ice deep groan
                float iceGroan = Mathf.Sin(2f * Mathf.PI * 52f * timeSec) * 0.03f;

                data[i] = (whistle + noise + iceGroan) * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Ambient_ArcticFrost_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateDesertAmbience()
        {
            int sampleRate = 22050;
            float duration = 4.0f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                // Warm sand dune wind sweeping
                float duneGust = Mathf.Sin(2f * Mathf.PI * 0.2f * timeSec) * 0.5f + 0.5f;
                float sandWind = (UnityEngine.Random.value * 2f - 1f) * (0.04f + duneGust * 0.06f);

                // Trickle of oasis water spring
                float waterTrickle = Mathf.Sin(2f * Mathf.PI * 920f * timeSec) * (UnityEngine.Random.value * 0.02f);

                data[i] = (sandWind + waterTrickle) * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Ambient_DesertOasis_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateNightAmbience()
        {
            int sampleRate = 22050;
            float duration = 4.0f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                // Cricket chirp bursts (around 4500Hz)
                bool isChirping = (Mathf.Sin(2f * Mathf.PI * 1.5f * timeSec) > 0.6f);
                float cricket = isChirping ? Mathf.Sin(2f * Mathf.PI * 4500f * timeSec) * (UnityEngine.Random.value * 0.06f) : 0f;

                // Soft night sea lap
                float sea = (UnityEngine.Random.value * 2f - 1f) * 0.025f;

                data[i] = (cricket + sea) * 0.40f;
            }

            AudioClip clip = AudioClip.Create("Ambient_NightLayer_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion

        #region Procedural Island Gimmick Signature SFX
        // ------------------ WORLD 2 GIMMICK: PEARL HARVEST CLACK ------------------
        private AudioClip CreatePearlHarvestClackSound()
        {
            int sampleRate = 44100;
            float duration = 0.38f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                float t = (float)i / samples;

                // Sharp shell snap
                float snapEnv = Mathf.Exp(-t * 22f);
                float snap = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 250f, t) * timeSec) * snapEnv;

                // Crystalline pearl chime (E6 = 1318Hz, B6 = 1975Hz)
                float chimeEnv = Mathf.Exp(-t * 8f);
                float chime = (Mathf.Sin(2f * Mathf.PI * 1318f * timeSec) + Mathf.Sin(2f * Mathf.PI * 1975f * timeSec) * 0.5f) * chimeEnv * 0.6f;

                data[i] = (snap * 0.6f + chime * 0.5f) * 0.85f;
            }

            AudioClip clip = AudioClip.Create("SFX_PearlHarvestClack", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------ WORLD 3 GIMMICK: SMOKER BBQ SIZZLE ------------------
        private AudioClip CreateBBQSizzleSound()
        {
            int sampleRate = 44100;
            float duration = 0.45f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Sin(t * Mathf.PI);
                // Sizzling burst
                float sizzle = (UnityEngine.Random.value * 2f - 1f) * 0.65f;
                // Pop crackle
                float pop = (UnityEngine.Random.value > 0.96f) ? (UnityEngine.Random.value * 2f - 1f) * 0.4f : 0f;

                data[i] = (sizzle + pop) * env * 0.75f;
            }

            AudioClip clip = AudioClip.Create("SFX_BBQSizzle", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------ WORLD 4 GIMMICK: ARCTIC ICE CRACK ------------------
        private AudioClip CreateIceCrackSound()
        {
            int sampleRate = 44100;
            float duration = 0.32f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                float t = (float)i / samples;

                // Deep sub-ice thud (65Hz)
                float thud = Mathf.Sin(2f * Mathf.PI * 65f * timeSec) * Mathf.Exp(-t * 12f) * 0.75f;
                // High brittle fracture snap
                float crack = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 26f) * 0.8f;

                data[i] = (thud + crack) * 0.85f;
            }

            AudioClip clip = AudioClip.Create("SFX_IceCrack", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------ WORLD 5 GIMMICK: CARAVAN BELLS ------------------
        private AudioClip CreateCaravanBellsSound()
        {
            int sampleRate = 44100;
            float duration = 0.55f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                float t = (float)i / samples;

                // Dual brass camel bells (A5 = 880Hz, E6 = 1320Hz)
                float env = Mathf.Exp(-t * 6.5f);
                float bell1 = Mathf.Sin(2f * Mathf.PI * 880f * timeSec);
                float bell2 = Mathf.Sin(2f * Mathf.PI * 1320f * timeSec) * 0.65f;
                float coinRustle = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 15f) * 0.2f;

                data[i] = ((bell1 + bell2) * env * 0.6f + coinRustle) * 0.80f;
            }

            AudioClip clip = AudioClip.Create("SFX_CaravanBells", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion

        #region Core Procedural SFX Generators
        private AudioClip CreateSplashSound()
        {
            int sampleRate = 44100;
            float duration = 0.28f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Pow(1.0f - t, 2.0f);
                float freq = Mathf.Lerp(650f, 180f, t);
                float sine = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.45f;
                data[i] = (sine * 0.55f + noise) * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("Procedural_Splash", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateWhackSound()
        {
            int sampleRate = 44100;
            float duration = 0.16f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 18.0f);
                float freq = Mathf.Lerp(190f, 45f, t);
                float sine = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate));
                float snap = (t < 0.08f) ? (UnityEngine.Random.value * 2f - 1f) * (1f - t / 0.08f) * 0.4f : 0f;
                data[i] = (sine * 0.7f + snap) * env * 0.85f;
            }

            AudioClip clip = AudioClip.Create("Procedural_Whack", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateSliceSound()
        {
            int sampleRate = 44100;
            float duration = 0.14f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-t * 6f);
                float freq = Mathf.Lerp(1200f, 500f, t);
                float sine = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.5f;
                data[i] = (sine * 0.5f + noise) * env * 0.65f;
            }

            AudioClip clip = AudioClip.Create("Procedural_Slice", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateCoinSound()
        {
            int sampleRate = 44100;
            float duration = 0.26f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 14.0f);
                float s1 = Mathf.Sin(2f * Mathf.PI * 1760f * (i / (float)sampleRate));
                float s2 = Mathf.Sin(2f * Mathf.PI * 2640f * (i / (float)sampleRate)) * 0.45f;
                data[i] = (s1 + s2) * env * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Procedural_Coin", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateChestOpenSound()
        {
            int sampleRate = 44100;
            float duration = 0.70f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            float[] freqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDuration = duration / freqs.Length;

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                int noteIndex = Mathf.Clamp(Mathf.FloorToInt(timeSec / noteDuration), 0, freqs.Length - 1);
                float noteT = (timeSec - noteIndex * noteDuration) / noteDuration;
                float env = Mathf.Exp(-noteT * 5.0f);
                float freq = freqs[noteIndex];
                float s = Mathf.Sin(2f * Mathf.PI * freq * timeSec);
                float shimmer = Mathf.Sin(2f * Mathf.PI * (freq * 2f) * timeSec) * 0.25f;
                data[i] = (s + shimmer) * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("Procedural_ChestOpen", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateFrenzyStartSound()
        {
            int sampleRate = 44100;
            float duration = 1.1f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                float t = (float)i / samples;
                float env = Mathf.Sin(t * Mathf.PI);
                float freq1 = Mathf.Lerp(440f, 880f, t);
                float freq2 = freq1 * 1.25f;
                float s1 = Mathf.Sin(2f * Mathf.PI * freq1 * timeSec);
                float s2 = Mathf.Sin(2f * Mathf.PI * freq2 * timeSec) * 0.6f;
                data[i] = (s1 + s2) * env * 0.75f;
            }

            AudioClip clip = AudioClip.Create("Procedural_FrenzyStart", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateShipHornSound()
        {
            int sampleRate = 44100;
            float duration = 1.35f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                float t = (float)i / samples;
                float env = t < 0.10f ? (t / 0.10f) : Mathf.Exp(-(t - 0.10f) * 2.2f);
                float f1 = 110f;
                float f2 = 165f;
                float f3 = 220f;
                float s1 = Mathf.Sin(2f * Mathf.PI * f1 * timeSec);
                float s2 = Mathf.Sin(2f * Mathf.PI * f2 * timeSec) * 0.55f;
                float s3 = Mathf.Sin(2f * Mathf.PI * f3 * timeSec) * 0.25f;
                float brass = Mathf.Sin(2f * Mathf.PI * (f1 * 3f) * timeSec) * 0.12f;
                data[i] = (s1 + s2 + s3 + brass) * env * 0.85f;
            }

            AudioClip clip = AudioClip.Create("Procedural_ShipHorn", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateUIClickSound()
        {
            int sampleRate = 44100;
            float duration = 0.06f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Sin(t * Mathf.PI);
                float freq = Mathf.Lerp(550f, 880f, t);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * env * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Procedural_UIClick", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateOceanAmbientSound()
        {
            int sampleRate = 22050;
            float duration = 4.0f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            float b0 = 0f, b1 = 0f, b2 = 0f;
            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                float lfo = (Mathf.Sin(2f * Mathf.PI * 0.35f * timeSec) + 1f) * 0.5f;
                lfo = Mathf.Pow(lfo, 1.8f);

                float white = UnityEngine.Random.value * 2f - 1f;
                b0 = 0.99765f * b0 + white * 0.0990460f;
                b1 = 0.96300f * b1 + white * 0.2965164f;
                b2 = 0.57000f * b2 + white * 1.0526913f;
                float pink = (b0 + b1 + b2 + white * 0.1848f) * 0.08f;

                data[i] = pink * (0.3f + lfo * 0.7f) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("Procedural_OceanAmbient", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion

        #region Public API
        public void PlaySplash(Vector3 position = default) => PlaySFX(splashClip, 0.85f, 0.1f);
        public void PlayWhack(Vector3 position = default) => PlaySFX(whackClip, 0.9f, 0.12f);
        public void PlaySlice(Vector3 position = default) => PlaySFX(sliceClip, 0.75f, 0.1f);
        public void PlayCoin() => PlaySFX(coinClip, 0.75f, 0.15f);
        public void PlayChestOpen(Vector3 position = default) => PlaySFX(chestOpenClip, 1.0f, 0.02f);
        public void PlayLevelUp(Vector3 position = default) => PlaySFX(chestOpenClip, 1.0f, 0.05f);
        public void PlayFrenzyStart() => PlaySFX(frenzyStartClip, 1.0f);
        public void PlayUIClick() => PlaySFX(uiClickClip, 0.65f, 0.08f);
        public void PlayUIPop(Vector3 position = default) => PlaySFX(uiClickClip, 0.70f, 0.12f);
        public void PlayBubbles(Vector3 position = default) => PlaySFX(bubblesClip ?? splashClip, 0.75f, 0.15f);
        public void PlayStun(Vector3 position = default) => PlaySFX(stunClip ?? whackClip, 0.85f, 0.08f);
        public void PlayConfetti(Vector3 position = default) => PlaySFX(confettiClip ?? chestOpenClip, 0.95f, 0.05f);
        public void PlayHeart(Vector3 position = default) => PlaySFX(heartClip ?? coinClip, 0.85f, 0.1f);
        public void PlayRodCastWhoosh(Vector3 position = default) => PlaySFX(rodCastWhooshClip ?? sliceClip, 0.90f, 0.15f);
        public void PlayReelCrank(Vector3 position = default) => PlaySFX(reelCrankClip ?? uiClickClip, 0.75f, 0.18f);
        public void PlayWaterPlop(Vector3 position = default) => PlaySFX(waterPlopClip ?? splashClip, 0.85f, 0.12f);
        public void PlayFishDockDrop(Vector3 position = default) => PlaySFX(fishDockDropClip ?? whackClip, 0.80f, 0.10f);
        public void PlayWranglerCritWhack(Vector3 position = default) => PlaySFX(wranglerCritWhackClip ?? whackClip, 1.0f, 0.12f);
        public void PlayContractFanfare(Vector3 position = default) => PlaySFX(contractFanfareClip ?? chestOpenClip, 1.0f, 0.05f);
        public void PlayUpgradeFanfare(Vector3 position = default) => PlayContractFanfare(position);
        public void PlayLevelCelebration(Vector3 position = default) => PlaySFX(shipHornClip ?? CreateShipHornSound(), 1.0f, 0.02f);
        public void PlayShipHorn(Vector3 position = default) => PlaySFX(shipHornClip ?? CreateShipHornSound(), 0.95f, 0.03f);

        // Island Gimmick Signature SFX
        public void PlayPearlHarvestClack(Vector3 position = default) => PlaySFX(pearlHarvestClackClip ?? CreatePearlHarvestClackSound(), 1.0f, 0.06f);
        public void PlayBBQSizzle(Vector3 position = default) => PlaySFX(bbqSizzleClip ?? CreateBBQSizzleSound(), 0.95f, 0.08f);
        public void PlayIceCrack(Vector3 position = default) => PlaySFX(iceCrackClip ?? CreateIceCrackSound(), 1.05f, 0.08f);
        public void PlayCaravanBells(Vector3 position = default) => PlaySFX(caravanBellsClip ?? CreateCaravanBellsSound(), 0.95f, 0.06f);

        public void PlayOceanAmbient()
        {
            if (isMuted || ambientSource == null) return;
            ambientSource.volume = ambientVolume;
            if (!ambientSource.isPlaying) ambientSource.Play();
        }

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            AudioSource active = isUsingSourceA ? bgmSourceA : bgmSourceB;
            if (active != null) active.volume = isMuted ? 0f : (isNightActive ? bgmVolume * 0.7f : bgmVolume);
            SaveVolumePreferences();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SaveVolumePreferences();
        }

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            if (ambientSource != null) ambientSource.volume = isMuted ? 0f : ambientVolume;
            if (ambientNightSource != null) ambientNightSource.volume = (isNightActive && !isMuted) ? ambientVolume * 0.45f : 0f;
            SaveVolumePreferences();
        }

        public void SetMuted(bool mute)
        {
            isMuted = mute;
            if (bgmSourceA != null) bgmSourceA.mute = isMuted;
            if (bgmSourceB != null) bgmSourceB.mute = isMuted;
            if (ambientSource != null) ambientSource.mute = isMuted;
            if (ambientNightSource != null) ambientNightSource.mute = isMuted;
            if (sfxSource != null) sfxSource.mute = isMuted;
            SaveVolumePreferences();
        }

        private void PlaySFX(AudioClip clip, float volumeScale = 1.0f, float pitchVariance = 0f)
        {
            if (isMuted || sfxSource == null || clip == null) return;

            sfxSource.pitch = (pitchVariance > 0f) ? UnityEngine.Random.Range(1f - pitchVariance, 1f + pitchVariance) : 1f;
            sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }
        #endregion

        #region Event Handlers
        private void OnFrenzyStarted() => PlayFrenzyStart();
        private void OnChestOpened(int gold) => PlayChestOpen();
        private void OnBossSpawned(FishChainData boss) => PlayWhack();
        private void OnPrestigeCompleted(int medals) => PlayChestOpen();
        #endregion
    }
}
