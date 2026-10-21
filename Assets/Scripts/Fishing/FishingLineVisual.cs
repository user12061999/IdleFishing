using System.Collections;
using UnityEngine;
using IdleFishing.Audio;
using IdleFishing.VFX;

namespace IdleFishing.Fishing
{
    public enum FishingLinePhase
    {
        Resting,       // Cần nghỉ, phao treo dưới đầu cần
        CastingOut,    // Vung cần, phao bay trên không theo đường cong parabol
        Drifting,      // Phao nổi dập dềnh trên mặt nước, sóng lan tỏa
        Nibbling,      // Cá rỉa mồi, phao nhấp nháy tạo gợn sóng nhanh
        Strike,        // Cá đớp mồi giật mạnh, phao chìm, cần uốn cong
        ReelingIn      // Kéo cần thu dây, phao và cá rẽ nước tạo vệt sóng CFXR
    }

    public class FishingLineVisual : MonoBehaviour
    {
        [Header("Transforms")]
        [SerializeField] private Transform rodTransform;
        [SerializeField] private Transform rodTip;
        [SerializeField] private Vector3 targetWaterPos;

        [Header("Line Renderer Settings")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private int lineSegments = 16;
        [SerializeField] private float lineWidthTip = 0.024f;
        [SerializeField] private float lineWidthBobber = 0.012f;

        [Header("Bobber (Fishing Float)")]
        [SerializeField] private GameObject bobberRoot;
        [SerializeField] private Transform bobberVisual;
        [SerializeField] private float waterLevelY = 0.05f;

        [Header("Current Visual Phase")]
        [SerializeField] private FishingLinePhase currentPhase = FishingLinePhase.Resting;

        private Vector3 currentBobberPos;
        private Vector3 castStartPos;
        private Vector3 castTargetPos;
        private float castTotalTime = 0.8f;

        [Header("CFXR Water Ripple Tuning & Randomization")]
        [SerializeField] [Range(0f, 1f)] private float splashdownRippleChance = 0.50f;
        [SerializeField] [Range(0f, 1f)] private float driftingRippleChance = 0.28f;
        [SerializeField] private float minDriftRippleInterval = 4.0f;
        [SerializeField] private float maxDriftRippleInterval = 7.5f;
        [SerializeField] [Range(0f, 1f)] private float reelingRippleChance = 0.35f;
        [SerializeField] private float minReelRippleInterval = 0.70f;
        [SerializeField] private float globalRippleCooldown = 0.65f;

        // Điều phối nhịp điệu toàn cục để các Stickman không nổ sóng đè lên nhau
        private static float s_lastGlobalRippleTime = -999f;

        private float reelRippleTimer = 0f;
        private Quaternion initialRodRotation = Quaternion.identity;
        private Coroutine activeAnimationCoroutine;

        public FishingLinePhase Phase => currentPhase;
        public Vector3 CurrentBobberPos => currentBobberPos;
        public Transform RodTip => rodTip;

        private void Awake()
        {
            if (rodTransform != null)
            {
                initialRodRotation = rodTransform.localRotation;
            }

            SetupLineRenderer();
            SetupBobber();
        }

        private void Start()
        {
            ResetToRest();
        }

        private void LateUpdate()
        {
            UpdateLineAndBobber();
        }

        public void Setup(Transform rod, Transform tip, Vector3 targetOceanSpot)
        {
            rodTransform = rod;
            rodTip = tip;
            targetWaterPos = targetOceanSpot;
            targetWaterPos.y = waterLevelY;

            if (rodTransform != null)
            {
                initialRodRotation = rodTransform.localRotation;
            }

            SetupLineRenderer();
            SetupBobber();
            ResetToRest();
        }

        private void SetupLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            lineRenderer.positionCount = lineSegments;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = lineWidthTip;
            lineRenderer.endWidth = lineWidthBobber;

            // Semi-translucent monofilament nylon fishing line
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                       ?? Shader.Find("Particles/Standard Unlit")
                       ?? Shader.Find("Sprites/Default");
            Material lineMat = new Material(s);
            Color lineColor = new Color(0.85f, 0.95f, 1.0f, 0.85f);
            if (lineMat.HasProperty("_BaseColor")) lineMat.SetColor("_BaseColor", lineColor);
            else if (lineMat.HasProperty("_Color")) lineMat.color = lineColor;

            lineRenderer.sharedMaterial = lineMat;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }

        private void SetupBobber()
        {
            if (bobberRoot != null) return;

            bobberRoot = new GameObject("Fishing_Bobber");
            bobberRoot.transform.SetParent(transform, true);
            bobberVisual = new GameObject("Bobber_Visual").transform;
            bobberVisual.SetParent(bobberRoot.transform, false);

            // 1. Phao tròn nửa đỏ trên
            GameObject capTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            capTop.name = "Cap_Red";
            capTop.transform.SetParent(bobberVisual, false);
            capTop.transform.localPosition = new Vector3(0, 0.05f, 0);
            capTop.transform.localScale = new Vector3(0.14f, 0.12f, 0.14f);
            SetObjectColor(capTop, new Color(0.95f, 0.22f, 0.15f)); // Đỏ cam
            RemoveCollider(capTop);

            // 2. Nửa trắng dưới ngâm trong nước
            GameObject capBottom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            capBottom.name = "Body_White";
            capBottom.transform.SetParent(bobberVisual, false);
            capBottom.transform.localPosition = new Vector3(0, -0.04f, 0);
            capBottom.transform.localScale = new Vector3(0.13f, 0.12f, 0.13f);
            SetObjectColor(capBottom, new Color(0.96f, 0.98f, 1.0f)); // Trắng
            RemoveCollider(capBottom);

            // 3. Vành đai giữa màu sẫm
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring_Divider";
            ring.transform.SetParent(bobberVisual, false);
            ring.transform.localPosition = new Vector3(0, 0.01f, 0);
            ring.transform.localScale = new Vector3(0.145f, 0.015f, 0.145f);
            SetObjectColor(ring, new Color(0.15f, 0.18f, 0.22f));
            RemoveCollider(ring);

            // 4. Ăng-ten tăm phao có chóp vàng
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Antenna_Stem";
            stem.transform.SetParent(bobberVisual, false);
            stem.transform.localPosition = new Vector3(0, 0.14f, 0);
            stem.transform.localScale = new Vector3(0.018f, 0.08f, 0.018f);
            SetObjectColor(stem, new Color(0.2f, 0.2f, 0.2f));
            RemoveCollider(stem);

            GameObject bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bead.name = "Antenna_Tip";
            bead.transform.SetParent(bobberVisual, false);
            bead.transform.localPosition = new Vector3(0, 0.22f, 0);
            bead.transform.localScale = new Vector3(0.045f, 0.045f, 0.045f);
            SetObjectColor(bead, new Color(1.0f, 0.85f, 0.2f)); // Vàng chanh rực rỡ
            RemoveCollider(bead);
        }

        public void StartCast(Vector3 targetSpot, float castDuration)
        {
            if (activeAnimationCoroutine != null) StopCoroutine(activeAnimationCoroutine);
            targetWaterPos = targetSpot;
            targetWaterPos.y = waterLevelY;
            activeAnimationCoroutine = StartCoroutine(CastAnimationRoutine(castDuration));
        }

        private IEnumerator CastAnimationRoutine(float totalCastDuration)
        {
            currentPhase = FishingLinePhase.CastingOut;
            castStartPos = GetTipPosition();
            castTargetPos = targetWaterPos;
            castTotalTime = Mathf.Min(0.85f, totalCastDuration * 0.25f);

            // Giai đoạn 1: Lấy đà vung cần ra sau và vụt mạnh ra trước
            float windUpTime = 0.28f;
            float whipTime = castTotalTime - windUpTime;

            // A. Lấy đà (Wind-up): Ngả cần ra sau
            float timer = 0f;
            while (timer < windUpTime)
            {
                timer += Time.deltaTime;
                float t = timer / windUpTime;
                if (rodTransform != null)
                {
                    rodTransform.localRotation = initialRodRotation * Quaternion.Euler(-22f * Mathf.Sin(t * Mathf.PI * 0.5f), 0, 0);
                }
                currentBobberPos = GetTipPosition() + Vector3.down * 0.2f;
                yield return null;
            }

            // B. Vụt cần mạnh ra phía biển (Forward Whip) & Phao bay ra xa
            timer = 0f;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayRodCastWhoosh(transform.position);
            }

            while (timer < whipTime)
            {
                timer += Time.deltaTime;
                float t = timer / whipTime;

                // Rod vung mạnh về phía trước rồi hồi lại
                if (rodTransform != null)
                {
                    float whipAngle = Mathf.Lerp(38f, 5f, t);
                    rodTransform.localRotation = initialRodRotation * Quaternion.Euler(whipAngle, 0, 0);
                }

                // Phao bay theo quỹ đạo parabol cầu vồng
                Vector3 horizontal = Vector3.Lerp(castStartPos, castTargetPos, t);
                float arcHeight = Mathf.Sin(t * Mathf.PI) * 1.8f;
                currentBobberPos = new Vector3(horizontal.x, Mathf.Lerp(castStartPos.y, castTargetPos.y, t) + arcHeight, horizontal.z);

                yield return null;
            }

            // Giai đoạn 2: Phao chạm mặt nước -> Sóng tương tác mặt nước & xác suất CFXR
            currentBobberPos = castTargetPos;
            currentPhase = FishingLinePhase.Drifting;

            if (IdleFishing.World.OceanWaterFlowController.Instance != null)
            {
                IdleFishing.World.OceanWaterFlowController.Instance.RegisterRipple(currentBobberPos, 1.9f, 0.95f, 12.0f, 1.8f);
            }

            if (UnityEngine.Random.value < splashdownRippleChance)
            {
                TrySpawnWaterRipple(UnityEngine.Random.Range(0.85f, 1.15f));
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWaterPlop(currentBobberPos);
            }

            // Giai đoạn 3: Phao trôi nổi dập dềnh theo sóng biển + dòng nước chảy và tạo sóng thưa thớt, ngẫu nhiên
            float driftDuration = Mathf.Max(1.0f, totalCastDuration - castTotalTime - 1.2f);
            float driftTimer = 0f;
            float nextDriftRippleInterval = UnityEngine.Random.Range(minDriftRippleInterval, maxDriftRippleInterval);
            float driftRippleTimer = UnityEngine.Random.Range(0f, 2.0f); // Offset ngẫu nhiên tránh trùng giờ giữa các cần thủ
            Vector3 currentDriftOffset = Vector3.zero;

            while (driftTimer < driftDuration)
            {
                driftTimer += Time.deltaTime;

                // Trôi nhẹ theo dòng nước chảy (nhưng bị dây câu giữ lại trong bán kính nhỏ)
                Vector3 flowVel = IdleFishing.World.OceanWaterFlowController.Instance != null
                    ? IdleFishing.World.OceanWaterFlowController.Instance.GetFlowVelocityAtPosition(castTargetPos)
                    : new Vector3(0.45f, 0f, 0.1f);
                currentDriftOffset = Vector3.ClampMagnitude(currentDriftOffset + flowVel * (Time.deltaTime * 0.28f), 0.55f);

                float swayX = Mathf.Sin(Time.time * 1.4f + transform.position.x) * 0.10f + currentDriftOffset.x;
                float swayZ = Mathf.Cos(Time.time * 1.2f + transform.position.x) * 0.08f + currentDriftOffset.z;
                Vector3 horizontalBobberPos = new Vector3(castTargetPos.x + swayX, waterLevelY, castTargetPos.z + swayZ);

                // Nhấp nhô khớp chính xác với độ cao sóng mặt nước 3D
                float waveY = IdleFishing.World.OceanWaterFlowController.Instance != null
                    ? IdleFishing.World.OceanWaterFlowController.Instance.GetWaveHeightAtPosition(horizontalBobberPos)
                    : (waterLevelY + Mathf.Sin(Time.time * 2.8f) * 0.038f);

                currentBobberPos = new Vector3(horizontalBobberPos.x, waveY, horizontalBobberPos.z);

                // Độ thở nhịp nhàng của đầu cần
                if (rodTransform != null)
                {
                    float breath = Mathf.Sin(Time.time * 2.5f) * 2.2f;
                    rodTransform.localRotation = initialRodRotation * Quaternion.Euler(breath, 0, 0);
                }

                // Phát sóng ngẫu nhiên, giãn cách dài trên từng stickman
                driftRippleTimer += Time.deltaTime;
                if (driftRippleTimer >= nextDriftRippleInterval)
                {
                    driftRippleTimer = 0f;
                    nextDriftRippleInterval = UnityEngine.Random.Range(minDriftRippleInterval, maxDriftRippleInterval);
                    if (IdleFishing.World.OceanWaterFlowController.Instance != null)
                    {
                        IdleFishing.World.OceanWaterFlowController.Instance.RegisterRipple(currentBobberPos, 1.4f, 0.65f, 11.0f, 1.6f);
                    }
                    if (UnityEngine.Random.value < driftingRippleChance)
                    {
                        TrySpawnWaterRipple(UnityEngine.Random.Range(0.7f, 0.95f));
                    }
                }

                yield return null;
            }

            // Giai đoạn 4: Cá rỉa mồi & giật đớp mồi (Nibble & Strike)
            yield return StartCoroutine(BiteStrikeRoutine());
        }

        private IEnumerator BiteStrikeRoutine()
        {
            currentPhase = FishingLinePhase.Nibbling;

            // 1. Nhấp rỉa mồi lần 1 (Gợn vòng sóng nhẹ trên shader mặt nước)
            currentBobberPos.y = waterLevelY - 0.08f;
            if (IdleFishing.World.OceanWaterFlowController.Instance != null)
            {
                IdleFishing.World.OceanWaterFlowController.Instance.RegisterRipple(currentBobberPos, 1.25f, 0.7f, 13.0f, 1.3f);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIPop(currentBobberPos);
            if (rodTransform != null) rodTransform.localRotation = initialRodRotation * Quaternion.Euler(8f, 0, 0);
            yield return new WaitForSeconds(0.22f);

            // Nổi lại
            currentBobberPos.y = waterLevelY + 0.02f;
            if (rodTransform != null) rodTransform.localRotation = initialRodRotation;
            yield return new WaitForSeconds(0.28f);

            // 2. Nhấp rỉa mồi lần 2
            currentBobberPos.y = waterLevelY - 0.12f;
            if (IdleFishing.World.OceanWaterFlowController.Instance != null)
            {
                IdleFishing.World.OceanWaterFlowController.Instance.RegisterRipple(currentBobberPos, 1.45f, 0.8f, 13.5f, 1.4f);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIPop(currentBobberPos);
            if (rodTransform != null) rodTransform.localRotation = initialRodRotation * Quaternion.Euler(14f, 0, 0);
            yield return new WaitForSeconds(0.20f);

            // 3. CÁ ĐỚP MỒI THẬT (STRIKE!): Điểm nhấn quan trọng nhất -> Tạo sóng dứt khoát!
            currentPhase = FishingLinePhase.Strike;
            currentBobberPos.y = waterLevelY - 0.22f;
            TrySpawnWaterRipple(1.25f, ignoreGlobalCooldown: true);

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSplash(currentBobberPos, 0.85f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySplash(currentBobberPos);
            }
            if (IdleFishing.CameraControl.MobilePortraitCameraController.Instance != null)
            {
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerShake(0.18f, 0.22f);
            }

            float strikeHoldTime = 0.5f;
            float strikeTimer = 0f;
            while (strikeTimer < strikeHoldTime)
            {
                strikeTimer += Time.deltaTime;
                // Cần rung bần bật vì cá giãy mạnh
                if (rodTransform != null)
                {
                    float vibration = Mathf.Sin(Time.time * 35f) * 3.5f;
                    rodTransform.localRotation = initialRodRotation * Quaternion.Euler(28f + vibration, 0, 0);
                }
                yield return null;
            }
        }

        public void StartReeling(Vector3 landingPos, float duration = 1.1f)
        {
            if (activeAnimationCoroutine != null) StopCoroutine(activeAnimationCoroutine);
            activeAnimationCoroutine = StartCoroutine(ReelingAnimationRoutine(landingPos, duration));
        }

        private IEnumerator ReelingAnimationRoutine(Vector3 landingPos, float duration)
        {
            currentPhase = FishingLinePhase.ReelingIn;
            Vector3 reelStartPos = currentBobberPos;
            Vector3 dockEdgePos = transform.position + transform.forward * 1.8f;
            dockEdgePos.y = waterLevelY;

            float timer = 0f;
            reelRippleTimer = 0f;
            float reelAudioTimer = 0f;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayReelCrank(transform.position);
            }

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;

                // Tiếng máy câu quay rè rè cọt kẹt
                reelAudioTimer += Time.deltaTime;
                if (reelAudioTimer >= 0.38f)
                {
                    reelAudioTimer = 0f;
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayReelCrank(transform.position);
                    }
                }

                // Cần câu nhấp giật liên hồi (Pumping reel action)
                if (rodTransform != null)
                {
                    float pump = Mathf.Sin(timer * 14f) * 16f;
                    rodTransform.localRotation = initialRodRotation * Quaternion.Euler(15f + pump, 0, 0);
                }

                // Phao và dây câu bị kéo lướt qua mặt nước về phía bến
                currentBobberPos = Vector3.Lerp(reelStartPos, dockEdgePos, t);
                currentBobberPos.y = waterLevelY + Mathf.Sin(timer * 20f) * 0.03f;

                // Thỉnh thoảng mới xuất hiện 1 gợn rẽ nước nhỏ lăn tăn theo xác suất ngẫu nhiên
                reelRippleTimer += Time.deltaTime;
                if (reelRippleTimer >= minReelRippleInterval)
                {
                    reelRippleTimer = 0f;
                    if (UnityEngine.Random.value < reelingRippleChance)
                    {
                        TrySpawnWaterRipple(UnityEngine.Random.Range(0.6f, 0.8f));
                    }
                }

                yield return null;
            }

            // Kết thúc kéo cá, thu phao về vị trí nghỉ
            ResetToRest();
        }

        public void ResetToRest()
        {
            if (activeAnimationCoroutine != null)
            {
                StopCoroutine(activeAnimationCoroutine);
                activeAnimationCoroutine = null;
            }

            currentPhase = FishingLinePhase.Resting;
            if (rodTransform != null)
            {
                rodTransform.localRotation = initialRodRotation;
            }

            currentBobberPos = GetTipPosition() + Vector3.down * 0.35f;
            if (bobberRoot != null)
            {
                bobberRoot.transform.position = currentBobberPos;
            }
        }

        private bool TrySpawnWaterRipple(float scale = 1.0f, bool ignoreGlobalCooldown = false)
        {
            if (!ignoreGlobalCooldown && Time.time - s_lastGlobalRippleTime < globalRippleCooldown)
            {
                return false;
            }

            Vector3 ripplePos = currentBobberPos;
            ripplePos.y = waterLevelY;

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnCFXRWaterRipple(ripplePos, scale);
                s_lastGlobalRippleTime = Time.time;
                return true;
            }
            return false;
        }

        private void UpdateLineAndBobber()
        {
            if (lineRenderer == null) return;

            if (currentPhase == FishingLinePhase.Resting)
            {
                currentBobberPos = GetTipPosition() + Vector3.down * 0.35f;
            }

            Vector3 startPos = GetTipPosition();
            Vector3 endPos = currentBobberPos;

            if (bobberRoot != null)
            {
                bobberRoot.transform.position = endPos;

                // Xoay phao hơi nghiêng tự nhiên theo nhịp sóng
                if (bobberVisual != null)
                {
                    if (currentPhase == FishingLinePhase.Drifting || currentPhase == FishingLinePhase.Nibbling)
                    {
                        float tiltX = Mathf.Sin(Time.time * 2.8f) * 8f;
                        float tiltZ = Mathf.Cos(Time.time * 2.2f) * 6f;
                        bobberVisual.localRotation = Quaternion.Euler(tiltX, 0, tiltZ);
                    }
                    else if (currentPhase == FishingLinePhase.Strike)
                    {
                        bobberVisual.localRotation = Quaternion.Euler(25f, 0, 15f);
                    }
                    else
                    {
                        bobberVisual.localRotation = Quaternion.identity;
                    }
                }
            }

            // Tính toán đường cong dây câu (Curved Bezier / Catenary Slack)
            float dist = Vector3.Distance(startPos, endPos);
            float sagAmount = 0f;

            if (currentPhase == FishingLinePhase.Drifting)
            {
                // Dây câu chùng võng tự nhiên do trọng lực
                sagAmount = Mathf.Clamp(dist * 0.08f, 0.15f, 0.45f);
            }
            else if (currentPhase == FishingLinePhase.CastingOut)
            {
                sagAmount = -0.1f; // Bay căng trên không
            }
            else if (currentPhase == FishingLinePhase.Strike || currentPhase == FishingLinePhase.ReelingIn)
            {
                // Căng như dây đàn, rung tần số cao
                sagAmount = Mathf.Sin(Time.time * 40f) * 0.02f;
            }

            for (int i = 0; i < lineSegments; i++)
            {
                float t = (float)i / (lineSegments - 1);
                Vector3 point = Vector3.Lerp(startPos, endPos, t);

                // Parabolic sag
                float sag = Mathf.Sin(t * Mathf.PI) * sagAmount;
                point.y -= sag;

                // Gió thổi nhẹ đung đưa dây
                if (currentPhase == FishingLinePhase.Drifting)
                {
                    float wind = Mathf.Sin(Time.time * 2.0f + t * 3.0f) * 0.025f * Mathf.Sin(t * Mathf.PI);
                    point.x += wind;
                }

                lineRenderer.SetPosition(i, point);
            }
        }

        private Vector3 GetTipPosition()
        {
            if (rodTip != null) return rodTip.position;
            if (rodTransform != null) return rodTransform.position + rodTransform.up * 1.0f;
            return transform.position + Vector3.up * 1.5f + transform.forward * 0.8f;
        }

        private static void SetObjectColor(GameObject obj, Color color)
        {
            var r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = Application.isPlaying ? r.material : r.sharedMaterial;
                if (mat != null)
                {
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                    else if (mat.HasProperty("_Color")) mat.color = color;
                }
            }
        }

        private static void RemoveCollider(GameObject obj)
        {
            var col = obj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }
    }
}
