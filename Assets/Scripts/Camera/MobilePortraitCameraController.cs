using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace IdleFishing.CameraControl
{
    [RequireComponent(typeof(Camera))]
    public class MobilePortraitCameraController : MonoBehaviour
    {
        public static MobilePortraitCameraController Instance { get; private set; }

        [Header("Default Portrait Framing (9:16)")]
        [SerializeField] private Vector3 defaultPosition = new Vector3(0f, 29f, -18.5f);
        [SerializeField] private Vector3 defaultEulerAngles = new Vector3(55.5f, 0f, 0f);
        [SerializeField] private float baseFOV = 50f;
        [SerializeField] private float referenceAspect = 9f / 16f; // 0.5625

        [Header("Pan Limits (2D Ground Exploration)")]
        [SerializeField] private float minPanX = -13.5f; // Sang trái (Cổng Thế Giới, Bàn Phi-lê)
        [SerializeField] private float maxPanX = 13.5f;  // Sang phải (Thủy Cung Hoàng Gia)
        [SerializeField] private float minPanZ = -30.5f; // Xuống dưới (Quầy Kệ & Phố Khách Hàng)
        [SerializeField] private float maxPanZ = -6.5f;  // Lên trên (Cầu Cảng & Đại Dương)
        [SerializeField] private float panSensitivity = 0.024f;
        [SerializeField] private float smoothDampTime = 0.08f;
        [SerializeField] private float inertiaDecay = 7.5f;

        [Header("Zoom Limits & Sensitivity")]
        [SerializeField] private float minFOV = 22.0f; // Phóng to cận cảnh siêu nét
        [SerializeField] private float maxFOV = 62.0f; // Thu nhỏ toàn cảnh bến cảng
        [SerializeField] private float wheelZoomSpeed = 4.5f;
        [SerializeField] private float pinchZoomSpeed = 0.07f;
        [SerializeField] private float keyZoomSpeed = 28.0f;
        [SerializeField] private float keyPanSpeed = 18.0f;

        [Header("Toggles & Settings")]
        [SerializeField] private bool allowPan = true;
        [SerializeField] private bool allowZoom = true;
        [SerializeField] private bool invertDrag = false;
        [SerializeField] private float dragThresholdPixels = 8f;
        [SerializeField] private bool showOnScreenZoomButtons = true;

        private Camera cam;
        private Vector3 targetPosition;
        private float targetFOV;
        private Vector3 currentPanSmoothVelocity;
        private float currentFovSmoothVelocity;
        private Vector3 momentumVelocity;

        // Pointer drag state
        private bool isPointerDragging = false;
        private Vector2 lastPointerPosition;
        private float accumulatedDragDistance = 0f;
        private int activeTouchId = -1;

        // Pinch zoom state
        private bool isPinching = false;
        private float prevPinchDistance = 0f;

        // Camera Shake & Juice
        private float shakeIntensity = 0f;
        private float shakeDuration = 0f;
        private float shakeTimer = 0f;
        private float shakeFrequency = 32f;
        private Vector3 currentShakeOffset = Vector3.zero;

        private float punchFovOffset = 0f;
        private float punchFovVelocity = 0f;

        public Camera Cam => cam != null ? cam : (cam = GetComponent<Camera>());
        public float TargetFOV => targetFOV;
        public Vector3 TargetPosition => targetPosition;

        private void Awake()
        {
            Time.timeScale = 1.0f;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            cam = GetComponent<Camera>();
            ApplyDefaultFraming();
        }

        private void Start()
        {
            Time.timeScale = 1.0f;
            AdjustFOVForScreenAspect();
            if (showOnScreenZoomButtons)
            {
                CreateFloatingZoomControls();
            }
        }

        public void ApplyDefaultFraming()
        {
            transform.position = defaultPosition;
            transform.rotation = Quaternion.Euler(defaultEulerAngles);
            targetPosition = defaultPosition;
            targetFOV = baseFOV;
            momentumVelocity = Vector3.zero;

            AdjustFOVForScreenAspect();
            if (cam != null)
            {
                cam.fieldOfView = targetFOV;
            }
        }

        public void AdjustFOVForScreenAspect()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;

            float currentAspect = cam.aspect;
            if (currentAspect <= 0.01f) currentAspect = referenceAspect;

            float targetHRad = Mathf.Atan(Mathf.Tan(baseFOV * 0.5f * Mathf.Deg2Rad) * referenceAspect);
            float adaptiveVFOV = 2f * Mathf.Atan(Mathf.Tan(targetHRad) / currentAspect) * Mathf.Rad2Deg;

            targetFOV = Mathf.Clamp(adaptiveVFOV, minFOV, maxFOV);
            cam.fieldOfView = targetFOV;
        }

        private void Update()
        {
            HandleTouchInput();
            HandleMouseInput();
            HandleKeyboardInput();
            ApplyInertia();
        }

        private void LateUpdate()
        {
            targetPosition.y = defaultPosition.y; // Giữ độ cao máy quay ổn định song song mặt đất
            Vector3 smoothedPos = Vector3.SmoothDamp(transform.position - currentShakeOffset, targetPosition, ref currentPanSmoothVelocity, smoothDampTime);

            // Cập nhật rung màn hình (Perlin Noise Camera Shake)
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(shakeTimer / Mathf.Max(0.001f, shakeDuration));
                float currentMag = shakeIntensity * progress;
                float sampleTime = Time.unscaledTime * shakeFrequency;

                float offsetX = (Mathf.PerlinNoise(sampleTime, 0f) * 2f - 1f) * currentMag;
                float offsetZ = (Mathf.PerlinNoise(0f, sampleTime) * 2f - 1f) * currentMag * 0.75f;
                currentShakeOffset = new Vector3(offsetX, 0f, offsetZ);
            }
            else
            {
                currentShakeOffset = Vector3.zero;
            }

            transform.position = smoothedPos + currentShakeOffset;

            // Hồi phục Punch FOV đàn hồi
            punchFovOffset = Mathf.SmoothDamp(punchFovOffset, 0f, ref punchFovVelocity, 0.12f);

            if (cam != null)
            {
                float targetWithPunch = Mathf.Clamp(targetFOV + punchFovOffset, minFOV, maxFOV);
                cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetWithPunch, ref currentFovSmoothVelocity, smoothDampTime);
            }
        }

        public void TriggerShake(float intensity = 0.35f, float duration = 0.25f)
        {
            shakeIntensity = Mathf.Max(shakeIntensity * (shakeTimer > 0f ? 0.5f : 0f), intensity);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeTimer = shakeDuration;
        }

        public void TriggerPunchFOV(float punchAmount = -2.5f)
        {
            punchFovOffset = punchAmount;
        }

        private void HandleTouchInput()
        {
            if (!allowPan && !allowZoom) return;

            if (Input.touchCount == 1 && allowPan)
            {
                isPinching = false;
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    if (IsPointerOverUI(touch.fingerId))
                    {
                        isPointerDragging = false;
                        activeTouchId = -1;
                    }
                    else
                    {
                        isPointerDragging = true;
                        activeTouchId = touch.fingerId;
                        lastPointerPosition = touch.position;
                        accumulatedDragDistance = 0f;
                        momentumVelocity = Vector3.zero;
                    }
                }
                else if (touch.phase == TouchPhase.Moved && isPointerDragging && touch.fingerId == activeTouchId)
                {
                    ProcessPointerMove(touch.position);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    if (touch.fingerId == activeTouchId)
                    {
                        isPointerDragging = false;
                        activeTouchId = -1;
                    }
                }
            }
            else if (Input.touchCount >= 2 && allowZoom)
            {
                // 2-finger Pinch Zoom
                isPointerDragging = false;
                activeTouchId = -1;

                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                float curDist = Vector2.Distance(t0.position, t1.position);
                if (!isPinching || t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
                {
                    isPinching = true;
                    prevPinchDistance = curDist;
                }
                else
                {
                    float deltaDist = curDist - prevPinchDistance;
                    // Phóng to khi mở rộng 2 ngón tay, thu nhỏ khi chụm lại
                    targetFOV = Mathf.Clamp(targetFOV - deltaDist * pinchZoomSpeed, minFOV, maxFOV);
                    prevPinchDistance = curDist;
                }
            }
            else
            {
                isPinching = false;
            }
        }

        private void HandleMouseInput()
        {
            if (Input.touchCount > 0) return; // Ưu tiên touch trên thiết bị cảm ứng

            // 1. Mouse Drag (Hỗ trợ Chuột Trái sau ngưỡng kéo, Chuột Phải & Chuột Giữa kéo tự do ngay lập tức)
            if (allowPan)
            {
                bool rightOrMiddleDown = Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
                bool leftDown = Input.GetMouseButtonDown(0);

                if (rightOrMiddleDown || leftDown)
                {
                    if (leftDown && IsPointerOverUI(-1))
                    {
                        isPointerDragging = false;
                    }
                    else
                    {
                        isPointerDragging = true;
                        lastPointerPosition = Input.mousePosition;
                        accumulatedDragDistance = rightOrMiddleDown ? dragThresholdPixels : 0f;
                        momentumVelocity = Vector3.zero;
                    }
                }
                else if ((Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)) && isPointerDragging)
                {
                    ProcessPointerMove(Input.mousePosition);
                }
                else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
                {
                    isPointerDragging = false;
                }
            }

            // 2. Mouse Wheel Zoom
            if (allowZoom)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.005f)
                {
                    targetFOV = Mathf.Clamp(targetFOV - scroll * wheelZoomSpeed * 12f, minFOV, maxFOV);
                }
                else if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.05f)
                {
                    targetFOV = Mathf.Clamp(targetFOV - Input.mouseScrollDelta.y * wheelZoomSpeed, minFOV, maxFOV);
                }
            }
        }

        private void HandleKeyboardInput()
        {
            // WASD / Mũi tên để di chuyển góc nhìn trên PC
            if (allowPan)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");

                if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
                {
                    float fovRatio = (cam != null ? cam.fieldOfView : baseFOV) / baseFOV;
                    Vector3 moveDir = new Vector3(h, 0f, v).normalized;
                    Vector3 keyMove = moveDir * (keyPanSpeed * fovRatio * Time.deltaTime);

                    targetPosition += keyMove;
                    targetPosition.x = Mathf.Clamp(targetPosition.x, minPanX, maxPanX);
                    targetPosition.z = Mathf.Clamp(targetPosition.z, minPanZ, maxPanZ);
                    momentumVelocity = Vector3.zero;
                }
            }

            // Phím +/- để phóng to / thu nhỏ, phím R để quay về ban đầu
            if (allowZoom)
            {
                if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
                {
                    targetFOV = Mathf.Clamp(targetFOV - keyZoomSpeed * Time.deltaTime, minFOV, maxFOV);
                }
                else if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
                {
                    targetFOV = Mathf.Clamp(targetFOV + keyZoomSpeed * Time.deltaTime, minFOV, maxFOV);
                }

                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Home))
                {
                    ResetOverview();
                }
            }
        }

        private void ProcessPointerMove(Vector2 currentScreenPos)
        {
            Vector2 deltaPixels = currentScreenPos - lastPointerPosition;
            lastPointerPosition = currentScreenPos;

            accumulatedDragDistance += deltaPixels.magnitude;
            if (accumulatedDragDistance >= dragThresholdPixels)
            {
                float fovRatio = (cam != null ? cam.fieldOfView : baseFOV) / baseFOV;
                float effectiveSpeed = panSensitivity * fovRatio;
                if (invertDrag) effectiveSpeed = -effectiveSpeed;

                float sinTilt = Mathf.Sin(defaultEulerAngles.x * Mathf.Deg2Rad);
                if (sinTilt < 0.1f) sinTilt = 0.824f;

                // Kéo tự nhiên (Natural pan): Kéo tay sang phải -> thế giới trôi sang phải -> camera lùi sang trái
                float deltaWorldX = -deltaPixels.x * effectiveSpeed;
                float deltaWorldZ = -deltaPixels.y * (effectiveSpeed / sinTilt);

                Vector3 worldDelta = new Vector3(deltaWorldX, 0f, deltaWorldZ);
                targetPosition += worldDelta;

                targetPosition.x = Mathf.Clamp(targetPosition.x, minPanX, maxPanX);
                targetPosition.z = Mathf.Clamp(targetPosition.z, minPanZ, maxPanZ);

                if (Time.deltaTime > 0.0001f)
                {
                    momentumVelocity = worldDelta / Time.deltaTime;
                    momentumVelocity = Vector3.ClampMagnitude(momentumVelocity, 40f);
                }
            }
        }

        private void ApplyInertia()
        {
            if (!isPointerDragging && momentumVelocity.sqrMagnitude > 0.01f)
            {
                targetPosition += momentumVelocity * Time.deltaTime;
                targetPosition.x = Mathf.Clamp(targetPosition.x, minPanX, maxPanX);
                targetPosition.z = Mathf.Clamp(targetPosition.z, minPanZ, maxPanZ);

                momentumVelocity = Vector3.Lerp(momentumVelocity, Vector3.zero, Time.deltaTime * inertiaDecay);
            }
        }

        private bool IsPointerOverUI(int fingerId)
        {
            if (EventSystem.current == null) return false;

            if (fingerId >= 0)
            {
                return EventSystem.current.IsPointerOverGameObject(fingerId);
            }
            return EventSystem.current.IsPointerOverGameObject();
        }

        public void ZoomIn(float step = 6f)
        {
            targetFOV = Mathf.Clamp(targetFOV - step, minFOV, maxFOV);
        }

        public void ZoomOut(float step = 6f)
        {
            targetFOV = Mathf.Clamp(targetFOV + step, minFOV, maxFOV);
        }

        public void ResetOverview()
        {
            targetPosition = defaultPosition;
            targetFOV = baseFOV;
            momentumVelocity = Vector3.zero;
            AdjustFOVForScreenAspect();
        }

        public void FocusPier()
        {
            targetPosition = new Vector3(0f, defaultPosition.y, maxPanZ + 1.0f);
            momentumVelocity = Vector3.zero;
        }

        public void FocusMarket()
        {
            targetPosition = new Vector3(0f, defaultPosition.y, minPanZ + 3.0f);
            momentumVelocity = Vector3.zero;
        }

        /// <summary>
        /// Tạo cụm phím tắt Zoom gọn gàng ở cạnh phải màn hình (+, -, ⟲)
        /// </summary>
        private void CreateFloatingZoomControls()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Kiểm tra tránh trùng lặp
            if (canvas.transform.Find("FloatingZoomControls") != null) return;

            GameObject root = new GameObject("FloatingZoomControls");
            root.transform.SetParent(canvas.transform, false);

            RectTransform rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-20f, -40f);
            rt.sizeDelta = new Vector2(68f, 210f);

            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            // 1. Nút Phóng To [+]
            CreateZoomButton(root.transform, "+", () => ZoomIn(6f), new Color(0.12f, 0.22f, 0.35f, 0.88f), "Zoom In");

            // 2. Nút Thu Nhỏ [-]
            CreateZoomButton(root.transform, "-", () => ZoomOut(6f), new Color(0.12f, 0.22f, 0.35f, 0.88f), "Zoom Out");

            // 3. Nút Reset [R]
            CreateZoomButton(root.transform, "R", ResetOverview, new Color(0.16f, 0.40f, 0.55f, 0.90f), "Reset View");
        }

        private static GameObject CreateZoomButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, Color bgColor, string tooltip)
        {
            GameObject btnObj = new GameObject($"Btn_Zoom_{label}");
            btnObj.transform.SetParent(parent, false);

            var img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.25f, 0.65f, 0.95f, 0.95f);
            colors.pressedColor = new Color(0.10f, 0.50f, 0.80f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 28;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;

            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            return btnObj;
        }
    }
}
