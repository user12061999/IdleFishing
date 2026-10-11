using UnityEngine;

namespace IdleFishing.Aquarium
{
    public class SwimmingFishVisual : MonoBehaviour
    {
        private Bounds tankBounds;
        private Vector3 currentTarget;
        private float swimSpeed = 1.4f;
        private float turnSpeed = 3.5f;
        private float tailFrequency = 8f;
        private float tailAmplitude = 12f;
        private float timer = 0f;
        private Transform visualChild;
        private Quaternion baseRotation = Quaternion.identity;

        public void Initialize(Bounds bounds, float swimSpeedMultiplier = 1.0f)
        {
            tankBounds = bounds;
            visualChild = transform.childCount > 0 ? transform.GetChild(0) : transform;
            baseRotation = visualChild.localRotation;

            swimSpeed = Random.Range(1.1f, 1.8f) * swimSpeedMultiplier;
            turnSpeed = Random.Range(2.8f, 4.2f);
            tailFrequency = Random.Range(6f, 9.5f);
            tailAmplitude = Random.Range(8f, 14f);
            timer = Random.Range(0f, 10f); // Desynchronize fish wiggles

            PickNewTarget();
        }

        // Backward compatibility overload
        public void Initialize(Bounds bounds, Color fishColor, float scale = 0.5f)
        {
            transform.localScale = Vector3.one * scale;
            visualChild = transform.childCount > 0 ? transform.GetChild(0) : transform;
            baseRotation = visualChild.localRotation;

            foreach (var r in visualChild.GetComponentsInChildren<Renderer>())
            {
                if (r != null && r.material != null)
                {
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", fishColor);
                    else if (r.material.HasProperty("_Color")) r.material.color = fishColor;
                }
            }

            Initialize(bounds, 1.0f);
        }

        private void PickNewTarget()
        {
            float marginX = 0.35f;
            float marginY = 0.25f;
            float marginZ = 0.35f;

            float x = Random.Range(tankBounds.min.x + marginX, tankBounds.max.x - marginX);
            float y = Random.Range(tankBounds.min.y + marginY, tankBounds.max.y - marginY);
            float z = Random.Range(tankBounds.min.z + marginZ, tankBounds.max.z - marginZ);
            currentTarget = new Vector3(x, y, z);
        }

        private void Update()
        {
            timer += Time.deltaTime;

            // Move towards target
            Vector3 toTarget = currentTarget - transform.position;
            if (toTarget.magnitude < 0.35f)
            {
                PickNewTarget();
                return;
            }

            // Smooth rotation towards target heading
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
            }

            // Forward swimming motion
            transform.position += transform.forward * (swimSpeed * Time.deltaTime);

            // Clamp inside tank bounds to ensure no clipping outside glass
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, tankBounds.min.x + 0.15f, tankBounds.max.x - 0.15f);
            p.y = Mathf.Clamp(p.y, tankBounds.min.y + 0.15f, tankBounds.max.y - 0.15f);
            p.z = Mathf.Clamp(p.z, tankBounds.min.z + 0.15f, tankBounds.max.z - 0.15f);
            transform.position = p;

            // Fish tail waggle / wiggle animation relative to its base orientation
            if (visualChild != null && visualChild != transform)
            {
                float wiggle = Mathf.Sin(timer * tailFrequency) * tailAmplitude;
                visualChild.localRotation = baseRotation * Quaternion.Euler(0f, wiggle, 0f);
            }
        }
    }
}
