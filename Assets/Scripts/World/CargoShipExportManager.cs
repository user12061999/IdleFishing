using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Audio;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Inventory;
using IdleFishing.Logistics;
using IdleFishing.UI;
using IdleFishing.VFX;

namespace IdleFishing.World
{
    public enum CargoShipState
    {
        VoyageOut,          // Tàu đang ngoài khơi xa vận chuyển hàng đi bán
        Docking,            // Tàu cập bến cảng, hụ còi báo hiệu
        ContractActive,     // Hợp đồng xuất khẩu đang mở, chờ nhận hàng
        ContractFulfilled,  // Hoàn tất hợp đồng xuất khẩu thành công rực rỡ
        ContractExpired,    // Hết giờ hợp đồng, xuất cảng khẩn cấp
        Departing           // Tàu nhổ neo rẽ sóng ra khơi
    }

    [System.Serializable]
    public class OceanExportContract
    {
        public string contractName = "OCEAN CONTRACT";
        public ItemType requiredType = ItemType.WholeFish;
        public int requiredCount = 8;
        public int currentCount = 0;

        public int goldReward = 2500;
        public float totalDuration = 75f;
        public float remainingTime = 75f;

        public bool IsFulfilled => currentCount >= requiredCount;
    }

    public class CargoShipExportManager : MonoBehaviour
    {
        public static CargoShipExportManager Instance { get; private set; }

        [Header("Ship Ocean Motion")]
        [SerializeField] private float bobbingSpeed = 1.6f;
        [SerializeField] private float bobbingHeight = 0.14f;
        [SerializeField] private float rollAngle = 2.8f;

        [Header("Export Timers & State")]
        [SerializeField] private CargoShipState currentState = CargoShipState.VoyageOut;
        [SerializeField] private float voyageDuration = 55.0f; // Thời gian ra khơi giữa các chuyến
        [SerializeField] private float contractTimeLimit = 75.0f;
        [SerializeField] private OceanExportContract currentContract;

        [Header("Inventories & Logistics")]
        [SerializeField] private Inventory.Inventory exportPalletStorage;
        [SerializeField] private Inventory.Inventory harborIntakeStorage;

        [Header("3D Billboard UI")]
        [SerializeField] private GameObject billboardCanvasObj;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI rewardText;

        [Header("VFX Spawn Points")]
        [SerializeField] private Transform smokestackPoint;

        private Vector3 dockedPosition;
        private Quaternion dockedRotation;
        private float stateTimer = 0f;
        private Camera mainCamera;
        private int completedContractsCount = 0;
        private LogisticsRoute exportRouteFish;

        public CargoShipState CurrentState => currentState;
        public OceanExportContract CurrentContract => currentContract;
        public int CompletedContractsCount => completedContractsCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            dockedPosition = transform.position;
            dockedRotation = transform.rotation;
            mainCamera = Camera.main;
        }

        private void Start()
        {
            stateTimer = voyageDuration;
            currentState = CargoShipState.VoyageOut;
            CreateShipHUD();
            UpdateBillboardUI();
        }

        private void LateUpdate()
        {
            if (billboardCanvasObj != null)
            {
                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    billboardCanvasObj.transform.rotation = mainCamera.transform.rotation;
                }
            }
        }

        private void Update()
        {
            // 1. Hiệu ứng dập dềnh sóng biển
            float time = Time.time * bobbingSpeed;
            float offsetY = Mathf.Sin(time) * bobbingHeight;
            float pitch = Mathf.Sin(time * 0.7f) * (rollAngle * 0.6f);
            float roll = Mathf.Cos(time * 0.9f) * rollAngle;

            Vector3 basePos = dockedPosition;
            if (currentState == CargoShipState.VoyageOut)
            {
                // Khi ngoài khơi, tàu lùi xa ra biển một chút
                basePos += new Vector3(1.5f, -0.15f, 2.5f);
            }

            transform.position = basePos + new Vector3(0f, offsetY, 0f);
            transform.rotation = dockedRotation * Quaternion.Euler(pitch, 0f, roll);

            // 2. Quản lý trạng thái hợp đồng
            switch (currentState)
            {
                case CargoShipState.VoyageOut:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        StartCoroutine(DockShipSequence());
                    }
                    UpdateBillboardUI();
                    break;

                case CargoShipState.ContractActive:
                    if (currentContract != null)
                    {
                        currentContract.remainingTime -= Time.deltaTime;

                        // Kiểm tra hàng hóa được chở tới Pallet xuất khẩu
                        CheckPalletGoods();

                        if (currentContract.IsFulfilled)
                        {
                            StartCoroutine(FulfillContractSequence());
                        }
                        else if (currentContract.remainingTime <= 0f)
                        {
                            StartCoroutine(ExpireContractSequence());
                        }
                    }
                    UpdateBillboardUI();
                    break;

                case CargoShipState.Departing:
                    // Tàu đang chuẩn bị quay lại chu kỳ VoyageOut
                    break;
            }
        }

        public void Setup(
            Inventory.Inventory palletStorage,
            Inventory.Inventory harborStorage,
            Transform smokeTransform = null)
        {
            exportPalletStorage = palletStorage;
            harborIntakeStorage = harborStorage;
            smokestackPoint = smokeTransform;
        }

        private void CreateShipHUD()
        {
            if (billboardCanvasObj != null) return;

            billboardCanvasObj = new GameObject("Ship_Contract_HUD");
            billboardCanvasObj.transform.SetParent(transform, false);
            billboardCanvasObj.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            billboardCanvasObj.transform.localScale = Vector3.one * 0.008f;

            Canvas canvas = billboardCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rect = billboardCanvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250, 105);

            // Background Card
            GameObject bgObj = new GameObject("HUD_BG");
            bgObj.transform.SetParent(billboardCanvasObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.10f, 0.16f, 0.92f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(250, 105);

            // Title Text
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(bgObj.transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 15;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.85f, 0.3f);
            titleText.ApplyFont(UIFontCategory.Header);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0, 34);
            tRect.sizeDelta = new Vector2(240, 24);

            // Status Text (Goods required)
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(bgObj.transform, false);
            statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 13;
            statusText.fontStyle = FontStyles.Bold;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;
            statusText.ApplyFont(UIFontCategory.Default);
            RectTransform sRect = statusObj.GetComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0, 10);
            sRect.sizeDelta = new Vector2(240, 22);

            // Timer Text
            GameObject timerObj = new GameObject("Timer");
            timerObj.transform.SetParent(bgObj.transform, false);
            timerText = timerObj.AddComponent<TextMeshProUGUI>();
            timerText.fontSize = 12;
            timerText.fontStyle = FontStyles.Bold;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.color = new Color(0.95f, 0.45f, 0.45f);
            timerText.ApplyFont(UIFontCategory.Numbers);
            RectTransform timRect = timerObj.GetComponent<RectTransform>();
            timRect.anchoredPosition = new Vector2(0, -12);
            timRect.sizeDelta = new Vector2(240, 20);

            // Reward Text
            GameObject rewardObj = new GameObject("Reward");
            rewardObj.transform.SetParent(bgObj.transform, false);
            rewardText = rewardObj.AddComponent<TextMeshProUGUI>();
            rewardText.fontSize = 13;
            rewardText.fontStyle = FontStyles.Bold;
            rewardText.alignment = TextAlignmentOptions.Center;
            rewardText.color = new Color(0.25f, 1f, 0.65f);
            rewardText.ApplyFont(UIFontCategory.Button);
            RectTransform rRect = rewardObj.GetComponent<RectTransform>();
            rRect.anchoredPosition = new Vector2(0, -32);
            rRect.sizeDelta = new Vector2(240, 22);
        }

        private void UpdateBillboardUI()
        {
            if (billboardCanvasObj == null) return;

            switch (currentState)
            {
                case CargoShipState.VoyageOut:
                    if (titleText != null) titleText.text = "<color=#94A3B8>SHIP OUT AT SEA</color>";
                    if (statusText != null) statusText.text = "Transporting previous shipment";
                    if (timerText != null) timerText.text = $"Docking in: <color=#38BDF8>{Mathf.CeilToInt(stateTimer)}s</color>";
                    if (rewardText != null) rewardText.text = $"Completed voyages: {completedContractsCount}";
                    break;

                case CargoShipState.Docking:
                    if (titleText != null) titleText.text = "<color=#FBBF24>SHIP DOCKING...</color>";
                    if (statusText != null) statusText.text = "Preparing fresh fish export contract!";
                    if (timerText != null) timerText.text = "TOOT TOOT!";
                    if (rewardText != null) rewardText.text = "";
                    break;

                case CargoShipState.ContractActive:
                    if (currentContract != null)
                    {
                        if (titleText != null) titleText.text = $"<color=#FBBF24>[EXPORT] {currentContract.contractName}</color>";
                        if (statusText != null)
                        {
                            string c1 = currentContract.currentCount >= currentContract.requiredCount ? "#34D399" : "#38BDF8";
                            statusText.text = $"Fresh Whole Fish: <color={c1}>{currentContract.currentCount}/{currentContract.requiredCount}</color>";
                        }
                        if (timerText != null)
                        {
                            int sec = Mathf.Max(0, Mathf.CeilToInt(currentContract.remainingTime));
                            timerText.text = $"Time left: <color=#F87171>{sec}s</color>";
                        }
                        if (rewardText != null)
                        {
                            rewardText.text = $"Reward: <color=#FDE047>+{NumberFormatter.Format(currentContract.goldReward)} GOLD</color>";
                        }
                    }
                    break;

                case CargoShipState.ContractFulfilled:
                    if (titleText != null) titleText.text = "<color=#34D399>[*] CONTRACT COMPLETED! [*]</color>";
                    if (statusText != null) statusText.text = "All fresh fish loaded onto ship!";
                    if (timerText != null) timerText.text = "<color=#FBBF24>HUGE REWARD CLAIMED!</color>";
                    if (rewardText != null) rewardText.text = "Ship preparing to depart...";
                    break;

                case CargoShipState.ContractExpired:
                    if (titleText != null) titleText.text = "<color=#F87171>CONTRACT EXPIRED</color>";
                    if (statusText != null) statusText.text = "Paid for loaded fish";
                    if (timerText != null) timerText.text = "Ship departing...";
                    if (rewardText != null) rewardText.text = "";
                    break;
            }
        }

        private IEnumerator DockShipSequence()
        {
            currentState = CargoShipState.Docking;
            UpdateBillboardUI();

            // Còi tàu cập cảng
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayChestOpen(transform.position);
            }

            // Gợn sóng nước và khói tàu
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnWaterRipple(transform.position + Vector3.forward * 1.5f, 2.2f);
                if (smokestackPoint != null)
                {
                    VFXManager.Instance.SpawnDustPuff(smokestackPoint.position, 1.2f);
                }
            }

            yield return new WaitForSeconds(1.8f);

            // Mở hợp đồng xuất khẩu mới
            GenerateNewContract();
            currentState = CargoShipState.ContractActive;
            RegisterLogisticsExportRoutes();

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 3f,
                    currentContract.goldReward,
                    DamageStyle.GoldBonus,
                    "OCEAN SHIP DOCKED!"
                );
            }
        }

        private void GenerateNewContract()
        {
            currentContract = new OceanExportContract();
            currentContract.contractName = $"OCEAN CONTRACT #{completedContractsCount + 1}";
            currentContract.requiredType = ItemType.WholeFish;
            currentContract.requiredCount = Random.Range(7, 13);
            currentContract.currentCount = 0;

            int worldOrder = (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null) 
                ? WorldManager.Instance.CurrentWorld.worldOrder 
                : 1;
            int baseReward = 2000 + (completedContractsCount * 450);
            currentContract.goldReward = Mathf.RoundToInt(baseReward * (1.0f + (worldOrder - 1) * 2.5f));
            currentContract.totalDuration = contractTimeLimit;
            currentContract.remainingTime = contractTimeLimit;
        }

        private void RegisterLogisticsExportRoutes()
        {
            if (LogisticsManager.Instance == null || exportPalletStorage == null) return;

            Vector3 palletPos = exportPalletStorage.transform.position;

            // Route: Chuyển Cá Nguyên Con 3D từ Kho Bến Cảng ra Pallet Tàu hàng (Ưu tiên cao nhất)
            if (harborIntakeStorage != null)
            {
                exportRouteFish = new LogisticsRoute
                {
                    routeName = "Ship_Export_WholeFish",
                    sourceInventory = harborIntakeStorage,
                    pickupPosition = harborIntakeStorage.transform.position,
                    targetInventory = exportPalletStorage,
                    dropoffPosition = palletPos,
                    itemType = ItemType.WholeFish,
                    priority = 0,
                    isExportContract = true,
                    waypointsToDropoff = new List<Vector3>
                    {
                        new Vector3(-2.5f, 0.60f, 4.75f), // Entrance ramp up to pier deck
                        new Vector3(3.5f, 0.60f, 5.2f),   // Pier southern walkway
                        new Vector3(5.2f, 0.60f, 8.0f)    // Pier eastern boardwalk
                    },
                    waypointsToPickup = new List<Vector3>
                    {
                        new Vector3(5.2f, 0.60f, 8.0f),   // Pier eastern boardwalk
                        new Vector3(3.5f, 0.60f, 5.2f),   // Pier southern walkway
                        new Vector3(-2.5f, 0.60f, 4.75f)  // Entrance ramp down to harbor ground
                    }
                };
                LogisticsManager.Instance.RegisterDynamicRoute(exportRouteFish);
            }
        }

        private void UnregisterLogisticsExportRoutes()
        {
            if (LogisticsManager.Instance == null) return;
            if (exportRouteFish != null) LogisticsManager.Instance.UnregisterRoute(exportRouteFish);
            exportRouteFish = null;
        }

        private void CheckPalletGoods()
        {
            if (exportPalletStorage == null || currentContract == null) return;

            // Nhận Cá Nguyên Con 3D
            if (currentContract.currentCount < currentContract.requiredCount)
            {
                if (exportPalletStorage.TryGetFirstItemOfType(currentContract.requiredType, out ItemData fishItem))
                {
                    if (exportPalletStorage.TryRemove(fishItem, 1))
                    {
                        currentContract.currentCount++;
                        OnGoodsDeposited(fishItem.ItemName, currentContract.currentCount, currentContract.requiredCount);
                    }
                }
            }
        }

        private void OnGoodsDeposited(string itemName, int current, int target)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIPop(transform.position);
            }

            if (FloatingDamageManager.Instance != null)
            {
                Vector3 spawnPos = exportPalletStorage != null ? exportPalletStorage.transform.position : transform.position;
                FloatingDamageManager.Instance.SpawnDamage(
                    spawnPos + Vector3.up * 1.5f,
                    0,
                    DamageStyle.GoldBonus,
                    $"+1 {itemName} ({current}/{target})"
                );
            }
        }

        private IEnumerator FulfillContractSequence()
        {
            currentState = CargoShipState.ContractFulfilled;
            UnregisterLogisticsExportRoutes();
            UpdateBillboardUI();

            completedContractsCount++;
            int reward = currentContract.goldReward;

            // 1. Thưởng vàng kinh tế
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(reward);
            }

            // 2. Hiệu ứng ăn mừng rực rỡ: Pháo hoa Confetti, Tiền bay, Âm thanh
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnConfetti(transform.position + Vector3.up * 2.5f, 1.6f);
            }

            if (FlyingCoinManager.Instance != null)
            {
                FlyingCoinManager.Instance.PlayFlyingCoins(transform.position + Vector3.up * 2.0f, 8);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayContractFanfare(transform.position);
                AudioManager.Instance.PlayChestOpen(transform.position);
            }

            if (IdleFishing.CameraControl.MobilePortraitCameraController.Instance != null)
            {
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerShake(0.35f, 0.40f);
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerPunchFOV(-2.5f);
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 3.2f,
                    reward,
                    DamageStyle.GoldBonus,
                    $"[*] EXPORT COMPLETE! +{NumberFormatter.Format(reward)} G [*]"
                );
            }

            yield return new WaitForSeconds(3.5f);
            StartCoroutine(DepartShipSequence());
        }

        private IEnumerator ExpireContractSequence()
        {
            currentState = CargoShipState.ContractExpired;
            UnregisterLogisticsExportRoutes();
            UpdateBillboardUI();

            // Tính tiền thanh toán một phần dựa trên số cá đã nhận
            int totalRequired = currentContract.requiredCount;
            int totalDelivered = currentContract.currentCount;
            int partialReward = Mathf.RoundToInt(((float)totalDelivered / Mathf.Max(1, totalRequired)) * currentContract.goldReward * 0.65f);

            if (partialReward > 0)
            {
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddCoins(partialReward);
                }
                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        transform.position + Vector3.up * 2.5f,
                        partialReward,
                        DamageStyle.GoldBonus,
                        $"SHIP DEPARTED: EARNED +{NumberFormatter.Format(partialReward)} G"
                    );
                }
            }

            yield return new WaitForSeconds(2.5f);
            StartCoroutine(DepartShipSequence());
        }

        private IEnumerator DepartShipSequence()
        {
            currentState = CargoShipState.Departing;

            // Khói tàu & gợn sóng khi nhổ neo
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnWaterRipple(transform.position + Vector3.back * 1.5f, 2.0f);
                if (smokestackPoint != null)
                {
                    VFXManager.Instance.SpawnDustPuff(smokestackPoint.position, 1.2f);
                }
            }

            yield return new WaitForSeconds(2.0f);

            // Bắt đầu chu kỳ ra khơi kế tiếp
            stateTimer = voyageDuration;
            currentState = CargoShipState.VoyageOut;
            UpdateBillboardUI();
        }

        private void OnMouseDown()
        {
            if (currentState == CargoShipState.ContractActive && currentContract != null)
            {
                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        transform.position + Vector3.up * 2.2f,
                        0,
                        DamageStyle.GoldBonus,
                        $"Fresh Fish: {currentContract.currentCount}/{currentContract.requiredCount}"
                    );
                }
            }
            else if (currentState == CargoShipState.VoyageOut)
            {
                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        transform.position + Vector3.up * 2.2f,
                        0,
                        DamageStyle.FishAttack,
                        $"OUT AT SEA (DOCKING IN {Mathf.CeilToInt(stateTimer)}s)"
                    );
                }
            }
        }
    }
}
