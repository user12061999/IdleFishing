using System;
using UnityEngine;
using IdleFishing.Fishing;
using IdleFishing.Logistics;
using IdleFishing.Market;

namespace IdleFishing.Characters
{
    public enum StickmanRole
    {
        SittingFisherman,
        FishWrangler,
        LogisticsTransporter,
        CustomerOrTourist
    }

    /// <summary>
    /// Procedural cartoon animator for EKstudio Stickman characters.
    /// Delivers smooth, anatomically accurate Hyper-Casual / Arcade Idle locomotion,
    /// multi-joint spine/limb follow-through, role-specific state animations, and
    /// serialized bind-pose protection against edit/play rotation compounding.
    /// </summary>
    [ExecuteAlways]
    public class StickmanCartoonVisual : MonoBehaviour
    {
        [Header("Role & Posture")]
        [SerializeField] private StickmanRole role = StickmanRole.CustomerOrTourist;
        [SerializeField] private bool autoDetectMovement = true;

        [Header("Proportions & Chubby Styling")]
        [SerializeField] private bool applyChubbyProportions = true;
        [SerializeField] private Vector3 chubbyRootScale = new Vector3(1.36f, 1.10f, 1.26f);
        [SerializeField] private Vector3 chubbyHeadScale = new Vector3(1.12f, 1.12f, 1.12f);

        [Header("Procedural Walk Tuning")]
        [SerializeField] private float walkFrequency = 9.2f;
        [SerializeField] private float legSwingAngle = 34.0f;
        [SerializeField] private float armSwingAngle = 28.0f;
        [SerializeField] private float bodyBounceHeight = 0.045f;
        [SerializeField] private float bodyWaddleAngle = 3.8f;
        [SerializeField] private float forwardLeanAngle = 7.0f;
        [SerializeField] private float squashStretchAmount = 0.055f;

        [Header("Equipment Sockets")]
        [SerializeField] private Transform rightHandSocket;
        [SerializeField] private Transform leftHandSocket;
        [SerializeField] private Transform backCarrySocket;
        [SerializeField] private Transform headSocket;

        // Bone references (serialized hidden for fast runtime startup without Hierarchy scan)
        [SerializeField, HideInInspector] private Transform visualRoot;
        [SerializeField, HideInInspector] private Transform hips;
        [SerializeField, HideInInspector] private Transform spine;
        [SerializeField, HideInInspector] private Transform spine1;
        [SerializeField, HideInInspector] private Transform neck;
        [SerializeField, HideInInspector] private Transform head;
        [SerializeField, HideInInspector] private Transform rightArm, leftArm;
        [SerializeField, HideInInspector] private Transform rightForeArm, leftForeArm;
        [SerializeField, HideInInspector] private Transform rightHand, leftHand;
        [SerializeField, HideInInspector] private Transform rightUpLeg, leftUpLeg;
        [SerializeField, HideInInspector] private Transform rightLeg, leftLeg;
        [SerializeField, HideInInspector] private Transform rightFoot, leftFoot;
        [SerializeField, HideInInspector] private Transform rIndex1, rMiddle1, rRing1, rPinky1, rThumb1, rThumb2;
        [SerializeField, HideInInspector] private Transform lIndex1, lMiddle1, lRing1, lPinky1, lThumb1, lThumb2;

        // Immutable Canonical FBX Bind Pose constants (ground truth for all Stickman characters)
        private static readonly Quaternion CanonicalHipsRot = Quaternion.Euler(90f, 0f, 0f);
        private static readonly Vector3 CanonicalHipsPos = new Vector3(-0.002f, 0.003f, 0.648f);
        private static readonly Quaternion CanonicalSpineRot = Quaternion.Euler(358.309f, 0f, 0f);
        private static readonly Quaternion CanonicalSpine1Rot = Quaternion.identity;
        private static readonly Quaternion CanonicalNeckRot = Quaternion.Euler(1.691f, 0f, 0f);
        private static readonly Quaternion CanonicalHeadRot = Quaternion.identity;
        private static readonly Quaternion CanonicalRightUpLegRot = Quaternion.Euler(0.635f, 0.003f, 180.269f);
        private static readonly Quaternion CanonicalLeftUpLegRot = Quaternion.Euler(0.544f, -0.003f, 179.731f);
        private static readonly Quaternion CanonicalRightLegRot = Quaternion.Euler(356.629f, 0f, 0f);
        private static readonly Quaternion CanonicalLeftLegRot = Quaternion.Euler(356.648f, 0f, 0f);
        private static readonly Quaternion CanonicalRightFootRot = Quaternion.Euler(54.085f, 344.638f, 356.085f);
        private static readonly Quaternion CanonicalLeftFootRot = Quaternion.Euler(54.384f, 15.211f, 3.913f);
        private static readonly Quaternion CanonicalRightArmRot = Quaternion.Euler(345.314f, 1.801f, 0.731f);
        private static readonly Quaternion CanonicalLeftArmRot = Quaternion.Euler(345.314f, 358.230f, 0.669f);
        private static readonly Quaternion CanonicalRightForeArmRot = Quaternion.Euler(0.000f, 0.000f, 4.893f);
        private static readonly Quaternion CanonicalLeftForeArmRot = Quaternion.Euler(0.000f, 0.000f, 354.462f);
        private static readonly Quaternion CanonicalRightHandRot = Quaternion.Euler(353.416f, 358.305f, 358.192f);
        private static readonly Quaternion CanonicalLeftHandRot = Quaternion.Euler(355.167f, 1.682f, 2.730f);

        // Immutable serialized bind-pose transforms (prevents compounding rotation bug)
        [SerializeField, HideInInspector] private bool hasBindPose = false;
        [SerializeField, HideInInspector] private Quaternion initHips, initSpine, initSpine1, initNeck, initHead;
        [SerializeField, HideInInspector] private Quaternion initRightArm, initLeftArm;
        [SerializeField, HideInInspector] private Quaternion initRightForeArm, initLeftForeArm;
        [SerializeField, HideInInspector] private Quaternion initRightHand, initLeftHand;
        [SerializeField, HideInInspector] private Quaternion initRightUpLeg, initLeftUpLeg;
        [SerializeField, HideInInspector] private Quaternion initRightLeg, initLeftLeg;
        [SerializeField, HideInInspector] private Quaternion initRightFoot, initLeftFoot;
        [SerializeField, HideInInspector] private Vector3 initHipsPos;
        [SerializeField, HideInInspector] private Vector3 initVisualScale = Vector3.one * 1.45f;

        // Runtime state
        private Vector3 lastWorldPos;
        private float walkCycle = 0f;
        private float currentSpeed = 0f;
        private float locomotionBlend = 0f;
        private float idlePhaseOffset = 0f;
        private float castSubTimer = 0f;
        private bool isKnockedOut = false;
        private float attackTimer = 0f;
        private float attackDuration = 0.28f;
        private bool isInitialized = false;

        // Cached gameplay worker references
        private FishermanWorker fishermanWorker;
        private FishingLineVisual fishingLineVisual;
        private FishWranglerWorker wranglerWorker;
        private LogisticsTransporterWorker transporterWorker;
        private CustomerNPC customerNPC;

        public Transform RightHandSocket => rightHandSocket != null ? rightHandSocket : rightHand;
        public Transform LeftHandSocket => leftHandSocket != null ? leftHandSocket : leftHand;
        public Transform BackCarrySocket => backCarrySocket != null ? backCarrySocket : (spine1 != null ? spine1 : spine);
        public Transform HeadSocket => headSocket != null ? headSocket : head;

        public void SetupRole(StickmanRole newRole)
        {
            role = newRole;
        }

        public void Initialize(bool forceCaptureBindPose = false)
        {
            visualRoot = transform;

            // Disable any built-in Animator so it never fights procedural bone rotations
            var parentAnims = GetComponentsInParent<Animator>(true);
            for (int i = 0; i < parentAnims.Length; i++)
            {
                if (parentAnims[i] != null) parentAnims[i].enabled = false;
            }
            var childAnims = GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < childAnims.Length; i++)
            {
                if (childAnims[i] != null) childAnims[i].enabled = false;
            }

            CacheBones(forceCaptureBindPose);
            lastWorldPos = transform.position;
            idlePhaseOffset = (Mathf.Abs(GetInstanceID()) % 97) * 0.19f;

            fishermanWorker = GetComponentInParent<FishermanWorker>();
            fishingLineVisual = GetComponentInParent<FishingLineVisual>();
            wranglerWorker = GetComponentInParent<FishWranglerWorker>();
            transporterWorker = GetComponentInParent<LogisticsTransporterWorker>();
            customerNPC = GetComponentInParent<CustomerNPC>();

            isInitialized = true;
        }

        private void Awake()
        {
            Initialize(false);
        }

        private void OnEnable()
        {
            if (!isInitialized || visualRoot == null || hips == null)
            {
                Initialize(false);
            }
            lastWorldPos = transform.position;
        }

        private void CacheBones(bool forceCaptureBindPose)
        {
            if (hips == null || rightArm == null || leftArm == null || rightUpLeg == null || rIndex1 == null)
            {
                var all = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    Transform t = all[i];
                    switch (t.name)
                    {
                        case "Hips": hips = t; break;
                        case "Spine": spine = t; break;
                        case "Spine1": spine1 = t; break;
                        case "Neck": neck = t; break;
                        case "Head": head = t; break;
                        case "RightArm": rightArm = t; break;
                        case "LeftArm": leftArm = t; break;
                        case "RightForeArm": rightForeArm = t; break;
                        case "LeftForeArm": leftForeArm = t; break;
                        case "RightHand": rightHand = t; break;
                        case "LeftHand": leftHand = t; break;
                        case "RightUpLeg": rightUpLeg = t; break;
                        case "LeftUpLeg": leftUpLeg = t; break;
                        case "RightLeg": rightLeg = t; break;
                        case "LeftLeg": leftLeg = t; break;
                        case "RightFoot": rightFoot = t; break;
                        case "LeftFoot": leftFoot = t; break;
                        case "RightHandIndex1": rIndex1 = t; break;
                        case "RightHandMiddle1": rMiddle1 = t; break;
                        case "RightHandRing1": rRing1 = t; break;
                        case "RightHandPinky1": rPinky1 = t; break;
                        case "RightHandThumb1": rThumb1 = t; break;
                        case "RightHandThumb2": rThumb2 = t; break;
                        case "LeftHandIndex1": lIndex1 = t; break;
                        case "LeftHandMiddle1": lMiddle1 = t; break;
                        case "LeftHandRing1": lRing1 = t; break;
                        case "LeftHandPinky1": lPinky1 = t; break;
                        case "LeftHandThumb1": lThumb1 = t; break;
                        case "LeftHandThumb2": lThumb2 = t; break;
                    }
                }
            }

            // Always bind to canonical FBX bind-pose constants so runtime/edit-mode posing can NEVER compound
            initHips = CanonicalHipsRot;
            initHipsPos = CanonicalHipsPos;
            initSpine = CanonicalSpineRot;
            initSpine1 = CanonicalSpine1Rot;
            initNeck = CanonicalNeckRot;
            initHead = CanonicalHeadRot;
            initRightArm = CanonicalRightArmRot;
            initLeftArm = CanonicalLeftArmRot;
            initRightForeArm = CanonicalRightForeArmRot;
            initLeftForeArm = CanonicalLeftForeArmRot;
            initRightHand = CanonicalRightHandRot;
            initLeftHand = CanonicalLeftHandRot;
            initRightUpLeg = CanonicalRightUpLegRot;
            initLeftUpLeg = CanonicalLeftUpLegRot;
            initRightLeg = CanonicalRightLegRot;
            initLeftLeg = CanonicalLeftLegRot;
            initRightFoot = CanonicalRightFootRot;
            initLeftFoot = CanonicalLeftFootRot;
            initVisualScale = chubbyRootScale;
            hasBindPose = true;

            // Ensure equipment sockets exist with world-aligned orientation when right hand is held forward
            if (rightHand != null)
            {
                var s = rightHand.Find("Socket_RightHand");
                if (s == null)
                {
                    var g = new GameObject("Socket_RightHand");
                    g.transform.SetParent(rightHand, false);
                    s = g.transform;
                }
                s.localPosition = new Vector3(-0.01f, 0.09f, -0.02f);
                s.localRotation = Quaternion.Euler(291.7f, 256.1f, 174.3f);
                rightHandSocket = s;
            }

            Transform backParent = spine1 != null ? spine1 : spine;
            if (backParent != null && backCarrySocket == null)
            {
                var s = backParent.Find("Socket_BackCarry");
                if (s == null && spine != null) s = spine.Find("Socket_BackCarry");
                if (s == null)
                {
                    var g = new GameObject("Socket_BackCarry");
                    g.transform.SetParent(backParent, false);
                    s = g.transform;
                }
                s.localPosition = new Vector3(0f, 0.12f, -0.22f);
                s.localRotation = Quaternion.identity;
                backCarrySocket = s;
            }
        }

        private float CurrentAnimTime
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return (float)UnityEditor.EditorApplication.timeSinceStartup + idlePhaseOffset;
                }
#endif
                return Time.time + idlePhaseOffset;
            }
        }

        private void ApplyChubbyBones()
        {
            if (!applyChubbyProportions) return;

            if (hips != null) hips.localScale = Vector3.one;
            if (spine != null) spine.localScale = Vector3.one;
            if (spine1 != null) spine1.localScale = Vector3.one;
            if (neck != null) neck.localScale = Vector3.one;
            if (head != null) head.localScale = chubbyHeadScale;
        }

        /// <summary>
        /// Restores all cached bones back to their canonical bind-pose local transforms.
        /// </summary>
        public void ResetToBindPose()
        {
            initHips = CanonicalHipsRot;
            initHipsPos = CanonicalHipsPos;
            initSpine = CanonicalSpineRot;
            initSpine1 = CanonicalSpine1Rot;
            initNeck = CanonicalNeckRot;
            initHead = CanonicalHeadRot;
            initRightArm = CanonicalRightArmRot;
            initLeftArm = CanonicalLeftArmRot;
            initRightForeArm = CanonicalRightForeArmRot;
            initLeftForeArm = CanonicalLeftForeArmRot;
            initRightHand = CanonicalRightHandRot;
            initLeftHand = CanonicalLeftHandRot;
            initRightUpLeg = CanonicalRightUpLegRot;
            initLeftUpLeg = CanonicalLeftUpLegRot;
            initRightLeg = CanonicalRightLegRot;
            initLeftLeg = CanonicalLeftLegRot;
            initRightFoot = CanonicalRightFootRot;
            initLeftFoot = CanonicalLeftFootRot;
            hasBindPose = true;

            if (hips != null)
            {
                hips.localPosition = CanonicalHipsPos;
                hips.localRotation = CanonicalHipsRot;
            }
            if (spine != null) spine.localRotation = CanonicalSpineRot;
            if (spine1 != null) spine1.localRotation = CanonicalSpine1Rot;
            if (neck != null) neck.localRotation = CanonicalNeckRot;
            if (head != null) head.localRotation = CanonicalHeadRot;
            if (rightArm != null) rightArm.localRotation = CanonicalRightArmRot;
            if (leftArm != null) leftArm.localRotation = CanonicalLeftArmRot;
            if (rightForeArm != null) rightForeArm.localRotation = CanonicalRightForeArmRot;
            if (leftForeArm != null) leftForeArm.localRotation = CanonicalLeftForeArmRot;
            if (rightHand != null) rightHand.localRotation = CanonicalRightHandRot;
            if (leftHand != null) leftHand.localRotation = CanonicalLeftHandRot;
            if (rightUpLeg != null) rightUpLeg.localRotation = CanonicalRightUpLegRot;
            if (leftUpLeg != null) leftUpLeg.localRotation = CanonicalLeftUpLegRot;
            if (rightLeg != null) rightLeg.localRotation = CanonicalRightLegRot;
            if (leftLeg != null) leftLeg.localRotation = CanonicalLeftLegRot;
            if (rightFoot != null) rightFoot.localRotation = CanonicalRightFootRot;
            if (leftFoot != null) leftFoot.localRotation = CanonicalLeftFootRot;
            if (visualRoot != null) visualRoot.localScale = applyChubbyProportions ? chubbyRootScale : initVisualScale;
            ApplyChubbyBones();
            ResetFingersToBindPose();
        }

        private void CurlFinger(Transform f1, float a1, float a2, float a3)
        {
            if (f1 == null) return;
            f1.localRotation = Quaternion.Euler(a1, 0f, 0f);
            if (f1.childCount > 0)
            {
                Transform f2 = f1.GetChild(0);
                f2.localRotation = Quaternion.Euler(a2, 0f, 0f);
                if (f2.childCount > 0)
                {
                    Transform f3 = f2.GetChild(0);
                    f3.localRotation = Quaternion.Euler(a3, 0f, 0f);
                }
            }
        }

        private void ApplySittingFishermanFingers()
        {
            // Right hand firmly grips the fishing rod handle
            CurlFinger(rIndex1, 65f, 60f, 45f);
            CurlFinger(rMiddle1, 65f, 60f, 45f);
            CurlFinger(rRing1, 65f, 60f, 45f);
            CurlFinger(rPinky1, 65f, 60f, 45f);
            if (rThumb1 != null)
            {
                rThumb1.localRotation = Quaternion.Euler(20f, 35f, 40f);
                if (rThumb2 != null) rThumb2.localRotation = Quaternion.Euler(30f, 0f, 0f);
            }

            // Left hand rests comfortably on left thigh with naturally relaxed curved fingers
            CurlFinger(lIndex1, 30f, 35f, 20f);
            CurlFinger(lMiddle1, 30f, 35f, 20f);
            CurlFinger(lRing1, 30f, 35f, 20f);
            CurlFinger(lPinky1, 30f, 35f, 20f);
            if (lThumb1 != null)
            {
                lThumb1.localRotation = Quaternion.Euler(15f, -20f, -25f);
                if (lThumb2 != null) lThumb2.localRotation = Quaternion.Euler(20f, 0f, 0f);
            }
        }

        private void ResetFingersToBindPose()
        {
            CurlFinger(rIndex1, 6.23f, 0f, 0f);
            CurlFinger(rMiddle1, 0f, 0f, 0f);
            CurlFinger(rRing1, 2.44f, 0f, 0f);
            CurlFinger(rPinky1, 1.74f, 0f, 0f);
            if (rThumb1 != null)
            {
                rThumb1.localRotation = Quaternion.Euler(19.32f, 10.19f, 28.52f);
                if (rThumb2 != null) rThumb2.localRotation = Quaternion.identity;
            }

            CurlFinger(lIndex1, 4.75f, 0f, 0f);
            CurlFinger(lMiddle1, 0f, 0f, 0f);
            CurlFinger(lRing1, 1.71f, 0f, 0f);
            CurlFinger(lPinky1, 2.01f, 0f, 0f);
            if (lThumb1 != null)
            {
                lThumb1.localRotation = Quaternion.Euler(19.40f, -12f, -32.6f);
                if (lThumb2 != null) lThumb2.localRotation = Quaternion.identity;
            }
        }

        private void LateUpdate()
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.02f;
            if (dt <= 0.00001f) return;

            if (!isInitialized || hips == null)
            {
                Initialize(false);
                if (hips == null) return;
            }

            if (Application.isPlaying && autoDetectMovement && role != StickmanRole.SittingFisherman)
            {
                Vector3 curPos = transform.position;
                Vector3 delta = curPos - lastWorldPos;
                delta.y = 0f;
                float speed = delta.magnitude / dt;
                currentSpeed = Mathf.Lerp(currentSpeed, speed, dt * 14f);
                lastWorldPos = curPos;
            }
            else
            {
                currentSpeed = 0f;
            }

            if (isKnockedOut)
            {
                ApplyKnockoutPose();
                return;
            }

            if (role == StickmanRole.SittingFisherman)
            {
                ApplySittingFishermanPose(dt);
                return;
            }

            // Update smooth locomotion blend & walk cycle
            float targetBlend = Application.isPlaying ? Mathf.Clamp01((currentSpeed - 0.08f) / 1.8f) : 0f;
            locomotionBlend = Mathf.MoveTowards(locomotionBlend, targetBlend, dt * 7.5f);

            if (locomotionBlend > 0.01f)
            {
                float speedFactor = Mathf.Clamp(currentSpeed / 3.2f, 0.65f, 1.65f);
                walkCycle += dt * walkFrequency * speedFactor;
            }
            else
            {
                // Smoothly decay walkCycle toward nearest neutral phase (0 or PI)
                walkCycle = Mathf.Lerp(walkCycle, Mathf.Round(walkCycle / Mathf.PI) * Mathf.PI, dt * 8f);
            }

            ApplyStandardLocomotionPose(dt);

            // Overlay upper-body attack swing if active (allows legs to remain grounded or moving)
            if (attackTimer > 0f)
            {
                attackTimer -= dt;
                ApplyAttackPoseOverlay();
            }
        }

        private void ApplySittingFishermanPose(float dt)
        {
            if (fishermanWorker == null) fishermanWorker = GetComponentInParent<FishermanWorker>();
            if (fishingLineVisual == null && fishermanWorker != null)
            {
                fishingLineVisual = fishermanWorker.GetComponent<FishingLineVisual>();
            }

            float t = CurrentAnimTime;
            FishingLinePhase linePhase = fishingLineVisual != null ? fishingLineVisual.Phase : FishingLinePhase.Drifting;

            if (linePhase == FishingLinePhase.CastingOut)
            {
                castSubTimer += dt;
            }
            else
            {
                castSubTimer = 0f;
            }

            // 1. Seated Hips & Legs
            // Position hips cleanly on top of the wooden stool surface (+0.14m in Armature local space).
            // Legs hang calm and relaxed over the stool edge without erratic kicking.
            float legKick = 0f;
            float footFlex = 14f;

            if (hips != null)
            {
                hips.localPosition = CanonicalHipsPos + new Vector3(0f, 0f, 0.14f);
                hips.localRotation = CanonicalHipsRot * Quaternion.Euler(-4f, 0f, 0f);
            }

            // 2. Phase-driven Upper Body, Arms & Hand Posture
            float breathe = Mathf.Sin(t * 2.1f) * 1.6f;
            float spinePitch = breathe * 0.5f;
            float spineYaw = 0f;
            float headPitch = -breathe * 0.4f + 3f;
            float headYaw = Mathf.Sin(t * 0.6f) * 4f;

            // Default relaxed fishing: right arm holds rod elevated forward at waist (~32 deg),
            // left hand rests naturally with palm and curved fingers on top of the left thigh
            Vector3 rArmEuler = new Vector3(50f, 0f, 26f + breathe * 1.5f);
            Vector3 rForeEuler = new Vector3(18f, 0f, 22f);
            Vector3 rHandEuler = new Vector3(-16f, 0f, 0f);

            Vector3 lArmEuler = new Vector3(74f, 0f, -20f);
            Vector3 lForeEuler = new Vector3(56f, 0f, -12f);
            Vector3 lHandEuler = new Vector3(18f, 0f, 8f);

            switch (linePhase)
            {
                case FishingLinePhase.Resting:
                case FishingLinePhase.Drifting:
                    // Peaceful seated waiting: gentle breathing sway, calm hanging legs
                    legKick = Mathf.Sin(t * 1.2f) * 0.8f;
                    footFlex = 14f;
                    break;

                case FishingLinePhase.CastingOut:
                    // Smooth over-the-shoulder wind-up and forward whip
                    if (castSubTimer < 0.28f)
                    {
                        float w = Mathf.Clamp01(castSubTimer / 0.28f);
                        float ease = Mathf.Sin(w * Mathf.PI * 0.5f);
                        spinePitch = Mathf.Lerp(0f, -12f, ease);
                        spineYaw = Mathf.Lerp(0f, 10f, ease);
                        headPitch = 6f;
                        headYaw = 0f;
                        rArmEuler = new Vector3(Mathf.Lerp(50f, 36f, ease), 0f, Mathf.Lerp(26f, 10f, ease));
                        rForeEuler = new Vector3(Mathf.Lerp(18f, 22f, ease), 0f, Mathf.Lerp(22f, 52f, ease));
                        rHandEuler = new Vector3(Mathf.Lerp(-16f, 8f, ease), 0f, 0f);
                    }
                    else
                    {
                        float w = Mathf.Clamp01((castSubTimer - 0.28f) / 0.45f);
                        float whip = Mathf.Sin(w * Mathf.PI);
                        spinePitch = Mathf.Lerp(-12f, 10f, Mathf.Clamp01(w * 2f)) * (1f - w * 0.5f);
                        spineYaw = Mathf.Lerp(10f, -4f, w);
                        headPitch = 4f;
                        headYaw = 0f;
                        rArmEuler = new Vector3(52f, 0f, Mathf.Lerp(50f, 32f, w));
                        rForeEuler = new Vector3(20f, 0f, Mathf.Lerp(16f, 28f, w));
                        rHandEuler = new Vector3(-24f * whip, 0f, 0f);
                    }
                    legKick = 0f;
                    footFlex = 14f;
                    break;

                case FishingLinePhase.Nibbling:
                    // Alert forward lean watching the bobber, rod tip twitching
                    float alertPulse = Mathf.Sin(t * 12f) * 2.0f;
                    spinePitch = 6f + alertPulse * 0.4f;
                    headPitch = 8f;
                    headYaw = 0f;
                    rArmEuler = new Vector3(48f, 0f, 28f + alertPulse);
                    rForeEuler = new Vector3(20f, 0f, 24f);
                    rHandEuler = new Vector3(-18f + alertPulse * 1.5f, 0f, 0f);
                    lArmEuler = new Vector3(70f, 0f, -22f);
                    lForeEuler = new Vector3(50f, 0f, -14f);
                    lHandEuler = new Vector3(18f, 0f, 8f);
                    legKick = 0f;
                    footFlex = 18f;
                    break;

                case FishingLinePhase.Strike:
                    // Hook set! Sharp, energetic cartoon yank backward
                    spinePitch = -15f;
                    headPitch = 4f;
                    headYaw = 0f;
                    rArmEuler = new Vector3(36f, 0f, 48f);
                    rForeEuler = new Vector3(32f, 0f, 38f);
                    rHandEuler = new Vector3(10f, 0f, 0f);
                    lArmEuler = new Vector3(76f, 0f, -18f);
                    lForeEuler = new Vector3(60f, 0f, -10f);
                    lHandEuler = new Vector3(22f, 0f, 5f);
                    legKick = 2.0f;
                    footFlex = 22f;
                    break;

                case FishingLinePhase.ReelingIn:
                    // Rhythmic cartoon tug-of-war: fisherman pumps rod up and back, bracing on knee
                    float pump = Mathf.Sin(t * 7.0f);
                    spinePitch = -12f + pump * 6f;
                    spineYaw = pump * 3f;
                    headPitch = 6f - pump * 3f;
                    headYaw = 0f;
                    rArmEuler = new Vector3(42f - pump * 8f, 0f, 38f + pump * 12f);
                    rForeEuler = new Vector3(25f + pump * 10f, 0f, 28f + pump * 8f);
                    rHandEuler = new Vector3(-8f + pump * 12f, 0f, 0f);
                    lArmEuler = new Vector3(75f - pump * 4f, 0f, -20f);
                    lForeEuler = new Vector3(58f - pump * 3f, 0f, -12f);
                    lHandEuler = new Vector3(20f, 0f, 6f);
                    legKick = pump * 2.0f;
                    footFlex = 18f + Mathf.Max(0f, pump) * 4f;
                    break;
            }

            if (rightUpLeg != null) rightUpLeg.localRotation = CanonicalRightUpLegRot * Quaternion.Euler(78f, 6f, 0f);
            if (leftUpLeg != null) leftUpLeg.localRotation = CanonicalLeftUpLegRot * Quaternion.Euler(78f, -6f, 0f);
            if (rightLeg != null) rightLeg.localRotation = CanonicalRightLegRot * Quaternion.Euler(-84f + legKick, 0f, 0f);
            if (leftLeg != null) leftLeg.localRotation = CanonicalLeftLegRot * Quaternion.Euler(-84f - legKick, 0f, 0f);
            if (rightFoot != null) rightFoot.localRotation = CanonicalRightFootRot * Quaternion.Euler(footFlex, 0f, 0f);
            if (leftFoot != null) leftFoot.localRotation = CanonicalLeftFootRot * Quaternion.Euler(footFlex, 0f, 0f);

            if (spine != null) spine.localRotation = CanonicalSpineRot * Quaternion.Euler(spinePitch * 0.6f, spineYaw * 0.5f, 0f);
            if (spine1 != null) spine1.localRotation = CanonicalSpine1Rot * Quaternion.Euler(spinePitch * 0.5f, spineYaw * 0.5f, 0f);
            if (neck != null) neck.localRotation = CanonicalNeckRot * Quaternion.Euler(headPitch * 0.35f, headYaw * 0.4f, 0f);
            if (head != null) head.localRotation = CanonicalHeadRot * Quaternion.Euler(headPitch * 0.65f, headYaw * 0.6f, 0f);

            if (rightArm != null) rightArm.localRotation = CanonicalRightArmRot * Quaternion.Euler(rArmEuler);
            if (rightForeArm != null) rightForeArm.localRotation = CanonicalRightForeArmRot * Quaternion.Euler(rForeEuler);
            if (rightHand != null) rightHand.localRotation = CanonicalRightHandRot * Quaternion.Euler(rHandEuler);
            if (leftArm != null) leftArm.localRotation = CanonicalLeftArmRot * Quaternion.Euler(lArmEuler);
            if (leftForeArm != null) leftForeArm.localRotation = CanonicalLeftForeArmRot * Quaternion.Euler(lForeEuler);
            if (leftHand != null) leftHand.localRotation = CanonicalLeftHandRot * Quaternion.Euler(lHandEuler);

            ApplySittingFishermanFingers();

            if (visualRoot != null) visualRoot.localScale = applyChubbyProportions ? chubbyRootScale : initVisualScale;
            ApplyChubbyBones();
        }

        private void ApplyStandardLocomotionPose(float dt)
        {
            float t = CurrentAnimTime;
            float b = locomotionBlend;

            float sinCycle = Mathf.Sin(walkCycle);
            float cosCycle = Mathf.Cos(walkCycle);
            float cosDouble = Mathf.Cos(walkCycle * 2f);

            // Check role-specific sub-states
            bool isWranglerCarrying = false;
            bool isWranglerStooping = false;
            if (role == StickmanRole.FishWrangler)
            {
                if (wranglerWorker == null) wranglerWorker = GetComponentInParent<FishWranglerWorker>();
                if (wranglerWorker != null)
                {
                    isWranglerCarrying = wranglerWorker.IsCarrying;
                    isWranglerStooping = (wranglerWorker.State == WranglerState.PickingUp || wranglerWorker.State == WranglerState.Unloading);
                }
            }

            bool isTransporterLoading = false;
            if (role == StickmanRole.LogisticsTransporter)
            {
                if (transporterWorker == null) transporterWorker = GetComponentInParent<LogisticsTransporterWorker>();
                if (transporterWorker != null)
                {
                    TransporterState ts = transporterWorker.CurrentState;
                    isTransporterLoading = (ts == TransporterState.Loading || ts == TransporterState.Unloading);
                }
            }

            // 1. Legs, Knees & Ankles
            // +X on UpLeg swings thigh FORWARD (+Z world); -X on Leg bends knee BACKWARD (-Z world)
            float legSwing = sinCycle * legSwingAngle * b;
            float rKneeFlex = -Mathf.Clamp01(-sinCycle * 0.65f + Mathf.Max(0f, -cosCycle) * 0.55f) * (legSwingAngle * 1.15f) * b;
            float lKneeFlex = -Mathf.Clamp01( sinCycle * 0.65f + Mathf.Max(0f,  cosCycle) * 0.55f) * (legSwingAngle * 1.15f) * b;

            if (isWranglerStooping)
            {
                legSwing = Mathf.Lerp(legSwing, 28f, 0.75f);
                rKneeFlex = Mathf.Lerp(rKneeFlex, -38f, 0.75f);
                lKneeFlex = Mathf.Lerp(lKneeFlex, -38f, 0.75f);
            }

            float rFootPitch = Mathf.Sin(walkCycle + 0.35f) * 16f * b;
            float lFootPitch = -Mathf.Sin(walkCycle + 0.35f) * 16f * b;

            if (rightUpLeg != null) rightUpLeg.localRotation = initRightUpLeg * Quaternion.Euler(legSwing, 0f, 0f);
            if (leftUpLeg != null) leftUpLeg.localRotation = initLeftUpLeg * Quaternion.Euler(-legSwing, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = initRightLeg * Quaternion.Euler(rKneeFlex, 0f, 0f);
            if (leftLeg != null) leftLeg.localRotation = initLeftLeg * Quaternion.Euler(lKneeFlex, 0f, 0f);
            if (rightFoot != null) rightFoot.localRotation = initRightFoot * Quaternion.Euler(rFootPitch, 0f, 0f);
            if (leftFoot != null) leftFoot.localRotation = initLeftFoot * Quaternion.Euler(lFootPitch, 0f, 0f);

            // 2. Hips, Spine & Head
            // Note: Armature has -90 X rotation, so Hips local +Z is World Up (+Y)!
            float idleBreathe = Mathf.Sin(t * 2.4f);
            float walkBounce = Mathf.Abs(sinCycle) * bodyBounceHeight * b;
            float idleBounce = Mathf.Max(0f, idleBreathe) * 0.008f * (1f - b);
            float stoopDrop = isWranglerStooping ? -0.06f : 0f;

            float hipLean = forwardLeanAngle * b + (isWranglerCarrying ? 10f : 0f) + (isWranglerStooping ? 18f : 0f);
            float hipWaddle = sinCycle * bodyWaddleAngle * b;
            float hipYaw = sinCycle * 4.5f * b;

            if (hips != null)
            {
                hips.localPosition = initHipsPos + new Vector3(0f, 0f, walkBounce + idleBounce + stoopDrop);
                hips.localRotation = initHips * Quaternion.Euler(hipLean * 0.45f, hipYaw, hipWaddle);
            }

            float spinePitch = hipLean * 0.55f + idleBreathe * 1.8f * (1f - b);
            float spineCounterYaw = -sinCycle * 5.5f * b;
            if (isTransporterLoading)
            {
                float loadCycle = Mathf.Sin(t * 10f);
                spinePitch += 14f + Mathf.Abs(loadCycle) * 6f;
                spineCounterYaw += loadCycle * 16f;
            }

            if (spine != null) spine.localRotation = initSpine * Quaternion.Euler(spinePitch * 0.55f, spineCounterYaw * 0.5f, -hipWaddle * 0.35f);
            if (spine1 != null) spine1.localRotation = initSpine1 * Quaternion.Euler(spinePitch * 0.45f, spineCounterYaw * 0.5f, -hipWaddle * 0.35f);

            // Curious head look-around when idle (especially Customers/Tourists browsing)
            float idleLookYaw = (role == StickmanRole.CustomerOrTourist)
                ? Mathf.Sin(t * 1.35f) * 14f * (1f - b)
                : Mathf.Sin(t * 0.9f) * 6f * (1f - b);
            float headPitch = -spinePitch * 0.35f + Mathf.Cos(walkCycle * 2f) * 2.5f * b;

            if (neck != null) neck.localRotation = initNeck * Quaternion.Euler(headPitch * 0.4f, idleLookYaw * 0.4f, 0f);
            if (head != null) head.localRotation = initHead * Quaternion.Euler(headPitch * 0.6f, idleLookYaw * 0.6f, 0f);

            // 3. Arms, Elbows & Hands
            // Contralateral gait: when sinCycle > 0 (Right leg forward, Left leg back):
            // - RightArm swings BACKWARD (-Z on RightArm) -> -sinCycle * armSwingAngle
            // - LeftArm swings FORWARD (-Z on LeftArm)    -> -sinCycle * armSwingAngle
            float armSwing = sinCycle * armSwingAngle * b;
            float armRestDown = 64.0f;

            if (role == StickmanRole.FishWrangler)
            {
                if (isWranglerCarrying)
                {
                    // Carrying heavy fish on back: right hand holds bat low, left hand braces shoulder/back
                    float carryBob = Mathf.Abs(sinCycle) * 5f * b;
                    if (rightArm != null) rightArm.localRotation = initRightArm * Quaternion.Euler(56f, 0f, 22f + carryBob);
                    if (rightForeArm != null) rightForeArm.localRotation = initRightForeArm * Quaternion.Euler(20f, 0f, 30f);
                    if (leftArm != null) leftArm.localRotation = initLeftArm * Quaternion.Euler(48f, 0f, -32f - carryBob);
                    if (leftForeArm != null) leftForeArm.localRotation = initLeftForeArm * Quaternion.Euler(35f, 0f, -40f);
                }
                else
                {
                    // Ready stance with bat in right hand, left arm swinging naturally
                    float readyBob = Mathf.Sin(walkCycle * 2f) * 4f * b + idleBreathe * 1.5f * (1f - b);
                    if (rightArm != null) rightArm.localRotation = initRightArm * Quaternion.Euler(52f, 0f, 32f + readyBob);
                    if (rightForeArm != null) rightForeArm.localRotation = initRightForeArm * Quaternion.Euler(18f, 0f, 36f);
                    if (leftArm != null) leftArm.localRotation = initLeftArm * Quaternion.Euler(armRestDown, 0f, -armSwing * 0.9f);
                    float lElbowFlex = 18f + Mathf.Max(0f, sinCycle) * 18f * b;
                    if (leftForeArm != null) leftForeArm.localRotation = initLeftForeArm * Quaternion.Euler(12f, 0f, -lElbowFlex);
                }
                if (rightHand != null) rightHand.localRotation = initRightHand;
                if (leftHand != null) leftHand.localRotation = initLeftHand;
            }
            else if (role == StickmanRole.LogisticsTransporter)
            {
                if (isTransporterLoading)
                {
                    // Rhythmic loading/unloading motion between shelf/storage and cart
                    float loadWave = Mathf.Sin(t * 10f);
                    float liftWave = Mathf.Max(0f, Mathf.Cos(t * 10f));
                    if (rightArm != null) rightArm.localRotation = initRightArm * Quaternion.Euler(46f - liftWave * 12f, 0f, 34f + loadWave * 16f);
                    if (rightForeArm != null) rightForeArm.localRotation = initRightForeArm * Quaternion.Euler(24f, 0f, 32f + liftWave * 14f);
                    if (leftArm != null) leftArm.localRotation = initLeftArm * Quaternion.Euler(46f - liftWave * 12f, 0f, -34f - loadWave * 16f);
                    if (leftForeArm != null) leftForeArm.localRotation = initLeftForeArm * Quaternion.Euler(24f, 0f, -32f - liftWave * 14f);
                }
                else
                {
                    // Both hands reach FORWARD (+Z on RightArm, -Z on LeftArm) to grip the wooden cart handles at waist height!
                    float pushBob = Mathf.Sin(walkCycle * 2f) * 3.0f * b;
                    if (rightArm != null) rightArm.localRotation = initRightArm * Quaternion.Euler(65f, 0f, 26f + pushBob);
                    if (rightForeArm != null) rightForeArm.localRotation = initRightForeArm * Quaternion.Euler(18f, 0f, 16f);
                    if (leftArm != null) leftArm.localRotation = initLeftArm * Quaternion.Euler(65f, 0f, -26f - pushBob);
                    if (leftForeArm != null) leftForeArm.localRotation = initLeftForeArm * Quaternion.Euler(18f, 0f, -16f);
                }
                if (rightHand != null) rightHand.localRotation = initRightHand;
                if (leftHand != null) leftHand.localRotation = initLeftHand;
            }
            else
            {
                // Customer / Tourist natural contralateral arm swing + dynamic elbow bend
                float rElbowFlex = 16f + Mathf.Max(0f, -sinCycle) * 20f * b;
                float lElbowFlex = 16f + Mathf.Max(0f,  sinCycle) * 20f * b;

                if (rightArm != null) rightArm.localRotation = initRightArm * Quaternion.Euler(armRestDown, 0f, -armSwing);
                if (leftArm != null) leftArm.localRotation = initLeftArm * Quaternion.Euler(armRestDown, 0f, -armSwing);
                if (rightForeArm != null) rightForeArm.localRotation = initRightForeArm * Quaternion.Euler(12f, 0f, rElbowFlex);
                if (leftForeArm != null) leftForeArm.localRotation = initLeftForeArm * Quaternion.Euler(12f, 0f, -lElbowFlex);
                if (rightHand != null) rightHand.localRotation = initRightHand;
                if (leftHand != null) leftHand.localRotation = initLeftHand;
            }

            // 4. Subtle Cartoon Squash & Stretch
            if (visualRoot != null)
            {
                float walkSquash = cosDouble * squashStretchAmount * b;
                float idleSquash = idleBreathe * 0.016f * (1f - b);
                float totalSquash = walkSquash + idleSquash;

                Vector3 baseScale = applyChubbyProportions ? chubbyRootScale : initVisualScale;
                visualRoot.localScale = new Vector3(
                    baseScale.x * (1f - totalSquash * 0.45f),
                    baseScale.y * (1f + totalSquash),
                    baseScale.z * (1f - totalSquash * 0.45f)
                );
            }
            ApplyChubbyBones();
        }

        public void TriggerWhack(float duration = 0.28f)
        {
            attackDuration = Mathf.Max(0.18f, duration);
            attackTimer = attackDuration;
        }

        private void ApplyAttackPoseOverlay()
        {
            float t = 1f - Mathf.Clamp01(attackTimer / Mathf.Max(0.01f, attackDuration));

            // 3-Stage Bat Smash:
            // Stage 1 (0.00 -> 0.28): High overhead wind-up (strikePhase: 0 -> -1)
            // Stage 2 (0.28 -> 0.58): Explosive downward smash (strikePhase: -1 -> +1)
            // Stage 3 (0.58 -> 1.00): Follow-through recovery back to ready stance (+1 -> 0)
            Vector3 rArmEuler;
            Vector3 rForeEuler;
            Vector3 rHandEuler;
            Vector3 lArmEuler;
            float torsoPitch;
            float torsoYaw;

            if (t < 0.28f)
            {
                float w = Mathf.Sin((t / 0.28f) * Mathf.PI * 0.5f);
                torsoPitch = Mathf.Lerp(0f, -15f, w);
                torsoYaw = Mathf.Lerp(0f, 22f, w);
                rArmEuler = Vector3.Lerp(new Vector3(52f, 0f, 32f), new Vector3(26f, 0f, -22f), w);
                rForeEuler = Vector3.Lerp(new Vector3(18f, 0f, 36f), new Vector3(10f, 0f, 54f), w);
                rHandEuler = Vector3.Lerp(Vector3.zero, new Vector3(0f, 0f, 32f), w);
                lArmEuler = Vector3.Lerp(new Vector3(60f, 0f, -15f), new Vector3(48f, 0f, -38f), w);
            }
            else if (t < 0.58f)
            {
                float s = (t - 0.28f) / 0.30f;
                // Fast snap curve
                float snap = 1f - Mathf.Pow(1f - s, 3f);
                torsoPitch = Mathf.Lerp(-15f, 24f, snap);
                torsoYaw = Mathf.Lerp(22f, -18f, snap);
                rArmEuler = Vector3.Lerp(new Vector3(26f, 0f, -22f), new Vector3(62f, 0f, 54f), snap);
                rForeEuler = Vector3.Lerp(new Vector3(10f, 0f, 54f), new Vector3(24f, 0f, 16f), snap);
                rHandEuler = Vector3.Lerp(new Vector3(0f, 0f, 32f), new Vector3(0f, 0f, -54f), snap);
                lArmEuler = Vector3.Lerp(new Vector3(48f, 0f, -38f), new Vector3(58f, 0f, 28f), snap);
            }
            else
            {
                float r = (t - 0.58f) / 0.42f;
                float ease = Mathf.SmoothStep(0f, 1f, r);
                torsoPitch = Mathf.Lerp(24f, 0f, ease);
                torsoYaw = Mathf.Lerp(-18f, 0f, ease);
                rArmEuler = Vector3.Lerp(new Vector3(62f, 0f, 54f), new Vector3(52f, 0f, 32f), ease);
                rForeEuler = Vector3.Lerp(new Vector3(24f, 0f, 16f), new Vector3(18f, 0f, 36f), ease);
                rHandEuler = Vector3.Lerp(new Vector3(0f, 0f, -54f), Vector3.zero, ease);
                lArmEuler = Vector3.Lerp(new Vector3(58f, 0f, 28f), new Vector3(60f, 0f, -15f), ease);
            }

            if (spine != null) spine.localRotation = initSpine * Quaternion.Euler(torsoPitch * 0.55f, torsoYaw * 0.55f, 0f);
            if (spine1 != null) spine1.localRotation = initSpine1 * Quaternion.Euler(torsoPitch * 0.45f, torsoYaw * 0.45f, 0f);
            if (rightArm != null) rightArm.localRotation = initRightArm * Quaternion.Euler(rArmEuler);
            if (rightForeArm != null) rightForeArm.localRotation = initRightForeArm * Quaternion.Euler(rForeEuler);
            if (rightHand != null) rightHand.localRotation = initRightHand * Quaternion.Euler(rHandEuler);
            if (leftArm != null) leftArm.localRotation = initLeftArm * Quaternion.Euler(lArmEuler);
        }

        public void SetKnockedOut(bool knocked)
        {
            isKnockedOut = knocked;
            if (!knocked)
            {
                lastWorldPos = transform.position;
            }
        }

        private void ApplyKnockoutPose()
        {
            float t = CurrentAnimTime;
            float dizzyRoll = Mathf.Sin(t * 5.5f) * 14f;
            float dizzyPitch = Mathf.Cos(t * 4.5f) * 10f;
            float legTwitch = Mathf.Sin(t * 9f) * 8f;

            if (hips != null)
            {
                hips.localPosition = initHipsPos;
                hips.localRotation = initHips;
            }
            if (spine != null) spine.localRotation = initSpine * Quaternion.Euler(dizzyPitch * 0.5f, 0f, dizzyRoll * 0.5f);
            if (spine1 != null) spine1.localRotation = initSpine1 * Quaternion.Euler(dizzyPitch * 0.5f, 0f, dizzyRoll * 0.5f);
            if (head != null) head.localRotation = initHead * Quaternion.Euler(-dizzyPitch, Mathf.Sin(t * 4f) * 28f, -dizzyRoll);

            // Sprawled arms and twitching legs while knocked out on back
            if (rightArm != null) rightArm.localRotation = initRightArm * Quaternion.Euler(35f, 0f, -15f);
            if (leftArm != null) leftArm.localRotation = initLeftArm * Quaternion.Euler(35f, 0f, 15f);
            if (rightForeArm != null) rightForeArm.localRotation = initRightForeArm * Quaternion.Euler(15f, 0f, 25f);
            if (leftForeArm != null) leftForeArm.localRotation = initLeftForeArm * Quaternion.Euler(15f, 0f, -25f);
            if (rightUpLeg != null) rightUpLeg.localRotation = initRightUpLeg * Quaternion.Euler(12f + legTwitch, 10f, 0f);
            if (leftUpLeg != null) leftUpLeg.localRotation = initLeftUpLeg * Quaternion.Euler(12f - legTwitch, -10f, 0f);
            if (rightLeg != null) rightLeg.localRotation = initRightLeg * Quaternion.Euler(-20f, 0f, 0f);
            if (leftLeg != null) leftLeg.localRotation = initLeftLeg * Quaternion.Euler(-20f, 0f, 0f);

            if (visualRoot != null) visualRoot.localScale = applyChubbyProportions ? chubbyRootScale : initVisualScale;
            ApplyChubbyBones();
        }
    }
}
