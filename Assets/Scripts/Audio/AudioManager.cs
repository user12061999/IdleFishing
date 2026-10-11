using System;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;

namespace IdleFishing.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        private AudioSource sfxSource;
        private AudioSource ambientSource;

        [Header("Volume Controls")]
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float ambientVolume = 0.25f;
        [SerializeField] private bool isMuted = false;

        // Audio Clips (Authentic SFX + Procedural Fallback)
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
        [SerializeField] private AudioClip oceanAmbientClip;
        [SerializeField] private AudioClip rodCastWhooshClip;
        [SerializeField] private AudioClip reelCrankClip;
        [SerializeField] private AudioClip waterPlopClip;
        [SerializeField] private AudioClip fishDockDropClip;
        [SerializeField] private AudioClip wranglerCritWhackClip;
        [SerializeField] private AudioClip contractFanfareClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            LoadOrGenerateAudioClips();
        }

        private void LoadOrGenerateAudioClips()
        {
#if UNITY_EDITOR
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

            if (splashClip == null) splashClip = CreateSplashSound();
            if (whackClip == null) whackClip = CreateWhackSound();
            if (sliceClip == null) sliceClip = CreateSliceSound();
            if (coinClip == null) coinClip = CreateCoinSound();
            if (chestOpenClip == null) chestOpenClip = CreateChestOpenSound();
            if (frenzyStartClip == null) frenzyStartClip = CreateFrenzyStartSound();
            if (uiClickClip == null) uiClickClip = CreateUIClickSound();
            if (oceanAmbientClip == null) oceanAmbientClip = CreateOceanAmbientSound();
        }

        private void Start()
        {
            PlayOceanAmbient();
            RegisterEvents();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
        }

        private void InitializeAudioSources()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D clean UI & gameplay sound

            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
            ambientSource.volume = ambientVolume;
        }

        private void RegisterEvents()
        {
            GameEvents.FrenzyStarted += OnFrenzyStarted;
            GameEvents.TreasureChestOpened += OnChestOpened;
            GameEvents.BossFishSpawned += OnBossSpawned;
            GameEvents.PrestigeCompleted += OnPrestigeCompleted;
        }

        private void UnregisterEvents()
        {
            GameEvents.FrenzyStarted -= OnFrenzyStarted;
            GameEvents.TreasureChestOpened -= OnChestOpened;
            GameEvents.BossFishSpawned -= OnBossSpawned;
            GameEvents.PrestigeCompleted -= OnPrestigeCompleted;
        }

        // 1. Water Splash (Pitch-glide drop + filtered white noise)
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

        // 2. Whack / Hit Impact (Thump bass + quick snap)
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

        // 3. Slice / Knife Cut (High-frequency zip friction)
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

        // 4. Coin Clink (Harmonic dual sine: 1760Hz + 2640Hz with smooth bell decay)
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

        // 5. Chest Fanfare (Ascending chord: C5 - E5 - G5 - C6)
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

        // 6. Fishing Frenzy Fanfare (Warm triumphal horns arpeggio)
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
                float freq2 = freq1 * 1.25f; // Major third
                float s1 = Mathf.Sin(2f * Mathf.PI * freq1 * timeSec);
                float s2 = Mathf.Sin(2f * Mathf.PI * freq2 * timeSec) * 0.6f;
                data[i] = (s1 + s2) * env * 0.75f;
            }

            AudioClip clip = AudioClip.Create("Procedural_FrenzyStart", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // 7. UI Click Pop (Bubble/Pop click)
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

        // 8. Ocean Waves Ambient (Rolling pink-like noise modulated by an LFO)
        private AudioClip CreateOceanAmbientSound()
        {
            int sampleRate = 22050;
            float duration = 4.0f; // 4 second seamless rolling wave loop
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            float b0 = 0f, b1 = 0f, b2 = 0f;
            for (int i = 0; i < samples; i++)
            {
                float timeSec = (float)i / sampleRate;
                // LFO rolling wave frequency (0.35 Hz)
                float lfo = (Mathf.Sin(2f * Mathf.PI * 0.35f * timeSec) + 1f) * 0.5f;
                lfo = Mathf.Pow(lfo, 1.8f);

                // Simple Pink Noise Filter
                float white = UnityEngine.Random.value * 2f - 1f;
                b0 = 0.99765f * b0 + white * 0.0990460f;
                b1 = 0.96300f * b1 + white * 0.2965164f;
                b2 = 0.57000f * b2 + white * 1.0526913f;
                float pink = (b0 + b1 + b2 + white * 0.1848f) * 0.08f;

                // Modulate with wave surge
                data[i] = pink * (0.3f + lfo * 0.7f) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("Procedural_OceanAmbient", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ==================== PUBLIC API ====================
        public void PlaySplash(Vector3 position = default)
        {
            PlaySFX(splashClip, volumeScale: 0.85f, pitchVariance: 0.1f);
        }

        public void PlayWhack(Vector3 position = default)
        {
            PlaySFX(whackClip, volumeScale: 0.9f, pitchVariance: 0.12f);
        }

        public void PlaySlice(Vector3 position = default)
        {
            PlaySFX(sliceClip, volumeScale: 0.75f, pitchVariance: 0.1f);
        }

        public void PlayCoin()
        {
            PlaySFX(coinClip, volumeScale: 0.75f, pitchVariance: 0.15f);
        }

        public void PlayChestOpen(Vector3 position = default)
        {
            PlaySFX(chestOpenClip, volumeScale: 1.0f, pitchVariance: 0.02f);
        }

        public void PlayLevelUp(Vector3 position = default)
        {
            PlaySFX(chestOpenClip, volumeScale: 1.0f, pitchVariance: 0.05f);
        }

        public void PlayFrenzyStart()
        {
            PlaySFX(frenzyStartClip, volumeScale: 1.0f);
        }

        public void PlayUIClick()
        {
            PlaySFX(uiClickClip, volumeScale: 0.65f, pitchVariance: 0.08f);
        }

        public void PlayUIPop(Vector3 position = default)
        {
            PlaySFX(uiClickClip, volumeScale: 0.70f, pitchVariance: 0.12f);
        }

        public void PlayBubbles(Vector3 position = default)
        {
            if (bubblesClip != null) PlaySFX(bubblesClip, volumeScale: 0.75f, pitchVariance: 0.15f);
        }

        public void PlayStun(Vector3 position = default)
        {
            if (stunClip != null) PlaySFX(stunClip, volumeScale: 0.85f, pitchVariance: 0.08f);
        }

        public void PlayConfetti(Vector3 position = default)
        {
            PlaySFX(confettiClip != null ? confettiClip : chestOpenClip, volumeScale: 0.95f, pitchVariance: 0.05f);
        }

        public void PlayHeart(Vector3 position = default)
        {
            PlaySFX(heartClip != null ? heartClip : coinClip, volumeScale: 0.85f, pitchVariance: 0.1f);
        }

        public void PlayRodCastWhoosh(Vector3 position = default)
        {
            PlaySFX(rodCastWhooshClip != null ? rodCastWhooshClip : sliceClip, volumeScale: 0.90f, pitchVariance: 0.15f);
        }

        public void PlayReelCrank(Vector3 position = default)
        {
            PlaySFX(reelCrankClip != null ? reelCrankClip : uiClickClip, volumeScale: 0.75f, pitchVariance: 0.18f);
        }

        public void PlayWaterPlop(Vector3 position = default)
        {
            PlaySFX(waterPlopClip != null ? waterPlopClip : splashClip, volumeScale: 0.85f, pitchVariance: 0.12f);
        }

        public void PlayFishDockDrop(Vector3 position = default)
        {
            PlaySFX(fishDockDropClip != null ? fishDockDropClip : whackClip, volumeScale: 0.80f, pitchVariance: 0.10f);
        }

        public void PlayWranglerCritWhack(Vector3 position = default)
        {
            PlaySFX(wranglerCritWhackClip != null ? wranglerCritWhackClip : whackClip, volumeScale: 1.0f, pitchVariance: 0.12f);
        }

        public void PlayContractFanfare(Vector3 position = default)
        {
            PlaySFX(contractFanfareClip != null ? contractFanfareClip : chestOpenClip, volumeScale: 1.0f, pitchVariance: 0.05f);
        }

        public void PlayOceanAmbient()
        {
            if (isMuted || ambientSource == null || oceanAmbientClip == null) return;
            ambientSource.clip = oceanAmbientClip;
            ambientSource.volume = ambientVolume;
            ambientSource.Play();
        }

        public void SetMuted(bool mute)
        {
            isMuted = mute;
            if (ambientSource != null)
            {
                ambientSource.mute = isMuted;
            }
            if (sfxSource != null)
            {
                sfxSource.mute = isMuted;
            }
        }

        private void PlaySFX(AudioClip clip, float volumeScale = 1.0f, float pitchVariance = 0f)
        {
            if (isMuted || sfxSource == null || clip == null) return;

            sfxSource.pitch = (pitchVariance > 0f) ? UnityEngine.Random.Range(1f - pitchVariance, 1f + pitchVariance) : 1f;
            sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        // ==================== EVENT HANDLERS ====================
        private void OnFrenzyStarted()
        {
            PlayFrenzyStart();
        }

        private void OnChestOpened(int gold)
        {
            PlayChestOpen();
        }

        private void OnBossSpawned(FishChainData boss)
        {
            PlayWhack();
        }

        private void OnPrestigeCompleted(int medals)
        {
            PlayChestOpen();
        }
    }
}
