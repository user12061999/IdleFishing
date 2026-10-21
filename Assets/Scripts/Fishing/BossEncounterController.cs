using System.Collections;
using UnityEngine;
using IdleFishing.Audio;
using IdleFishing.Core;
using IdleFishing.UI;
using IdleFishing.VFX;

namespace IdleFishing.Fishing
{
    public class BossEncounterController : MonoBehaviour
    {
        [Header("State & Tuning")]
        [SerializeField] private int currentPhase = 1; // 1 = Deck Hops & Slams, 2 = Warning Charge & Enraged
        [SerializeField] private float phase2HealthThreshold = 0.60f; // 60% HP
        [SerializeField] private bool isTelegraphing = false;
        [SerializeField] private bool hasTransitionedToPhase2 = false;

        private LiveFish liveFish;
        private GameObject telegraphDecal;
        private Coroutine bossRoutine;

        public int CurrentPhase => currentPhase;
        public bool IsTelegraphing => isTelegraphing;

        private void Awake()
        {
            liveFish = GetComponent<LiveFish>();
        }

        private void Start()
        {
            bossRoutine = StartCoroutine(BossBehaviorLoop());
        }

        private void OnDestroy()
        {
            if (telegraphDecal != null)
            {
                Destroy(telegraphDecal);
            }
        }

        private void Update()
        {
            if (liveFish == null || liveFish.IsDefeated) return;

            // Health Phase Transition check
            float hpPercent = (float)liveFish.CurrentHealth / Mathf.Max(1, liveFish.MaxHealth);
            if (!hasTransitionedToPhase2 && hpPercent <= phase2HealthThreshold)
            {
                TransitionToPhase2();
            }
        }

        private void TransitionToPhase2()
        {
            hasTransitionedToPhase2 = true;
            currentPhase = 2;

            Debug.Log("[BossEncounterController] *** BOSS ENTERED PHASE 2: ENRAGED RAMPAGE! ***");

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 2.2f,
                    0,
                    DamageStyle.GoldBonus,
                    "PHASE 2: ENRAGED CHARGE!"
                );
            }

            if (IdleFishing.CameraControl.MobilePortraitCameraController.Instance != null)
            {
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerShake(0.45f, 0.35f);
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerPunchFOV(-2.5f);
            }

            // Brief slow-mo impact
            StartCoroutine(PhaseChangeSlowMoRoutine());
        }

        private IEnumerator PhaseChangeSlowMoRoutine()
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0.4f;
            yield return YieldCache.WaitForSeconds(0.18f);
            Time.timeScale = originalTimeScale;
        }

        private IEnumerator BossBehaviorLoop()
        {
            while (liveFish != null && !liveFish.IsDefeated)
            {
                if (currentPhase == 1)
                {
                    // Phase 1: Heavy deck slam every 5 seconds
                    yield return YieldCache.WaitForSeconds(5.0f);
                    if (liveFish.IsDefeated) yield break;

                    PerformDeckSlam();
                }
                else
                {
                    // Phase 2: Warning Telegraph & Charge every 6 seconds
                    yield return YieldCache.WaitForSeconds(5.5f);
                    if (liveFish.IsDefeated) yield break;

                    yield return PerformTelegraphCharge();
                }
            }
        }

        private void PerformDeckSlam()
        {
            if (liveFish.IsDefeated) return;

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSplash(transform.position + Vector3.up * 0.1f, 1.8f);
                VFXManager.Instance.SpawnWaterRipple(transform.position, 3.2f);
                VFXManager.Instance.SpawnDustPuff(transform.position + Vector3.up * 0.05f, 1.2f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWranglerCritWhack();
            }

            if (IdleFishing.CameraControl.MobilePortraitCameraController.Instance != null)
            {
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerShake(0.35f, 0.22f);
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerPunchFOV(-1.8f);
            }
        }

        private IEnumerator PerformTelegraphCharge()
        {
            isTelegraphing = true;
            SpawnTelegraphDecal();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCoin(); // Audible charging chime
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 2.0f,
                    0,
                    DamageStyle.FishAttack,
                    "! DANGER CHARGE !"
                );
            }

            // 1.5s Telegraph Warning (Idle safe: auto-executes without requiring player tap)
            float elapsed = 0f;
            while (elapsed < 1.5f)
            {
                if (liveFish == null || liveFish.IsDefeated)
                {
                    CleanupTelegraphDecal();
                    isTelegraphing = false;
                    yield break;
                }

                // Pulsate telegraph red
                if (telegraphDecal != null)
                {
                    float alpha = Mathf.PingPong(Time.time * 7f, 0.40f) + 0.30f;
                    SetDecalAlpha(alpha);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Execute Charge
            CleanupTelegraphDecal();
            isTelegraphing = false;

            if (liveFish != null && !liveFish.IsDefeated)
            {
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnSplash(transform.position + transform.forward * 2.0f, 2.2f);
                    VFXManager.Instance.SpawnWaterRipple(transform.position, 3.8f);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayWranglerCritWhack();
                }

                if (IdleFishing.CameraControl.MobilePortraitCameraController.Instance != null)
                {
                    IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerShake(0.42f, 0.30f);
                    IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerPunchFOV(-2.5f);
                }
            }
        }

        public void OnPlayerTappedDuringTelegraph()
        {
            if (!isTelegraphing) return;

            Debug.Log("[BossEncounterController] Player countered Boss telegraph with a timely tap!");
            CleanupTelegraphDecal();
            isTelegraphing = false;

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 1.8f,
                    0,
                    DamageStyle.Crit,
                    "[CRITICAL HIT] WEAK SPOT BROKEN!"
                );
            }

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnStunStars(transform, 3.0f, Vector3.up * 1.5f);
                VFXManager.Instance.SpawnGoldBurst(transform.position + Vector3.up * 1.5f, 15);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayStun();
                AudioManager.Instance.PlayChestOpen();
            }

            // Hit-Stop Micro-freeze (35ms)
            StartCoroutine(HitStopRoutine(0.04f));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            float prevTimeScale = Time.timeScale;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = prevTimeScale;
        }

        private void SpawnTelegraphDecal()
        {
            if (telegraphDecal != null) Destroy(telegraphDecal);

            telegraphDecal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            telegraphDecal.name = "Boss_Telegraph_Decal";
            Vector3 decalPos = transform.position + transform.forward * 2.4f;
            decalPos.y = 0.635f;
            telegraphDecal.transform.position = decalPos;
            telegraphDecal.transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
            telegraphDecal.transform.localScale = new Vector3(3.2f, 5.8f, 1.0f);

            var col = telegraphDecal.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = telegraphDecal.GetComponent<Renderer>();
            if (rend != null)
            {
                // Create transparent unlit material with sprites/default for guaranteed clean mobile transparency
                Material mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
                mat.renderQueue = 3100;
                rend.material = mat;
                SetDecalAlpha(0.45f);
            }
        }

        private void SetDecalAlpha(float a)
        {
            if (telegraphDecal == null) return;
            var rend = telegraphDecal.GetComponent<Renderer>();
            if (rend != null && rend.material != null)
            {
                Color c = new Color(1.0f, 0.12f, 0.12f, a);
                if (rend.material.HasProperty("_Color")) rend.material.color = c;
                if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", c);
            }
        }

        private void CleanupTelegraphDecal()
        {
            if (telegraphDecal != null)
            {
                Destroy(telegraphDecal);
                telegraphDecal = null;
            }
        }
    }
}
