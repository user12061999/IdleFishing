using UnityEngine;
using IdleFishing.VFX;

namespace IdleFishing.World
{
    /// <summary>
    /// Makes any floating object (buoy, boat, crate, raft) bob and tilt naturally
    /// with the ocean surface waves and current from OceanWaterFlowController.
    /// </summary>
    [ExecuteAlways]
    public class WaterBuoyancyFloater : MonoBehaviour
    {
        [Header("Buoyancy & Wave Response")]
        [SerializeField] private float heightOffset = 0.0f;
        [SerializeField] private float verticalResponsiveness = 1.0f;
        [SerializeField] private float tiltAngleMultiplier = 14.0f;
        [SerializeField] private float currentSwayAmplitude = 0.08f;
        [SerializeField] private float phaseOffset = 0.0f;

        [Header("Interactive Wake Ripples")]
        [SerializeField] private bool emitWakeRipples = true;
        [SerializeField] private float wakeInterval = 3.2f;
        [SerializeField] private float wakeScale = 0.85f;

        private Vector3 anchorPosition;
        private Quaternion anchorRotation;
        private bool anchorInitialized = false;
        private float wakeTimer = 0f;

        public void SetAnchor(Vector3 worldPos, Quaternion worldRot)
        {
            anchorPosition = worldPos;
            anchorRotation = worldRot;
            anchorInitialized = true;
        }

        public void Configure(float yOffset, float responsiveness = 1.0f, float tiltMult = 14.0f, bool emitWake = true, float wakeScaleVal = 0.85f)
        {
            heightOffset = yOffset;
            verticalResponsiveness = responsiveness;
            tiltAngleMultiplier = tiltMult;
            emitWakeRipples = emitWake;
            wakeScale = wakeScaleVal;
            anchorPosition = transform.position;
            anchorRotation = transform.rotation;
            anchorInitialized = true;
            phaseOffset = Mathf.Abs(transform.position.x * 1.37f + transform.position.z * 0.83f);
        }

        private void OnEnable()
        {
            if (!anchorInitialized)
            {
                anchorPosition = transform.position;
                anchorRotation = transform.rotation;
                anchorInitialized = true;
            }
            phaseOffset = Mathf.Abs(anchorPosition.x * 1.37f + anchorPosition.z * 0.83f);
            wakeTimer = Random.Range(0f, wakeInterval);
        }

        private void Update()
        {
            if (!anchorInitialized)
            {
                anchorPosition = transform.position;
                anchorRotation = transform.rotation;
                anchorInitialized = true;
            }

#if UNITY_EDITOR
            float t = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
            float t = Time.time;
#endif

            float waveY = 0.05f + Mathf.Sin(t * 1.8f + phaseOffset) * 0.055f;
            float gx = Mathf.Cos(t * 1.8f + phaseOffset) * 0.08f;
            float gz = Mathf.Sin(t * 1.4f + phaseOffset) * 0.08f;
            Vector3 waveNormal = new Vector3(-gx, 1f, -gz).normalized;
            Vector2 flowDir = new Vector2(0.92f, 0.22f).normalized;

            // Gentle horizontal tug along the water flow current
            float sway = Mathf.Sin(t * 1.35f + phaseOffset) * currentSwayAmplitude;
            Vector3 targetPos = new Vector3(
                anchorPosition.x + flowDir.x * sway,
                Mathf.Lerp(anchorPosition.y, waveY + heightOffset, verticalResponsiveness),
                anchorPosition.z + flowDir.y * sway
            );

            transform.position = targetPos;

            // Tilt object to align with wave slope + secondary roll
            Quaternion slopeRot = Quaternion.FromToRotation(Vector3.up, Vector3.Lerp(Vector3.up, waveNormal, tiltAngleMultiplier * 0.06f));
            float roll = Mathf.Sin(t * 1.7f + phaseOffset) * (tiltAngleMultiplier * 0.25f);
            float pitch = Mathf.Cos(t * 1.3f + phaseOffset) * (tiltAngleMultiplier * 0.2f);
            transform.rotation = slopeRot * anchorRotation * Quaternion.Euler(pitch, 0f, roll);

            // Emit periodic particle wake ripples in Play Mode
            if (Application.isPlaying && emitWakeRipples)
            {
                wakeTimer += Time.deltaTime;
                if (wakeTimer >= wakeInterval)
                {
                    wakeTimer = 0f;
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.SpawnWaterRipple(
                            new Vector3(transform.position.x, 0.05f, transform.position.z),
                            0.65f * wakeScale
                        );
                    }
                }
            }
        }
    }
}
