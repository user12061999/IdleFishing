using UnityEngine;

namespace IdleFishing.World
{
    /// <summary>
    /// Applies gentle sinusoidal floating, pitch, and roll to distant ocean props
    /// (ships, buoys, ice floes, marine wildlife) for a vibrant, living maritime atmosphere.
    /// </summary>
    [SelectionBase]
    public class DistantOceanBobbing : MonoBehaviour
    {
        [Header("Heave (Vertical Floating)")]
        [SerializeField] private float heaveAmplitude = 0.08f;
        [SerializeField] private float heaveSpeed = 1.3f;

        [Header("Pitch (Forward/Backward Rocking)")]
        [SerializeField] private float pitchAmplitude = 2.0f;
        [SerializeField] private float pitchSpeed = 0.95f;

        [Header("Roll (Side-to-Side Rocking)")]
        [SerializeField] private float rollAmplitude = 2.8f;
        [SerializeField] private float rollSpeed = 1.15f;

        [Header("Drift (Gentle Yaw / Horizontal Shift)")]
        [SerializeField] private float yawAmplitude = 1.2f;
        [SerializeField] private float yawSpeed = 0.6f;

        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private float phaseOffset;

        private void Awake()
        {
            initialLocalPos = transform.localPosition;
            initialLocalRot = transform.localRotation;
            // Deterministic yet varied phase based on position
            phaseOffset = (transform.position.x * 1.7f + transform.position.z * 2.3f) % (Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            if (initialLocalPos == Vector3.zero && initialLocalRot == Quaternion.identity)
            {
                initialLocalPos = transform.localPosition;
                initialLocalRot = transform.localRotation;
            }
        }

        private void Update()
        {
            float t = Time.time;

            // Vertical heave
            float dy = Mathf.Sin(t * heaveSpeed + phaseOffset) * heaveAmplitude;
            transform.localPosition = initialLocalPos + new Vector3(0f, dy, 0f);

            // Rotational rocking: Pitch (X), Yaw (Y), Roll (Z)
            float pitch = Mathf.Sin(t * pitchSpeed + phaseOffset * 1.2f) * pitchAmplitude;
            float yaw   = Mathf.Sin(t * yawSpeed + phaseOffset * 0.7f) * yawAmplitude;
            float roll  = Mathf.Cos(t * rollSpeed + phaseOffset * 1.5f) * rollAmplitude;

            Quaternion rockRot = Quaternion.Euler(pitch, yaw, roll);
            transform.localRotation = initialLocalRot * rockRot;
        }

        public void Configure(float heave, float pitch, float roll, float speedMultiplier = 1.0f)
        {
            heaveAmplitude = heave;
            pitchAmplitude = pitch;
            rollAmplitude = roll;
            heaveSpeed *= speedMultiplier;
            pitchSpeed *= speedMultiplier;
            rollSpeed *= speedMultiplier;
        }
    }
}
