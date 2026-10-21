using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.UI;
using IdleFishing.Audio;
using IdleFishing.VFX;

namespace IdleFishing.World
{
    /// <summary>
    /// Thuyền Ngoài Khơi (Offshore Travel Ship) thay thế Cổng Dịch Chuyển cũ.
    /// Neo đậu dập dềnh trên sóng nước bến cảng, cho phép nhấp vào để mở menu chuyển Level.
    /// Khi đổi Level, thuyền sẽ hú còi, rẽ sóng chạy ra xa khơi rồi kích hoạt màn chuyển cảnh Load In/Out.
    /// </summary>
    public class OffshoreTravelShip : MonoBehaviour
    {
        private static OffshoreTravelShip _instance;
        public static OffshoreTravelShip Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<OffshoreTravelShip>();
                }
                return _instance;
            }
            private set { _instance = value; }
        }

        [Header("Mooring Anchor (Offshore Dock Position)")]
        [SerializeField] private Vector3 dockedPosition = new Vector3(7.6f, 0.22f, 17.2f);
        [SerializeField] private Vector3 dockedRotation = new Vector3(0f, 260.0f, 0f);

        [Header("Sail Away Waypoints (Departure & Arrival Course)")]
        [SerializeField] private Vector3 sailDestination = new Vector3(12.0f, 0.22f, 42.0f);
        [SerializeField] private Vector3 incomingStartPos = new Vector3(11.0f, 0.22f, 30.0f);
        [SerializeField] private float sailOutDuration = 2.0f;
        [SerializeField] private float sailInDuration = 1.2f;

        [Header("5 Level Ship Prefabs")]
        [SerializeField] private GameObject[] levelShipPrefabs = new GameObject[5];
        [SerializeField] private GameObject currentShipVisual;
        [SerializeField] private int currentWorldOrder = 1;

        [Header("VFX References")]
        [SerializeField] private Transform smokestackPoint;

        private GameObject overheadBadgeObj;
        private TextMeshProUGUI badgeText;
        private bool isTransitioning = false;
        private float bobPhase;
        private float ambientRippleTimer = 0f;
        private float ambientSmokeTimer = 0f;

        public bool IsTransitioning => isTransitioning;

        private void OnEnable()
        {
            GameEvents.WorldChanged += HandleWorldChanged;
        }

        private void OnDisable()
        {
            GameEvents.WorldChanged -= HandleWorldChanged;
        }

        private void HandleWorldChanged(WorldData newWorld)
        {
            if (newWorld != null)
            {
                SetShipVisualForWorld(newWorld.worldOrder);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            bobPhase = UnityEngine.Random.Range(0f, 10f);
            transform.position = dockedPosition;
            transform.rotation = Quaternion.Euler(dockedRotation);

            EnsureCollider();
            SetupSmokestack();
            CreateOverheadBadge();
        }

        private void Start()
        {
            if (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null)
            {
                SetShipVisualForWorld(WorldManager.Instance.CurrentWorld.worldOrder);
            }
            else
            {
                SetShipVisualForWorld(1);
            }
        }

        /// <summary>
        /// Đồng bộ visual, ống khói VFX, BoxCollider và Badge bảng tên cho thuyền theo Level tương ứng (1..5)
        /// </summary>
        public void SetShipVisualForWorld(int worldOrder)
        {
            worldOrder = Mathf.Clamp(worldOrder, 1, 5);
            currentWorldOrder = worldOrder;

            string expectedVisualName = $"Visual_Ship_Level{worldOrder}";

            // 1. Check if any children exist matching this or other level visuals
            Transform activeVisualTr = null;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("Visual_Ship_Level") || child.name == "Model_Toon_Cargo_Trawler_Ship")
                {
                    if (child.name == expectedVisualName)
                    {
                        child.gameObject.SetActive(true);
                        activeVisualTr = child;
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            // If not found among existing children, instantiate it
            if (activeVisualTr == null)
            {
                GameObject prefab = null;
                if (levelShipPrefabs != null && levelShipPrefabs.Length >= worldOrder && levelShipPrefabs[worldOrder - 1] != null)
                {
                    prefab = levelShipPrefabs[worldOrder - 1];
                }
                else
                {
                    string[] prefabNames = new string[]
                    {
                        "Toon_Ship_Level1_Trawler",
                        "Toon_Ship_Level2_Cruiser",
                        "Toon_Ship_Level3_SteamTrawler",
                        "Toon_Ship_Level4_Icebreaker",
                        "Toon_Ship_Level5_RoyalFlagship"
                    };
                    string prefabPath = $"Assets/Prefabs/Buildings/{prefabNames[worldOrder - 1]}.prefab";
#if UNITY_EDITOR
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
#endif
                }

                if (prefab != null)
                {
                    GameObject newShip = Instantiate(prefab, transform);
                    newShip.name = expectedVisualName;
                    newShip.transform.localPosition = Vector3.zero;
                    newShip.transform.localRotation = Quaternion.identity;
                    newShip.transform.localScale = Vector3.one;
                    newShip.SetActive(true);
                    activeVisualTr = newShip.transform;

                    Transform legacyModel = transform.Find("Model_Toon_Cargo_Trawler_Ship");
                    if (legacyModel != null) legacyModel.gameObject.SetActive(false);
                }
            }

            if (activeVisualTr != null)
            {
                currentShipVisual = activeVisualTr.gameObject;
            }

            // 2. Adjust Smokestack position for VFX (chỉ bật ở Level 3, 4, 5)
            SetupSmokestack();
            if (smokestackPoint != null)
            {
                if (worldOrder <= 2)
                {
                    smokestackPoint.gameObject.SetActive(false);
                }
                else
                {
                    smokestackPoint.gameObject.SetActive(true);
                    Vector3 smokePos = worldOrder switch
                    {
                        3 => new Vector3(0f, 0.95f, -2.40f), // Ống xả động cơ gắn ngoài
                        4 => new Vector3(0f, 2.60f, -1.10f), // Ống khói cabin tàu băng
                        5 => new Vector3(0f, 2.95f, -1.40f), // Ống khói đôi đại tàu
                        _ => new Vector3(0f, 1.50f, -1.00f)
                    };
                    smokestackPoint.localPosition = smokePos;
                }
            }

            // 3. Adjust Overhead Badge position
            EnsureSingleOverheadBadge();
            Vector3 badgePos = worldOrder switch
            {
                1 => new Vector3(0f, 1.95f, 0.15f),
                2 => new Vector3(0f, 2.25f, 0.15f),
                3 => new Vector3(0f, 2.85f, 0.20f),
                4 => new Vector3(0f, 3.45f, 0.20f),
                5 => new Vector3(0f, 4.35f, 0.30f),
                _ => new Vector3(0f, 2.20f, 0.20f)
            };
            if (overheadBadgeObj != null)
            {
                overheadBadgeObj.transform.localPosition = badgePos;
                overheadBadgeObj.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
                var rt = overheadBadgeObj.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition3D = badgePos;
            }

            // 4. Adjust BoxCollider size & center (vừa khít với mô hình thuyền mới)
            var col = GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            switch (worldOrder)
            {
                case 1:
                    col.center = new Vector3(0f, 0.40f, 0.10f);
                    col.size = new Vector3(1.50f, 1.20f, 3.20f);
                    break;
                case 2:
                    col.center = new Vector3(0f, 0.50f, 0.10f);
                    col.size = new Vector3(2.00f, 1.40f, 4.40f);
                    break;
                case 3:
                    col.center = new Vector3(0f, 1.10f, 0.05f);
                    col.size = new Vector3(2.30f, 2.40f, 5.50f);
                    break;
                case 4:
                    col.center = new Vector3(0f, 1.35f, 0.10f);
                    col.size = new Vector3(2.60f, 2.90f, 6.50f);
                    break;
                case 5:
                    col.center = new Vector3(0f, 1.60f, 0.10f);
                    col.size = new Vector3(2.90f, 3.50f, 7.60f);
                    break;
            }

            // 5. Adjust Boarding Ladder position
            Transform ladder = transform.Find("Boarding_Ladder");
            if (ladder != null)
            {
                if (worldOrder <= 2)
                {
                    ladder.gameObject.SetActive(false);
                }
                else
                {
                    ladder.gameObject.SetActive(true);
                    Vector3 ladderPos = worldOrder switch
                    {
                        3 => new Vector3(0.96f, 0.45f, -0.6f),
                        4 => new Vector3(1.14f, 0.65f, -0.6f),
                        5 => new Vector3(1.29f, 0.75f, -0.6f),
                        _ => new Vector3(1.00f, 0.50f, -0.6f)
                    };
                    ladder.localPosition = ladderPos;
                }
            }
        }

        private void EnsureCollider()
        {
            var col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 2.5f, 0.5f);
                col.size = new Vector3(4.5f, 6.0f, 10.5f);
            }
        }

        private void SetupSmokestack()
        {
            if (smokestackPoint == null)
            {
                Transform found = transform.Find("SmokestackPoint");
                if (found == null)
                {
                    GameObject sm = new GameObject("SmokestackPoint");
                    sm.transform.SetParent(transform, false);
                    sm.transform.localPosition = new Vector3(0f, 4.3f, -0.6f);
                    smokestackPoint = sm.transform;
                }
                else
                {
                    smokestackPoint = found;
                }
            }
        }

        private void EnsureSingleOverheadBadge()
        {
            overheadBadgeObj = null;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "Ship_Overhead_Badge")
                {
                    if (overheadBadgeObj == null)
                    {
                        overheadBadgeObj = child.gameObject;
                    }
                    else
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            if (overheadBadgeObj == null)
            {
                CreateOverheadBadge();
            }
        }

        private void CreateOverheadBadge()
        {
            if (overheadBadgeObj != null) return;

            overheadBadgeObj = new GameObject("Ship_Overhead_Badge");
            overheadBadgeObj.transform.SetParent(transform, false);
            overheadBadgeObj.transform.localPosition = new Vector3(0f, 2.0f, 0.2f);
            overheadBadgeObj.transform.localScale = Vector3.one * 0.0085f;

            Canvas canvas = overheadBadgeObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rect = overheadBadgeObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240, 60);

            GameObject bgObj = new GameObject("Badge_BG");
            bgObj.transform.SetParent(overheadBadgeObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = FloatingHealthBarUI.GetWhiteSprite();
            bgImg.color = new Color(0.04f, 0.10f, 0.18f, 0.92f); // Deep Oceanic Navy
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(240, 60);

            GameObject textObj = new GameObject("Badge_Text");
            textObj.transform.SetParent(bgObj.transform, false);
            badgeText = textObj.AddComponent<TextMeshProUGUI>();
            badgeText.fontSize = 17;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.color = Color.white;
            badgeText.text = "<color=#38BDF8>[EXPEDITION SHIP]</color>\n<size=12><color=#FCD34D>Tap to set sail</color></size>";
            badgeText.ApplyFont(UIFontCategory.Header);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(230, 54);
        }

        private void Update()
        {
            if (!isTransitioning)
            {
                // Nhấp nhô bồng bềnh tự nhiên theo sóng biển
                float yOffset = Mathf.Sin((Time.time + bobPhase) * 1.5f) * 0.08f;
                float roll = Mathf.Sin((Time.time + bobPhase) * 1.2f) * 2.2f;
                float pitch = Mathf.Cos((Time.time + bobPhase) * 1.4f) * 1.5f;

                transform.position = dockedPosition + new Vector3(0f, yOffset, 0f);
                transform.rotation = Quaternion.Euler(dockedRotation.x + pitch, dockedRotation.y, dockedRotation.z + roll);

                // Giữ bảng tên overhead luôn hướng thẳng về Camera người chơi (Billboard)
                if (overheadBadgeObj != null)
                {
                    var mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        overheadBadgeObj.transform.rotation = mainCam.transform.rotation;
                    }
                }

                // Gợn sóng biển nhẹ quanh thân thuyền
                ambientRippleTimer += Time.deltaTime;
                if (ambientRippleTimer >= 2.8f)
                {
                    ambientRippleTimer = 0f;
                    if (VFXManager.Instance != null)
                    {
                        float rearOffset = currentWorldOrder switch
                        {
                            1 => 1.2f,
                            2 => 1.6f,
                            3 => 2.1f,
                            4 => 2.6f,
                            _ => 3.0f
                        };
                        VFXManager.Instance.SpawnWaterRipple(transform.position + Vector3.back * rearOffset, 2.0f);
                    }
                }

                // Luồng khói nhẹ từ ống khói tàu (chỉ các tàu có động cơ Level 3, 4, 5)
                ambientSmokeTimer += Time.deltaTime;
                if (ambientSmokeTimer >= 3.2f)
                {
                    ambientSmokeTimer = 0f;
                    if (VFXManager.Instance != null && smokestackPoint != null && smokestackPoint.gameObject.activeInHierarchy)
                    {
                        VFXManager.Instance.SpawnDustPuff(smokestackPoint.position, 0.7f);
                    }
                }

                badgeCheckTimer += Time.deltaTime;
                if (badgeCheckTimer >= 1.0f)
                {
                    badgeCheckTimer = 0f;
                    UpdateBadgeState();
                }
            }
        }

        private float badgeCheckTimer = 0f;

        private void UpdateBadgeState()
        {
            if (badgeText == null || WorldManager.Instance == null) return;
            int curIndex = WorldManager.Instance.ActiveWorldIndex;
            int nextIndex = curIndex + 1;
            var worlds = WorldManager.Instance.Worlds;
            if (worlds != null && nextIndex < worlds.Count)
            {
                WorldData nextWorld = worlds[nextIndex];
                if (!WorldManager.Instance.IsWorldUnlocked(nextWorld) && WorldManager.Instance.CanUnlockWorld(nextWorld, out _))
                {
                    badgeText.text = $"<color=#4ADE80>[SAIL] READY TO SET SAIL! [SAIL]</color>\n<size=12><color=#FCD34D>Tap to Unlock Level {nextIndex + 1}</color></size>";
                    return;
                }
            }

            badgeText.text = "<color=#38BDF8>[EXPEDITION SHIP]</color>\n<size=12><color=#FCD34D>Tap to set sail</color></size>";
        }

        private void LateUpdate()
        {
            if (overheadBadgeObj != null && Camera.main != null)
            {
                overheadBadgeObj.transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void OnMouseDown()
        {
            if (isTransitioning) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }

            // Nếu cấp độ tiếp theo đã sẵn sàng mở khóa, bật thẳng màn hình chúc mừng hoành tráng
            if (WorldManager.Instance != null)
            {
                int curIndex = WorldManager.Instance.ActiveWorldIndex;
                int nextIndex = curIndex + 1;
                var worlds = WorldManager.Instance.Worlds;
                if (worlds != null && nextIndex < worlds.Count)
                {
                    WorldData nextWorld = worlds[nextIndex];
                    if (!WorldManager.Instance.IsWorldUnlocked(nextWorld) && WorldManager.Instance.CanUnlockWorld(nextWorld, out _))
                    {
                        WorldUnlockCelebrationModal.Instance.Show(nextWorld, nextIndex);
                        return;
                    }
                }
            }

            if (WorldSelectionPopupUI.Instance != null)
            {
                WorldSelectionPopupUI.Instance.Open();
            }
        }

        /// <summary>
        /// Kích hoạt chuỗi animation thuyền nhổ neo rẽ sóng ra xa khơi và chạy màn Load In/Out chuyển Level
        /// </summary>
        public void SailAwayToWorld(int targetWorldIndex, bool isPreview = false)
        {
            if (isTransitioning) return;
            StartCoroutine(SailAwaySequenceRoutine(targetWorldIndex, isPreview));
        }

        private IEnumerator SailAwaySequenceRoutine(int targetWorldIndex, bool isPreview)
        {
            isTransitioning = true;

            if (WorldSelectionPopupUI.Instance != null)
            {
                WorldSelectionPopupUI.Instance.Close();
            }

            // 1. Còi tàu hú vang vọng & hiệu ứng bọt sóng xuất bến
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayShipHorn(transform.position);
            }

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnWaterRipple(transform.position, 3.2f);
                VFXManager.Instance.SpawnWaterRipple(transform.position + Vector3.forward * 2f, 2.5f);
            }

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 oceanDir = (sailDestination - startPos).normalized;
            Quaternion targetSailRot = Quaternion.LookRotation(oceanDir, Vector3.up);

            WorldData targetWorld = null;
            if (WorldManager.Instance != null && targetWorldIndex >= 0 && targetWorldIndex < WorldManager.Instance.Worlds.Count)
            {
                targetWorld = WorldManager.Instance.Worlds[targetWorldIndex];
            }

            // 2. Thuyền nhổ neo: Quay mũi tàu về hướng biển rồi tăng tốc lướt sóng ra xa khơi
            float elapsed = 0f;
            float smokeTimer = 0f;
            float rippleTimer = 0f;
            bool triggeredCurtain = false;

            while (elapsed < sailOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / sailOutDuration);

                // Giai đoạn 1 (0 -> 25% thời gian): Thuyền xoay mũi tại chỗ về hướng ra khơi
                // Giai đoạn 2 (25% -> 100% thời gian): Thuyền phóng thẳng về phía biển, mũi luôn hướng về hướng di chuyển
                if (t < 0.25f)
                {
                    float turnProgress = t / 0.25f;
                    transform.position = startPos;
                    transform.rotation = Quaternion.Slerp(startRot, targetSailRot, turnProgress);
                }
                else
                {
                    float moveProgress = (t - 0.25f) / 0.75f;
                    float easeT = moveProgress * moveProgress; // Gia tốc phóng ra biển
                    transform.position = Vector3.Lerp(startPos, sailDestination, easeT);

                    // Mũi thuyền luôn hướng thẳng theo hướng di chuyển (oceanDir)
                    float wavePitch = Mathf.Sin(moveProgress * Mathf.PI * 4f) * 1.5f;
                    float waveRoll = Mathf.Cos(moveProgress * Mathf.PI * 3f) * 1.8f;
                    transform.rotation = targetSailRot * Quaternion.Euler(wavePitch, 0f, waveRoll);
                }

                // Khói xả liên tục từ ống khói khi thuyền chạy nhanh (chỉ Level 3, 4, 5)
                smokeTimer += Time.deltaTime;
                if (smokeTimer >= 0.18f)
                {
                    smokeTimer = 0f;
                    if (VFXManager.Instance != null && smokestackPoint != null && smokestackPoint.gameObject.activeInHierarchy)
                    {
                        VFXManager.Instance.SpawnDustPuff(smokestackPoint.position, 1.1f);
                    }
                }

                // Bọt nước rẽ sóng liên tục ở đuôi tàu
                rippleTimer += Time.deltaTime;
                if (rippleTimer >= 0.28f)
                {
                    rippleTimer = 0f;
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.SpawnWaterRipple(transform.position - oceanDir * 2.5f, 2.8f);
                    }
                }

                // Khi thuyền đã chạy ra một đoạn (~35% thời gian), kích hoạt MÂY TỪ XUNG QUANH BAY VÀO GIỮA MÀN VÀ GIỮ NGUYÊN
                if (!triggeredCurtain && t >= 0.35f)
                {
                    triggeredCurtain = true;
                    if (VoyageTransitionUI.Instance != null)
                    {
                        Vector3 inDir = (dockedPosition - incomingStartPos).normalized;
                        Quaternion targetInRot = Quaternion.LookRotation(inDir, Vector3.up);

                        VoyageTransitionUI.Instance.StartEngulfInward(
                            targetWorld,
                            onMidpointAction: () =>
                            {
                                // HOÁN ĐỔI LEVEL & DỮ LIỆU TẠI MIDPOINT (KHI MÂY ĐANG GIỮ NGUYÊN CHE KÍN MÀN HÌNH)
                                if (WorldManager.Instance != null)
                                {
                                    if (isPreview)
                                    {
                                        WorldManager.Instance.PreviewWorld(targetWorldIndex);
                                    }
                                    else
                                    {
                                        WorldManager.Instance.SwitchToWorld(targetWorldIndex);
                                    }
                                }

                                // Đưa thuyền về vị trí ngoài khơi bến mới, MŨI HƯỚNG THẲNG VỀ BẾN CẬP
                                transform.position = incomingStartPos;
                                transform.rotation = targetInRot;
                            }
                        );
                    }
                }

                yield return null;
            }

            // 3. Đợi cho màn mây giữ nguyên ở giữa màn hình trong lúc chuẩn bị thuyền mới
            yield return new WaitForSeconds(0.45f);

            // =========================================================================
            // 4. KHI THUYỀN BẮT ĐẦU ĐI VÀO -> MÂY BẮT ĐẦU BAY RA XUNG QUANH ĐỀU THEO HÌNH TRÒN!
            // =========================================================================
            if (VoyageTransitionUI.Instance != null)
            {
                VoyageTransitionUI.Instance.StartDisperseOutward(sailInDuration);
            }
            elapsed = 0f;
            Vector3 returnDir = (dockedPosition - incomingStartPos).normalized;
            Quaternion returnHeadingRot = Quaternion.LookRotation(returnDir, Vector3.up);

            while (elapsed < sailInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / sailInDuration);
                float easeOutT = 1f - (1f - t) * (1f - t); // Giảm tốc nhẹ nhàng vào bến

                transform.position = Vector3.Lerp(incomingStartPos, dockedPosition, easeOutT);

                // Mũi thuyền luôn hướng chính xác về hướng di chuyển (returnDir)
                float wavePitch = Mathf.Sin(t * Mathf.PI * 3f) * 1.2f;
                float waveRoll = Mathf.Cos(t * Mathf.PI * 2f) * 1.5f;
                transform.rotation = returnHeadingRot * Quaternion.Euler(wavePitch, 0f, waveRoll);

                if (VFXManager.Instance != null && UnityEngine.Random.value < 0.35f)
                {
                    VFXManager.Instance.SpawnWaterRipple(transform.position - returnDir * 1.8f, 2.2f);
                }

                yield return null;
            }

            transform.position = dockedPosition;
            
            // Xoay nhẹ nhàng êm ái về góc neo đậu bến (dockedRotation)
            float settleElapsed = 0f;
            Quaternion finalDockRot = Quaternion.Euler(dockedRotation);
            while (settleElapsed < 0.35f)
            {
                settleElapsed += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(returnHeadingRot, finalDockRot, settleElapsed / 0.35f);
                yield return null;
            }
            transform.rotation = finalDockRot;

            // 5. Cập bến hoàn tất
            isTransitioning = false;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayContractFanfare(transform.position);
            }
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnConfetti(dockedPosition + Vector3.up * 2f, 1.8f);
            }
            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    dockedPosition + Vector3.up * 3.5f,
                    0,
                    DamageStyle.GoldBonus,
                    $"ARRIVED AT {targetWorld?.worldName?.ToUpper()}!"
                );
            }
        }
    }
}
