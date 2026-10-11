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
    public class StickmanCartoonVisual : MonoBehaviour
    {
        [Header("Role & Posture")]
        [SerializeField] private StickmanRole role = StickmanRole.CustomerOrTourist;
        [SerializeField] private bool autoDetectMovement = true;

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
            if (hips == null || rightArm == null || leftArm == null || rightUpLeg == null)
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
                    }
                }
            }

            if (!hasBindPose || forceCaptureBindPose)
            {
                if (hips != null) { initHips = hips.localRotation; initHipsPos = hips.localPosition; }
                if (spine != null) initSpine = spine.localRotation;
                if (spine1 != null) initSpine1 = spine1.localRotation;
                if (neck != null) initNeck = neck.localRotation;
                if (head != null) initHead = head.localRotation;
                if (rightArm != null) initRightArm = rightArm.localRotation;
                if (leftArm != null) initLeftArm = leftArm.localRotation;
                if (rightForeArm != null) initRightForeArm = rightForeArm.localRotation;
                if (leftForeArm != null) initLeftForeArm = leftForeArm.localRotation;
                if (rightHand != null) initRightHand = rightHand.localRotation;
                if (leftHand != null) initLeftHand = leftHand.localRotation;
                if (rightUpLeg != null) initRightUpLeg = rightUpLeg.localRotation;
                if (leftUpLeg != null) initLeftUpLeg = leftUpLeg.localRotation;
                if (rightLeg != null) initRightLeg = rightLeg.localRotation;
                if (leftLeg != null) initLeftLeg = leftLeg.localRotation;
                if (rightFoot != null) initRightFoot = rightFoot.localRotation;
                if (leftFoot != null) initLeftFoot = leftFoot.localRotation;
                if (visualRoot != null && visualRoot.localScale.sqrMagnitude > 0.01f)
                {
                    initVisualScale = visualRoot.localScale;
                }
                hasBindPose = true;
            }

            // Ensure equipment sockets exist with world-aligned orientation when right hand is held forward
            if (rightHand != null && rightHandSocket == null)
            {
                var s = rightHand.Find("Socket_RightHand");
                if (s == null)
                {
                    var g = new GameObject("Socket_RightHand");
                    g.transform.SetParent(rightHand, false);
                    s = g.transform;
                }
                s.localPosition = new Vector3(0f, 0.08f, 0f);
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

        /// <summary>
        /// Restores all cached bones back to their canonical bind-pose local transforms.
        /// </summary>
        public void ResetToBindPose()
        {
            if (!hasBindPose) return;

            if (hips != null)
            {
                hips.localPosition = initHipsPos;
                hips.localRotation = initHips;
            }
            if (spine != null) spine.localRotation = initSpine;
            if (spine1 != null) spine1.localRotation = initSpine1;
            if (neck != null) neck.localRotation = initNeck;
            if (head != null) head.localRotation = initHead;
            if (rightArm != null) rightArm.localRotation = initRightArm;
            if (leftArm != null) leftArm.localRotation = initLeftArm;
            if (rightForeArm != null) rightForeArm.localRotation = initRightForeArm;
            if (leftForeArm != null) leftForeArm.localRotation = initLeftForeArm;
            if (rightHand != null) rightHand.localRotation = initRightHand;
            if (leftHand != null) leftHand.localRotation = initLeftHand;
            if (rightUpLeg != null) rightUpLeg.localRotation = initRightUpLeg;
            if (leftUpLeg != null) leftUpLeg.localRotation = initLeftUpLeg;
            if (rightLeg != null) rightLeg.localRotation = initRightLeg;
            if (leftLeg != null) leftLeg.localRotation = initLeftLeg;
            if (rightFoot != null) rightFoot.localRotation = initRightFoot;
            if (leftFoot != null) leftFoot.localRotation = initLeftFoot;
            if (visualRoot != null) visualRoot.localScale = initVisualScale;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            float dt = Time.deltaTime;
            if (dt <= 0.00001f) return;

            if (autoDetectMovement && role != StickmanRole.SittingFisherman)
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
            float targetBlend = Mathf.Clamp01((currentSpeed - 0.08f) / 1.8f);
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

            float t = Time.time + idlePhaseOffset;
            FishingLinePhase linePhase = fishingLineVisual != null ? fishingLineVisual.Phase : FishingLinePhase.Drifting;

            if (linePhase == FishingLinePhase.CastingOut)
            {
                castSubTimer += dt;
            }
            else
            {
                castSubTimer = 0f;
            }

            // 1. Seated Hips & Legs (dangling comfortably off the pier edge facing +Z)
            float kickFreq = (linePhase == FishingLinePhase.Strike || linePhase == FishingLinePhase.ReelingIn) ? 9.5f : 2.1f;
            float kickAmp = (linePhase == FishingLinePhase.Strike || linePhase == FishingLinePhase.ReelingIn) ? 10.0f : 4.5f;
            float legKick = Mathf.Sin(t * kickFreq) * kickAmp;

            if (hips != null)
            {
                hips.localPosition = initHipsPos;
                hips.localRotation = initHips * Quaternion.Euler(-4f, 0f, 0f);
            }
            if (rightUpLeg != null) rightUpLeg.localRotation = initRightUpLeg * Quaternion.Euler(78f, 7f, 0f);
            if (leftUpLeg != null) leftUpLeg.localRotation = initLeftUpLeg * Quaternion.Euler(78f, -7f, 0f);
            if (rightLeg != null) rightLeg.localRotation = initRightLeg * Quaternion.Euler(-78f + legKick, 0f, 0f);
            if (leftLeg != null) leftLeg.localRotation = initLeftLeg * Quaternion.Euler(-78f - legKick, 0f, 0f);
            if (rightFoot != null) rightFoot.localRotation = initRightFoot * Quaternion.Euler(legKick * 0.6f, 0f, 0f);
            if (leftFoot != null) leftFoot.localRotation = initLeftFoot * Quaternion.Euler(-legKick * 0.6f, 0f, 0f);

            // 2. Phase-driven Upper Body, Head & Arms
            // Note on Stickman bone axes:
            // RightArm / RightForeArm: +X lowers arm to side, +Z swings arm FORWARD (+Z world).
            // LeftArm  / LeftForeArm : +X lowers arm to side, -Z swings arm FORWARD (+Z world).
            float breathe = Mathf.Sin(t * 2.3f) * 2.2f;
            float spinePitch = breathe * 0.5f;
            float spineYaw = 0f;
            float headPitch = -breathe * 0.4f + 4f;
            float headYaw = Mathf.Sin(t * 0.8f) * 6f;

            Vector3 rArmEuler = new Vector3(55f, 0f, 35f + breathe);
            Vector3 rForeEuler = new Vector3(18f, 0f, 32f);
            Vector3 rHandEuler = Vector3.zero;

            // Default left arm resting on left knee/thigh
            Vector3 lArmEuler = new Vector3(58f, 0f, -24f);
            Vector3 lForeEuler = new Vector3(34f, 0f, -22f);

            switch (linePhase)
            {
                case FishingLinePhase.CastingOut:
                    // Wind-up (0..0.28s) then whip forward (0.28..0.85s)
                    if (castSubTimer < 0.28f)
                    {
                        float w = Mathf.Clamp01(castSubTimer / 0.28f);
                        float ease = Mathf.Sin(w * Mathf.PI * 0.5f);
                        spinePitch = Mathf.Lerp(0f, -14f, ease);
                        spineYaw = Mathf.Lerp(0f, 12f, ease);
                        headPitch = 6f;
                        headYaw = 0f;
                        rArmEuler = new Vector3(Mathf.Lerp(55f, 38f, ease), 0f, Mathf.Lerp(35f, 8f, ease));
                        rForeEuler = new Vector3(15f, 0f, Mathf.Lerp(32f, 48f, ease));
                    }
                    else
                    {
                        float w = Mathf.Clamp01((castSubTimer - 0.28f) / 0.45f);
                        float whip = Mathf.Sin(w * Mathf.PI);
                        spinePitch = Mathf.Lerp(-14f, 12f, Mathf.Clamp01(w * 2f)) * (1f - w * 0.5f);
                        spineYaw = Mathf.Lerp(12f, -6f, w);
                        headPitch = 4f;
                        headYaw = 0f;
                        rArmEuler = new Vector3(52f, 0f, Mathf.Lerp(54f, 35f, w));
                        rForeEuler = new Vector3(18f, 0f, Mathf.Lerp(18f, 32f, w));
                        rHandEuler = new Vector3(0f, 0f, -whip * 18f);
                    }
                    break;

                case FishingLinePhase.Nibbling:
                    // Alert lean forward watching the bobber
                    float alertPulse = Mathf.Sin(t * 14f) * 2.5f;
                    spinePitch = 9f + alertPulse * 0.3f;
                    headPitch = 8f;
                    headYaw = 0f;
                    rArmEuler = new Vector3(53f, 0f, 38f + alertPulse);
                    rForeEuler = new Vector3(20f, 0f, 34f);
                    // Left hand lifts slightly getting ready to grab the reel
                    lArmEuler = new Vector3(55f, 0f, -32f);
                    lForeEuler = new Vector3(38f, 0f, -28f);
                    break;

                case FishingLinePhase.Strike:
                case FishingLinePhase.ReelingIn:
                    // Active tug-of-war: lean back, pump rod with right arm, crank reel in circles with left hand!
                    float pump = Mathf.Sin(t * 13f);
                    float crankSin = Mathf.Sin(t * 18f);
                    float crankCos = Mathf.Cos(t * 18f);

                    spinePitch = -11f + pump * 5f;
                    spineYaw = -6f + pump * 3f;
                    headPitch = 8f - pump * 3f;
                    headYaw = 0f;

                    rArmEuler = new Vector3(46f, 0f, 42f + pump * 9f);
                    rForeEuler = new Vector3(20f, 0f, 36f - pump * 6f);
                    rHandEuler = new Vector3(0f, 0f, pump * 8f);

                    // Left hand reaches across to the reel and cranks!
                    lArmEuler = new Vector3(50f + crankCos * 6f, 0f, -38f + crankSin * 10f);
                    lForeEuler = new Vector3(42f + crankSin * 8f, 0f, -32f + crankCos * 12f);
                    break;
            }

            if (spine != null) spine.localRotation = initSpine * Quaternion.Euler(spinePitch * 0.6f, spineYaw * 0.5f, 0f);
            if (spine1 != null) spine1.localRotation = initSpine1 * Quaternion.Euler(spinePitch * 0.5f, spineYaw * 0.5f, 0f);
            if (neck != null) neck.localRotation = initNeck * Quaternion.Euler(headPitch * 0.35f, headYaw * 0.4f, 0f);
            if (head != null) head.localRotation = initHead * Quaternion.Euler(headPitch * 0.65f, headYaw * 0.6f, 0f);

            if (rightArm != null) rightArm.localRotation = initRightArm * Quaternion.Euler(rArmEuler);
            if (rightForeArm != null) rightForeArm.localRotation = initRightForeArm * Quaternion.Euler(rForeEuler);
            if (rightHand != null) rightHand.localRotation = initRightHand * Quaternion.Euler(rHandEuler);
            if (leftArm != null) leftArm.localRotation = initLeftArm * Quaternion.Euler(lArmEuler);
            if (leftForeArm != null) leftForeArm.localRotation = initLeftForeArm * Quaternion.Euler(lForeEuler);
            if (leftHand != null) leftHand.localRotation = initLeftHand;

            if (visualRoot != null) visualRoot.localScale = initVisualScale;
        }

        private void ApplyStandardLocomotionPose(float dt)
        {
            float t = Time.time + idlePhaseOffset;
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

                visualRoot.localScale = new Vector3(
                    initVisualScale.x * (1f - totalSquash * 0.45f),
                    initVisualScale.y * (1f + totalSquash),
                    initVisualScale.z * (1f - totalSquash * 0.45f)
                );
            }
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
            float t = Time.time + idlePhaseOffset;
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
        }
    }
}
