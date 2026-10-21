using System.Collections.Generic;
using UnityEngine;

namespace IdleFishing.Aquarium
{
    public class SwimmingFishVisual : MonoBehaviour
    {
        private static readonly List<SwimmingFishVisual> allActiveFish = new List<SwimmingFishVisual>();

        public enum DepthLayer
        {
            Bottom,
            MidWater,
            UpperWater,
            FullRange
        }

        private Bounds tankBounds;
        private Vector3 currentTarget;
        private Transform visualChild;
        private Quaternion baseRotation = Quaternion.identity;

        // Dynamic Locomotion Parameters
        private float cruiseSpeed = 0.40f;
        private float currentSpeed = 0.40f;
        private float turnSpeed = 2.4f;
        private float tailFrequency = 4.5f;
        private float tailAmplitude = 9.0f;
        private float pitchAmplitude = 1.0f;
        private float timer = 0f;

        // Personality & Depth Preferences
        private DepthLayer depthLayer = DepthLayer.FullRange;
        private float glideTimer = 0f;
        private float glideDuration = 0f;
        private bool isGliding = false;
        private float timeToNextGlide = 5f;

        // Box Perimeter Orbiting System (Ensures fish swim all AROUND the box instead of clumping in the center)
        private int orbitDirection = 1; // 1 = Clockwise, -1 = Counter-Clockwise
        private int currentCornerIndex = 0; // 0=Front-Left, 1=Front-Right, 2=Back-Right, 3=Back-Left
        private float orbitOffset = 0f; // Multi-lane staggered offset from glass
        private float targetTimer = 0f;
        private float targetTimeout = 14.0f;
        private int cornersVisited = 0;

        public void Initialize(Bounds bounds, string speciesId = null, bool isBoss = false, float weightKg = 2.0f)
        {
            tankBounds = bounds;
            visualChild = transform.childCount > 0 ? transform.GetChild(0) : transform;
            baseRotation = visualChild.localRotation;
            timer = Random.Range(0f, 100f);

            orbitDirection = (Random.value < 0.5f) ? 1 : -1;
            orbitOffset = Random.Range(0.0f, 0.45f);

            ConfigureSpeciesBehavior(speciesId, isBoss, weightKg);

            if (!allActiveFish.Contains(this))
            {
                allActiveFish.Add(this);
            }

            InitPerimeterCorner();
            PickNextCornerTarget();
        }

        // Backward compatibility overload
        public void Initialize(Bounds bounds, float swimSpeedMultiplier = 1.0f)
        {
            Initialize(bounds, null, false, 2.0f);
            cruiseSpeed *= swimSpeedMultiplier;
            currentSpeed = cruiseSpeed;
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

            Initialize(bounds, null, false, 2.0f);
        }

        private void OnDestroy()
        {
            allActiveFish.Remove(this);
        }

        private void OnDisable()
        {
            allActiveFish.Remove(this);
        }

        private void ConfigureSpeciesBehavior(string speciesId, bool isBoss, float weightKg)
        {
            string id = speciesId?.ToLowerInvariant() ?? "";

            if (isBoss || id.Contains("shark") || id.Contains("ray"))
            {
                // Large Apex Bosses: Majestic wide perimeter patrolling
                cruiseSpeed = Random.Range(0.38f, 0.50f);
                turnSpeed = Random.Range(1.8f, 2.6f);
                tailFrequency = Random.Range(2.4f, 3.4f);
                tailAmplitude = Random.Range(9f, 13f);
                pitchAmplitude = 0.7f;
                depthLayer = Random.value < 0.5f ? DepthLayer.MidWater : DepthLayer.FullRange;
                timeToNextGlide = Random.Range(5f, 9f);
            }
            else if (id == "pufferfish")
            {
                // Pufferfish: Cute substrate & mid-depth flutter
                cruiseSpeed = Random.Range(0.24f, 0.32f);
                turnSpeed = Random.Range(2.2f, 3.2f);
                tailFrequency = Random.Range(6.0f, 8.0f);
                tailAmplitude = Random.Range(4.5f, 6.5f);
                pitchAmplitude = 1.8f;
                depthLayer = DepthLayer.Bottom;
                timeToNextGlide = Random.Range(3f, 6f);
            }
            else if (id == "snow_crab" || id == "lobster")
            {
                // Bottom grazers near the sandbed
                cruiseSpeed = Random.Range(0.22f, 0.30f);
                turnSpeed = Random.Range(1.8f, 2.5f);
                tailFrequency = Random.Range(2.5f, 3.8f);
                tailAmplitude = Random.Range(2.5f, 4.0f);
                pitchAmplitude = 0.3f;
                depthLayer = DepthLayer.Bottom;
                timeToNextGlide = Random.Range(4f, 8f);
            }
            else if (id == "clownfish" || id == "green_chromis" || id == "blue_tang" || id == "yellow_tang" || id == "longnose_butterfly")
            {
                // Agile reef fish: Active, lively swimming along glass
                cruiseSpeed = Random.Range(0.42f, 0.56f);
                turnSpeed = Random.Range(3.0f, 4.2f);
                tailFrequency = Random.Range(5.5f, 7.5f);
                tailAmplitude = Random.Range(8.5f, 12f);
                pitchAmplitude = 1.4f;
                depthLayer = Random.value < 0.65f ? DepthLayer.Bottom : DepthLayer.MidWater;
                timeToNextGlide = Random.Range(3f, 6f);
            }
            else if (id == "herring" || id == "arctic_tuna")
            {
                // Sleek pelagic / upper cruisers: Fast, sweeping laps
                cruiseSpeed = Random.Range(0.46f, 0.60f);
                turnSpeed = Random.Range(2.2f, 3.2f);
                tailFrequency = Random.Range(4.2f, 6.0f);
                tailAmplitude = Random.Range(7.5f, 11f);
                pitchAmplitude = 1.0f;
                depthLayer = DepthLayer.UpperWater;
                timeToNextGlide = Random.Range(4f, 7f);
            }
            else // salmon, cod, default
            {
                // Classic medium cruising fish
                cruiseSpeed = Random.Range(0.38f, 0.52f);
                turnSpeed = Random.Range(2.2f, 3.0f);
                tailFrequency = Random.Range(3.5f, 5.0f);
                tailAmplitude = Random.Range(7.5f, 11f);
                pitchAmplitude = 1.0f;
                depthLayer = DepthLayer.MidWater;
                timeToNextGlide = Random.Range(4f, 7f);
            }

            // Slight natural weight scaling
            float weightMod = Mathf.Clamp(1.0f - (weightKg - 2.0f) * 0.02f, 0.85f, 1.15f);
            cruiseSpeed *= weightMod;
            currentSpeed = cruiseSpeed;
        }

        private void InitPerimeterCorner()
        {
            float midX = tankBounds.center.x;
            float midZ = tankBounds.center.z;
            Vector3 pos = transform.position;

            // Determine nearest starting corner based on current position
            if (pos.x < midX && pos.z < midZ) currentCornerIndex = 0; // Front-Left
            else if (pos.x >= midX && pos.z < midZ) currentCornerIndex = 1; // Front-Right
            else if (pos.x >= midX && pos.z >= midZ) currentCornerIndex = 2; // Back-Right
            else currentCornerIndex = 3; // Back-Left

            // Advance to next corner in orbit direction to start cruising around the perimeter
            currentCornerIndex = (currentCornerIndex + orbitDirection + 4) % 4;
        }

        public void PickNewTarget()
        {
            PickNextCornerTarget();
        }

        private void PickNextCornerTarget()
        {
            targetTimer = 0f;
            cornersVisited++;

            // Every 4-8 corners (1-2 full perimeter laps), 25% chance to reverse orbit direction
            if (cornersVisited > 4 && Random.value < 0.25f)
            {
                orbitDirection = -orbitDirection;
                cornersVisited = 0;
            }

            float xMargin = 0.42f + orbitOffset;
            float zMargin = 0.42f + orbitOffset;
            float xMin = tankBounds.min.x + xMargin;
            float xMax = tankBounds.max.x - xMargin;
            float zMin = tankBounds.min.z + zMargin;
            float zMax = tankBounds.max.z - zMargin;

            // 4 Perimeter Corners around the 3D Box:
            // Corner 0: Front-Left (minX, minZ)
            // Corner 1: Front-Right (maxX, minZ)  -> Corridor 0->1 cruises across the entire Front Viewing Glass!
            // Corner 2: Back-Right (maxX, maxZ)   -> Corridor 1->2 cruises along the Right Glass Wall!
            // Corner 3: Back-Left (minX, maxZ)    -> Corridor 2->3 cruises along the Back Wall!
            // Corner 3->0: cruises along the Left Glass Wall!
            // All 4 corridors patrol AROUND the perimeter, completely freeing up the center!
            float tx, tz;
            switch (currentCornerIndex)
            {
                case 0: // Front-Left
                    tx = xMin + Random.Range(0f, 0.45f);
                    tz = zMin + Random.Range(0f, 0.35f);
                    break;
                case 1: // Front-Right
                    tx = xMax - Random.Range(0f, 0.45f);
                    tz = zMin + Random.Range(0f, 0.35f);
                    break;
                case 2: // Back-Right
                    tx = xMax - Random.Range(0f, 0.45f);
                    tz = zMax - Random.Range(0f, 0.35f);
                    break;
                default: // Back-Left
                    tx = xMin + Random.Range(0f, 0.45f);
                    tz = zMax - Random.Range(0f, 0.35f);
                    break;
            }

            // Height based on depth layer (substrate floor vs mid-water vs upper pelagic surface)
            float ty;
            float totalH = tankBounds.size.y;
            switch (depthLayer)
            {
                case DepthLayer.Bottom:
                    ty = Random.Range(tankBounds.min.y + 0.15f, tankBounds.min.y + totalH * 0.32f);
                    break;
                case DepthLayer.UpperWater:
                    ty = Random.Range(tankBounds.min.y + totalH * 0.65f, tankBounds.max.y - 0.15f);
                    break;
                case DepthLayer.MidWater:
                    ty = Random.Range(tankBounds.min.y + totalH * 0.28f, tankBounds.min.y + totalH * 0.72f);
                    break;
                default: // FullRange
                    ty = Random.Range(tankBounds.min.y + 0.20f, tankBounds.max.y - 0.20f);
                    break;
            }

            currentTarget = new Vector3(tx, ty, tz);

            // Timeout scaled by distance so fish never get cut off mid-lap!
            float dist = Vector3.Distance(transform.position, currentTarget);
            targetTimeout = Mathf.Max(6.5f, (dist / Mathf.Max(0.25f, cruiseSpeed)) + 3.5f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            timer += dt;
            targetTimer += dt;

            // 1. Waypoint arrival or timeout: advance to the next perimeter corner around the box!
            Vector3 toTarget = currentTarget - transform.position;
            if (toTarget.sqrMagnitude < 0.36f || targetTimer >= targetTimeout) // within 0.60m
            {
                currentCornerIndex = (currentCornerIndex + orbitDirection + 4) % 4;
                PickNextCornerTarget();
                toTarget = currentTarget - transform.position;
            }

            // 2. Gliding / Coasting state management (for lifelike pacing variation)
            glideTimer += dt;
            if (!isGliding && glideTimer >= timeToNextGlide)
            {
                isGliding = true;
                glideTimer = 0f;
                glideDuration = Random.Range(1.2f, 2.5f);
            }
            else if (isGliding && glideTimer >= glideDuration)
            {
                isGliding = false;
                glideTimer = 0f;
                timeToNextGlide = Random.Range(4f, 8f);
            }

            float targetSpeed = isGliding ? (cruiseSpeed * 0.45f) : cruiseSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, dt * 2.0f);

            // 3. Gentle glass wall guide
            Vector3 wallDeflection = Vector3.zero;
            float bufferX = 0.35f;
            float bufferZ = 0.35f;
            float bufferY = 0.20f;

            if (transform.position.x < tankBounds.min.x + bufferX)
                wallDeflection.x += (tankBounds.min.x + bufferX - transform.position.x) / bufferX * 0.8f;
            else if (transform.position.x > tankBounds.max.x - bufferX)
                wallDeflection.x -= (transform.position.x - (tankBounds.max.x - bufferX)) / bufferX * 0.8f;

            if (transform.position.z < tankBounds.min.z + bufferZ)
                wallDeflection.z += (tankBounds.min.z + bufferZ - transform.position.z) / bufferZ * 0.8f;
            else if (transform.position.z > tankBounds.max.z - bufferZ)
                wallDeflection.z -= (transform.position.z - (tankBounds.max.z - bufferZ)) / bufferZ * 0.8f;

            if (transform.position.y < tankBounds.min.y + bufferY)
                wallDeflection.y += (tankBounds.min.y + bufferY - transform.position.y) / bufferY * 0.8f;
            else if (transform.position.y > tankBounds.max.y - bufferY)
                wallDeflection.y -= (transform.position.y - (tankBounds.max.y - bufferY)) / bufferY * 0.8f;

            // 4. Inter-Fish Separation (Soft dispersion to avoid collision when crossing)
            Vector3 fishSeparation = Vector3.zero;
            for (int i = 0; i < allActiveFish.Count; i++)
            {
                var other = allActiveFish[i];
                if (other != null && other != this)
                {
                    Vector3 diff = transform.position - other.transform.position;
                    float dist = diff.magnitude;
                    if (dist < 0.45f && dist > 0.001f)
                    {
                        fishSeparation += (diff / dist) * (1.0f - dist / 0.45f);
                    }
                }
            }

            // 5. Combine forces into desired steering heading
            Vector3 desiredDir = toTarget.normalized;
            if (wallDeflection.sqrMagnitude > 0.001f)
            {
                desiredDir = (desiredDir + wallDeflection * 0.9f).normalized;
            }
            if (fishSeparation.sqrMagnitude > 0.001f)
            {
                desiredDir = (desiredDir + fishSeparation * 0.45f).normalized;
            }

            // 6. Smooth rotation towards heading
            if (desiredDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desiredDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * turnSpeed);

                float turnAngle = Quaternion.Angle(transform.rotation, targetRot);
                float turnMod = Mathf.Lerp(1.0f, 0.75f, Mathf.Clamp01(turnAngle / 100f));
                transform.position += transform.forward * (currentSpeed * turnMod * dt);
            }
            else
            {
                transform.position += transform.forward * (currentSpeed * dt);
            }

            // 7. Safety clamp inside tank volume
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, tankBounds.min.x + 0.15f, tankBounds.max.x - 0.15f);
            p.y = Mathf.Clamp(p.y, tankBounds.min.y + 0.12f, tankBounds.max.y - 0.12f);
            p.z = Mathf.Clamp(p.z, tankBounds.min.z + 0.15f, tankBounds.max.z - 0.15f);
            transform.position = p;

            // 8. Natural waggle & subtle vertical breathing motion
            if (visualChild != null && visualChild != transform)
            {
                float wiggleMod = isGliding ? 0.35f : 1.0f;
                float wiggle = Mathf.Sin(timer * tailFrequency) * (tailAmplitude * wiggleMod);
                float pitchWiggle = Mathf.Cos(timer * tailFrequency * 0.5f) * (pitchAmplitude * wiggleMod);
                visualChild.localRotation = baseRotation * Quaternion.Euler(pitchWiggle, wiggle, 0f);
            }
        }
    }
}
