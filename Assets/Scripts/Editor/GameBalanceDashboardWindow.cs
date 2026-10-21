using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using IdleFishing.Data;
using IdleFishing.Audio;
using IdleFishing.VFX;

namespace IdleFishing.Editor
{
    public class GameBalanceDashboardWindow : EditorWindow
    {
        [MenuItem("Tools/Idle Fishing/Game Balance Dashboard ⚙️", false, 10)]
        public static void Open()
        {
            var window = GetWindow<GameBalanceDashboardWindow>("Balance Dashboard");
            window.minSize = new Vector2(780, 620);
            window.Show();
        }

        private enum Tab
        {
            FishingAndEvents,
            FishPrices,
            Upgrades,
            WorldProgression,
            Aquarium,
            AudioAndVFX,
            PresetsAndTools
        }

        private Tab currentTab = Tab.FishingAndEvents;
        private Vector2 scrollPos;
        private string fishSearchText = "";
        private float bulkPriceMultiplier = 1.0f;
        private int selectedUpgradeWorldIndex = 0;

        // Cached Data Assets
        private GameBalanceConfig balanceConfig;
        private UpgradeConfig upgradeConfig;
        private List<WorldData> worldAssets = new List<WorldData>();
        private List<ItemData> fishItemAssets = new List<ItemData>();
        private AudioManager audioManager;
        private VFXManager vfxManager;
        private float sfxVolume = 0.85f;
        private float bgmVolume = 0.45f;
        private float ambientVolume = 0.28f;
        private bool isMuted = false;
        private bool isFrenzyAuraActive = false;

        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle tabButtonStyle;
        private GUIStyle cardBoxStyle;

        private void OnEnable()
        {
            LoadAllAssets();
        }

        private void LoadAllAssets()
        {
            // 1. Balance Config
            balanceConfig = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>("Assets/GameData/Balance/DefaultGameBalanceConfig.asset");

            // 2. Upgrade Config
            upgradeConfig = AssetDatabase.LoadAssetAtPath<UpgradeConfig>("Assets/GameData/Upgrades/DefaultUpgradeConfig.asset");

            // 3. World Data (Level 1 - 5)
            worldAssets.Clear();
            for (int i = 1; i <= 5; i++)
            {
                var w = AssetDatabase.LoadAssetAtPath<WorldData>($"Assets/GameData/Worlds/world_{i}.asset");
                if (w != null) worldAssets.Add(w);
            }

            // 4. All Fish Items
            fishItemAssets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/GameData/Items" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null)
                {
                    fishItemAssets.Add(item);
                }
            }

            fishItemAssets.Sort((a, b) => a.BasePrice.CompareTo(b.BasePrice));

            // 5. Audio & VFX Managers & Prefs
            audioManager = UnityEngine.Object.FindObjectOfType<AudioManager>();
            vfxManager = UnityEngine.Object.FindObjectOfType<VFXManager>();
            sfxVolume = PlayerPrefs.GetFloat("IdleFishing_SFX_Volume", 0.85f);
            bgmVolume = PlayerPrefs.GetFloat("IdleFishing_BGM_Volume", 0.45f);
            ambientVolume = PlayerPrefs.GetFloat("IdleFishing_Ambient_Volume", 0.28f);
            isMuted = PlayerPrefs.GetInt("IdleFishing_Muted", 0) == 1;
        }

        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    margin = new RectOffset(0, 0, 8, 4)
                };
            }

            if (cardBoxStyle == null)
            {
                cardBoxStyle = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(12, 12, 10, 10),
                    margin = new RectOffset(0, 0, 6, 6)
                };
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // Top Header Bar
            DrawTopBar();

            // Navigation Tabs Toolbar
            DrawTabsToolbar();

            // Main Content Area with ScrollView
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.Space(8);

            switch (currentTab)
            {
                case Tab.FishingAndEvents:
                    DrawFishingAndEventsTab();
                    break;
                case Tab.FishPrices:
                    DrawFishPricesTab();
                    break;
                case Tab.Upgrades:
                    DrawUpgradesTab();
                    break;
                case Tab.WorldProgression:
                    DrawWorldProgressionTab();
                    break;
                case Tab.Aquarium:
                    DrawAquariumTab();
                    break;
                case Tab.AudioAndVFX:
                    DrawAudioAndVFXTab();
                    break;
                case Tab.PresetsAndTools:
                    DrawPresetsAndToolsTab();
                    break;
            }

            EditorGUILayout.Space(16);
            EditorGUILayout.EndScrollView();

            // Bottom Save Actions
            DrawBottomBar();
        }

        private void DrawTopBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🎮 Idle Fishing - Game Balance Dashboard", headerStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🔄 Tải lại (Reload)", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                LoadAllAssets();
                ShowNotification(new GUIContent("Đã tải lại toàn bộ chỉ số từ ổ đĩa!"));
            }

            if (GUILayout.Button("💾 LƯU TẤT CẢ (Save All)", EditorStyles.toolbarButton, GUILayout.Width(140)))
            {
                SaveAllAssets();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabsToolbar()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            string[] tabNames = new[]
            {
                "🎣 Câu Cá & Sự Kiện",
                "🐟 Giá Bán Cá",
                "⚡ Nâng Cấp Core",
                "🗺️ Mở Khóa Đảo",
                "🐠 Bể Cá Thủy Cung",
                "🔊 Âm Thanh & VFX",
                "🛠️ Presets & Công Cụ"
            };

            for (int i = 0; i < tabNames.Length; i++)
            {
                bool isSelected = (int)currentTab == i;
                GUI.backgroundColor = isSelected ? new Color(0.35f, 0.75f, 1f, 1f) : Color.white;
                if (GUILayout.Button(tabNames[i], EditorStyles.miniButtonMid, GUILayout.Height(28)))
                {
                    currentTab = (Tab)i;
                    GUI.FocusControl(null);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        // ==========================================
        // TAB 1: FISHING & EVENTS
        // ==========================================
        private void DrawFishingAndEventsTab()
        {
            if (balanceConfig == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy DefaultGameBalanceConfig.asset!", MessageType.Error);
                return;
            }

            var so = new SerializedObject(balanceConfig);
            so.Update();

            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("🎣 1. CẦN THỦ & CÂU CÁ CƠ BẢN", sectionStyle);
            EditorGUILayout.HelpBox("Thời gian kéo một mẻ cá và số cá tối đa có thể nằm trên cầu tàu chờ thợ bắt.", MessageType.None);

            SerializedProperty propCastTime = so.FindProperty("fishermanBaseCastTime");
            propCastTime.floatValue = EditorGUILayout.Slider("Thời gian quăng & giật cần (giây)", propCastTime.floatValue, 1.0f, 12.0f);

            SerializedProperty propMaxPending = so.FindProperty("maxPendingFishOnGround");
            propMaxPending.intValue = EditorGUILayout.IntSlider("Số cá tối đa nằm trên bến", propMaxPending.intValue, 1, 10);

            SerializedProperty propClickDamage = so.FindProperty("playerClickBaseDamage");
            propClickDamage.intValue = EditorGUILayout.IntSlider("Sát thương khi người chơi Tap vào cá", propClickDamage.intValue, 1, 100);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("👑 2. CÁ BOSS KHỔNG LỒ (GIANT APEX BOSS)", sectionStyle);
            EditorGUILayout.HelpBox("Cá Boss vàng khổng lồ mang lại lượng vàng thưởng lớn khi bị hạ gục.", MessageType.None);

            SerializedProperty propBossChance = so.FindProperty("bossSpawnChance");
            propBossChance.floatValue = EditorGUILayout.Slider("Tỉ lệ xuất hiện Boss (%)", propBossChance.floatValue * 100f, 1f, 50f) / 100f;

            SerializedProperty propBossHp = so.FindProperty("bossHealthMultiplier");
            propBossHp.floatValue = EditorGUILayout.Slider("Hệ số Máu của Boss (x HP)", propBossHp.floatValue, 1.5f, 10.0f);

            SerializedProperty propBossGold = so.FindProperty("bossBaseBonusGold");
            propBossGold.intValue = EditorGUILayout.IntSlider("Vàng thưởng cơ bản khi bắt Boss", propBossGold.intValue, 50, 2000);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("💎 3. RƯƠNG KHO BÁU CHÌM (SUNKEN TREASURE)", sectionStyle);
            EditorGUILayout.HelpBox("Thỉnh thoảng cần thủ sẽ câu trúng rương vàng chìm dưới đáy biển.", MessageType.None);

            SerializedProperty propChestChance = so.FindProperty("treasureChestSpawnChance");
            propChestChance.floatValue = EditorGUILayout.Slider("Tỉ lệ câu trúng rương (%)", propChestChance.floatValue * 100f, 1f, 30f) / 100f;

            SerializedProperty propChestTaps = so.FindProperty("treasureChestRequiredTaps");
            propChestTaps.intValue = EditorGUILayout.IntSlider("Số lần gõ để mở rương", propChestTaps.intValue, 1, 15);

            EditorGUILayout.BeginHorizontal();
            SerializedProperty propChestMin = so.FindProperty("treasureChestMinGold");
            SerializedProperty propChestMax = so.FindProperty("treasureChestMaxGold");
            propChestMin.intValue = EditorGUILayout.IntField("Vàng rương tối thiểu", propChestMin.intValue);
            propChestMax.intValue = EditorGUILayout.IntField("Vàng rương tối đa", propChestMax.intValue);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("🔥 4. CHẾ ĐỘ CUỒNG NHIỆT (FISHING FRENZY)", sectionStyle);
            EditorGUILayout.HelpBox("Khi thanh Frenzy đầy 100%, tốc độ câu và sát thương tăng vọt.", MessageType.None);

            SerializedProperty propFrenzyDur = so.FindProperty("frenzyDuration");
            propFrenzyDur.floatValue = EditorGUILayout.Slider("Thời lượng cuồng nhiệt (giây)", propFrenzyDur.floatValue, 5f, 60f);

            SerializedProperty propFrenzySpeed = so.FindProperty("frenzySpeedMultiplier");
            propFrenzySpeed.floatValue = EditorGUILayout.Slider("Nhân tốc độ câu trong Frenzy (x)", propFrenzySpeed.floatValue, 1.5f, 5.0f);

            SerializedProperty propChargeFish = so.FindProperty("frenzyChargePerFish");
            propChargeFish.floatValue = EditorGUILayout.Slider("% Nạp Frenzy mỗi con cá kéo lên", propChargeFish.floatValue, 1f, 20f);

            SerializedProperty propChargeChest = so.FindProperty("frenzyChargePerChest");
            propChargeChest.floatValue = EditorGUILayout.Slider("% Nạp Frenzy khi mở rương báu", propChargeChest.floatValue, 10f, 100f);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("💤 5. THU NHẬP NGOẠI TUYẾN (OFFLINE EARNINGS)", sectionStyle);
            SerializedProperty propOffRate = so.FindProperty("offlineBaseCoinsPerSecond");
            propOffRate.floatValue = EditorGUILayout.FloatField("Vàng cơ bản mỗi giây khi Offline", propOffRate.floatValue);

            SerializedProperty propOffHours = so.FindProperty("offlineMaxHours");
            propOffHours.floatValue = EditorGUILayout.Slider("Thời gian tích lũy tối đa (giờ)", propOffHours.floatValue, 1f, 24f);
            EditorGUILayout.EndVertical();

            so.ApplyModifiedProperties();
        }

        // ==========================================
        // TAB 2: FISH PRICES & ITEMS
        // ==========================================
        private void DrawFishPricesTab()
        {
            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("🐟 BẢNG GIÁ TẤT CẢ CÁC LOÀI HẢI SẢN (ITEM DATA)", sectionStyle);
            EditorGUILayout.HelpBox("Chỉnh sửa giá bán trực tiếp của từng loài cá. Giá này quyết định doanh thu khi khách mua cá tại quầy và thu nhập vé bể cá.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍 Tìm kiếm cá:", GUILayout.Width(100));
            fishSearchText = EditorGUILayout.TextField(fishSearchText, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("X", GUILayout.Width(24))) fishSearchText = "";
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // Table Header
            EditorGUILayout.BeginHorizontal("Toolbar");
            GUILayout.Label("Mã Cá (ID)", EditorStyles.miniBoldLabel, GUILayout.Width(150));
            GUILayout.Label("Tên Hiển Thị", EditorStyles.miniBoldLabel, GUILayout.Width(180));
            GUILayout.Label("Giá Bán Cơ Bản (Vàng)", EditorStyles.miniBoldLabel, GUILayout.Width(160));
            GUILayout.Label("Thao tác", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();

            int count = 0;
            for (int i = 0; i < fishItemAssets.Count; i++)
            {
                var item = fishItemAssets[i];
                if (item == null) continue;

                if (!string.IsNullOrEmpty(fishSearchText))
                {
                    bool match = item.ItemId.IndexOf(fishSearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 item.ItemName.IndexOf(fishSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!match) continue;
                }

                count++;
                EditorGUILayout.BeginHorizontal(count % 2 == 0 ? "CN Box" : "HelpBox");

                EditorGUILayout.LabelField(item.ItemId, GUILayout.Width(150));
                EditorGUILayout.LabelField(item.ItemName, EditorStyles.boldLabel, GUILayout.Width(180));

                var so = new SerializedObject(item);
                so.Update();
                var propPrice = so.FindProperty("basePrice");
                int newPrice = EditorGUILayout.IntField(propPrice.intValue, GUILayout.Width(120));
                if (newPrice != propPrice.intValue)
                {
                    propPrice.intValue = Mathf.Max(1, newPrice);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(item);
                }

                GUILayout.Label("G", GUILayout.Width(25));

                if (GUILayout.Button("Chọn Asset", EditorStyles.miniButton, GUILayout.Width(80)))
                {
                    Selection.activeObject = item;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (count == 0)
            {
                EditorGUILayout.HelpBox("Không tìm thấy loài cá nào phù hợp với từ khóa.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();

            // Bulk Multiplier Tool
            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("⚡ CÔNG CỤ NHÂN GIÁ HÀNG LOẠT (BULK PRICE MULTIPLIER)", sectionStyle);
            EditorGUILayout.BeginHorizontal();
            bulkPriceMultiplier = EditorGUILayout.Slider("Hệ số nhân toàn bộ giá cá", bulkPriceMultiplier, 0.5f, 3.0f);
            if (GUILayout.Button($"Áp dụng x{bulkPriceMultiplier:0.0} cho tất cả cá", GUILayout.Width(220)))
            {
                if (EditorUtility.DisplayDialog("Xác nhận nhân giá cá", $"Bạn có chắc chắn muốn nhân giá bán của TOÀN BỘ {fishItemAssets.Count} loài cá với x{bulkPriceMultiplier:0.0}?", "Đồng ý", "Hủy"))
                {
                    foreach (var item in fishItemAssets)
                    {
                        if (item == null) continue;
                        var so = new SerializedObject(item);
                        so.Update();
                        var p = so.FindProperty("basePrice");
                        p.intValue = Mathf.Max(1, Mathf.RoundToInt(p.intValue * bulkPriceMultiplier));
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(item);
                    }
                    AssetDatabase.SaveAssets();
                    ShowNotification(new GUIContent($"Đã nhân giá toàn bộ cá với x{bulkPriceMultiplier:0.0}!"));
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // ==========================================
        // TAB 3: UPGRADES
        // ==========================================
        private void DrawUpgradesTab()
        {
            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("⚡ 3 TRỤ CỘT NÂNG CẤP TINH GỌN (CORE UPGRADES)", sectionStyle);
            EditorGUILayout.HelpBox("Chọn từng Hòn Đảo (World 1 - 5) để cân chỉnh chi phí nâng cấp riêng biệt, hoặc chọn 'Default' làm mặc định.", MessageType.Info);

            string[] worldTabLabels = new string[] { "🏝️ Đảo 1", "🏝️ Đảo 2", "🏝️ Đảo 3", "🏝️ Đảo 4", "🏝️ Đảo 5", "⚙️ Default" };
            selectedUpgradeWorldIndex = GUILayout.Toolbar(selectedUpgradeWorldIndex, worldTabLabels, GUILayout.Height(28));
            EditorGUILayout.Space(6);

            UpgradeConfig activeConfig = null;
            string activeTitle = "Default Fallback";

            if (selectedUpgradeWorldIndex >= 0 && selectedUpgradeWorldIndex < worldAssets.Count)
            {
                var w = worldAssets[selectedUpgradeWorldIndex];
                if (w != null && w.worldUpgradeConfig != null)
                {
                    activeConfig = w.worldUpgradeConfig;
                    activeTitle = $"Level {w.worldOrder}: {w.worldName} ({activeConfig.name})";
                }
            }

            if (activeConfig == null)
            {
                activeConfig = upgradeConfig;
                activeTitle = activeConfig != null ? $"Default Fallback ({activeConfig.name})" : "Không tìm thấy";
            }

            if (activeConfig == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy UpgradeConfig cho mục đã chọn!", MessageType.Error);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField($"📌 Đang chỉnh sửa cấu hình: <b>{activeTitle}</b>", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            var so = new SerializedObject(activeConfig);
            so.Update();
            var propUpgrades = so.FindProperty("upgrades");

            for (int i = 0; i < propUpgrades.arraySize; i++)
            {
                var entry = propUpgrades.GetArrayElementAtIndex(i);
                var title = entry.FindPropertyRelative("title").stringValue;
                var baseCost = entry.FindPropertyRelative("baseCost");
                var costMult = entry.FindPropertyRelative("costMultiplier");
                var baseVal = entry.FindPropertyRelative("baseValue");
                var incVal = entry.FindPropertyRelative("valueIncreasePerLevel");
                var maxLv = entry.FindPropertyRelative("maxLevel");

                EditorGUILayout.BeginVertical("HelpBox");
                EditorGUILayout.LabelField($"⭐ [{i + 1}] {title.ToUpper()}", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                baseCost.intValue = EditorGUILayout.IntField("Giá cơ bản (Level 0)", baseCost.intValue);
                costMult.floatValue = EditorGUILayout.Slider("Hệ số nhân giá mỗi cấp", costMult.floatValue, 1.05f, 2.0f);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                baseVal.floatValue = EditorGUILayout.FloatField("Giá trị cơ bản", baseVal.floatValue);
                incVal.floatValue = EditorGUILayout.FloatField("Tăng thêm mỗi cấp (+%)", incVal.floatValue);
                maxLv.intValue = EditorGUILayout.IntSlider("Cấp độ tối đa", maxLv.intValue, 10, 100);
                EditorGUILayout.EndHorizontal();

                // Cost Simulation Preview
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal("CN Box");
                GUILayout.Label("Dự toán giá:", EditorStyles.miniBoldLabel, GUILayout.Width(80));
                for (int lv = 0; lv <= 15; lv += 5)
                {
                    int simCost = Mathf.RoundToInt(baseCost.intValue * Mathf.Pow(costMult.floatValue, lv));
                    GUILayout.Label($"Lv.{lv}: {simCost:N0}G", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            so.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
        }

        // ==========================================
        // TAB 4: WORLD PROGRESSION
        // ==========================================
        private void DrawWorldProgressionTab()
        {
            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("🗺️ TIẾN TRÌNH 5 THẾ GIỚI / HÒN ĐẢO (WORLDS 1 - 5)", sectionStyle);
            EditorGUILayout.HelpBox("Chỉnh sửa số vàng yêu cầu để mua bệ mở khóa đảo tiếp theo (Pad_WorldPurchase) và số cấp nâng cấp tối thiểu yêu cầu.", MessageType.Info);

            for (int i = 0; i < worldAssets.Count; i++)
            {
                var w = worldAssets[i];
                if (w == null) continue;

                var so = new SerializedObject(w);
                so.Update();

                var propName = so.FindProperty("worldName");
                var propStartCoins = so.FindProperty("startingCoins");
                var propReqCoins = so.FindProperty("unlockRequiredCoins");
                var propReqUpgrades = so.FindProperty("unlockRequiredUpgrades");

                EditorGUILayout.BeginVertical("HelpBox");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🏝️ LEVEL {w.worldOrder}: {propName.stringValue}", EditorStyles.boldLabel, GUILayout.Width(250));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Xem Asset", EditorStyles.miniButton, GUILayout.Width(75)))
                {
                    Selection.activeObject = w;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                propStartCoins.intValue = EditorGUILayout.IntField("Vàng khởi đầu", propStartCoins.intValue);

                if (w.worldOrder == 1)
                {
                    EditorGUILayout.LabelField("Đảo khởi đầu (Mở sẵn miễn phí)");
                }
                else
                {
                    propReqCoins.intValue = EditorGUILayout.IntField("Vàng yêu cầu mở đảo", propReqCoins.intValue);
                    propReqUpgrades.intValue = EditorGUILayout.IntSlider("Số cấp nâng cấp yêu cầu", propReqUpgrades.intValue, 0, 50);
                }
                EditorGUILayout.EndHorizontal();

                so.ApplyModifiedProperties();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();
        }

        // ==========================================
        // TAB 5: AQUARIUM
        // ==========================================
        private void DrawAquariumTab()
        {
            if (balanceConfig == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy DefaultGameBalanceConfig.asset!", MessageType.Error);
                return;
            }

            var so = new SerializedObject(balanceConfig);
            so.Update();

            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("🐠 1. SỨC CHỨA & NÂNG CẤP BỂ CÁ", sectionStyle);
            EditorGUILayout.HelpBox("Quy định sức chứa cá tối đa và chi phí mở rộng bể kính nuôi cá.", MessageType.None);

            SerializedProperty propBaseCap = so.FindProperty("aquariumBaseCapacity");
            propBaseCap.intValue = EditorGUILayout.IntSlider("Sức chứa cơ bản ban đầu", propBaseCap.intValue, 4, 20);

            SerializedProperty propSlotsPerUp = so.FindProperty("aquariumCapacitySlotsPerUpgrade");
            propSlotsPerUp.intValue = EditorGUILayout.IntSlider("Số chỗ nuôi thêm mỗi lần nâng cấp", propSlotsPerUp.intValue, 1, 5);

            EditorGUILayout.BeginHorizontal();
            SerializedProperty propCapBaseCost = so.FindProperty("aquariumCapacityUpgradeBaseCost");
            propCapBaseCost.intValue = EditorGUILayout.IntField("Giá nâng cấp cơ bản (Level 1)", propCapBaseCost.intValue);

            SerializedProperty propCapMult = so.FindProperty("aquariumCapacityUpgradeMultiplier");
            propCapMult.floatValue = EditorGUILayout.Slider("Hệ số nhân giá nâng cấp", propCapMult.floatValue, 1.2f, 3.0f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("✨ 2. NÂNG CẤP TRANG TRÍ (DECORATION) & DOANH THU VÉ", sectionStyle);
            EditorGUILayout.HelpBox("Trang trí đèn LED, rạn san hô và cung điện ngọc trai giúp tăng hệ số nhân tiền vé thu từ du khách tham quan.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            SerializedProperty propDecorCost = so.FindProperty("aquariumDecorUpgradeBaseCost");
            propDecorCost.intValue = EditorGUILayout.IntField("Giá nâng cấp Decor cơ bản", propDecorCost.intValue);

            SerializedProperty propDecorMult = so.FindProperty("aquariumDecorUpgradeMultiplier");
            propDecorMult.floatValue = EditorGUILayout.Slider("Hệ số nhân giá Decor", propDecorMult.floatValue, 1.5f, 3.5f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Hệ Số Nhân Doanh Thu Vé Theo Cấp Độ:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Lv.1 (Căn bản): x1.0");

            SerializedProperty propM2 = so.FindProperty("aquariumDecorMultiplierLevel2");
            propM2.floatValue = EditorGUILayout.FloatField("Lv.2 (+LED)", propM2.floatValue);

            SerializedProperty propM3 = so.FindProperty("aquariumDecorMultiplierLevel3");
            propM3.floatValue = EditorGUILayout.FloatField("Lv.3 (+San hô)", propM3.floatValue);

            SerializedProperty propM4 = so.FindProperty("aquariumDecorMultiplierLevel4");
            propM4.floatValue = EditorGUILayout.FloatField("Lv.4 (+Ngọc trai)", propM4.floatValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            so.ApplyModifiedProperties();
        }

        // ==========================================
        // TAB 6: AUDIO & VFX
        // ==========================================
        private void DrawAudioAndVFXTab()
        {
            if (audioManager == null) audioManager = UnityEngine.Object.FindObjectOfType<AudioManager>();
            if (vfxManager == null) vfxManager = UnityEngine.Object.FindObjectOfType<VFXManager>();

            // 1. MASTER VOLUME CONTROLS
            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("🎚️ 1. ĐIỀU CHỈNH ÂM LƯỢNG HỆ THỐNG (AUDIO VOLUME CONTROLS)", sectionStyle);
            EditorGUILayout.HelpBox("Thanh trượt điều chỉnh âm lượng trực tiếp. Tự động đồng bộ với AudioManager và lưu vào PlayerPrefs.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            sfxVolume = EditorGUILayout.Slider("🔊 Âm lượng Hiệu ứng (SFX)", sfxVolume, 0f, 1f);
            bgmVolume = EditorGUILayout.Slider("🎵 Âm lượng Nhạc nền (BGM)", bgmVolume, 0f, 1f);
            ambientVolume = EditorGUILayout.Slider("🌊 Âm lượng Môi trường (Ambience)", ambientVolume, 0f, 1f);
            isMuted = EditorGUILayout.Toggle("🔇 Tắt toàn bộ âm thanh (Mute)", isMuted);

            if (EditorGUI.EndChangeCheck())
            {
                PlayerPrefs.SetFloat("IdleFishing_SFX_Volume", sfxVolume);
                PlayerPrefs.SetFloat("IdleFishing_BGM_Volume", bgmVolume);
                PlayerPrefs.SetFloat("IdleFishing_Ambient_Volume", ambientVolume);
                PlayerPrefs.SetInt("IdleFishing_Muted", isMuted ? 1 : 0);
                PlayerPrefs.Save();

                if (audioManager != null)
                {
                    audioManager.SetSFXVolume(sfxVolume);
                    audioManager.SetBGMVolume(bgmVolume);
                    audioManager.SetAmbientVolume(ambientVolume);
                    audioManager.SetMuted(isMuted);
                }
            }
            EditorGUILayout.EndVertical();

            // 2. SFX SOUNDBOARD
            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🎹 2. BÀN PHÍM PHÁT THỬ HIỆU ỨNG ÂM THANH (SFX SOUNDBOARD)", sectionStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("⏹ Dừng Âm Thanh", EditorStyles.miniButton, GUILayout.Width(110)))
            {
                StopAudioPreview();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Bấm các nút bên dưới để nghe thử lập tức từng âm thanh SFX chuẩn của game.", MessageType.None);

            // Group A: Fishing
            EditorGUILayout.LabelField("🎣 Hoạt động Câu cá:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawSfxButton("Quăng cần (Whoosh)", "Assets/Audio/SFX/sfx_rod_cast_whoosh.wav");
            DrawSfxButton("Quay cước (Reel)", "Assets/Audio/SFX/sfx_reel_crank.wav");
            DrawSfxButton("Cá đớp nước (Plop)", "Assets/Audio/SFX/sfx_water_plop.wav");
            DrawSfxButton("Rơi sàn tàu (Drop)", "Assets/Audio/SFX/sfx_fish_dock_drop.wav");
            EditorGUILayout.EndHorizontal();

            // Group B: Combat
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("⚔️ Thợ săn & Chiến đấu cá:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawSfxButton("Đập trúng cá (Whack)", "Assets/Audio/SFX/sfx_fish_hit.wav");
            DrawSfxButton("Đòn chí mạng (Crit)", "Assets/Audio/SFX/sfx_wrangler_crit_whack.wav");
            DrawSfxButton("Cá choáng (Stun)", "Assets/Audio/SFX/sfx_stun.wav");
            DrawSfxButton("Mổ xẻ cá (Slice)", "Assets/Audio/SFX/sfx_slice.wav");
            EditorGUILayout.EndHorizontal();

            // Group C: Economy & Rewards
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("🪙 Kinh tế & Phần thưởng:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawSfxButton("Thu tiền vàng (Coin)", "Assets/Audio/SFX/sfx_coin_collect.wav");
            DrawSfxButton("Mở rương báu (Chest)", "Assets/Audio/SFX/sfx_level_up.wav");
            DrawSfxButton("Pháo giấy (Confetti)", "Assets/Audio/SFX/sfx_confetti.wav");
            DrawSfxButton("Kèn hợp đồng (Fanfare)", "Assets/Audio/SFX/sfx_contract_fanfare.wav");
            DrawSfxButton("Còi tàu biển (Horn)", "Assets/Audio/SFX/sfx_ship_horn.wav");
            EditorGUILayout.EndHorizontal();

            // Group D: Island Gimmicks
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("🏝️ Cơ chế Độc quyền 5 Hòn Đảo:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawSfxButton("Đảo 2: Tách vỏ sò (Clack)", "Assets/Audio/SFX/sfx_pearl_harvest.wav");
            DrawSfxButton("Đảo 3: Nướng BBQ (Sizzle)", "Assets/Audio/SFX/sfx_bbq_sizzle.wav");
            DrawSfxButton("Đảo 4: Nứt băng (Crack)", "Assets/Audio/SFX/sfx_ice_crack.wav");
            DrawSfxButton("Đảo 5: Chuông lạc đà (Bells)", "Assets/Audio/SFX/sfx_caravan_bells.wav");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // 3. ISLAND BGM & AMBIENCE MATRIX
            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("🎵 3. NHẠC NỀN (BGM) & KHÔNG GIAN (AMBIENCE) 5 VÙNG BIỂN", sectionStyle);
            EditorGUILayout.HelpBox("Mỗi thế giới sở hữu bản sắc âm nhạc độc quyền. Bạn có thể nghe thử hoặc kéo thả file âm thanh khác vào ô bên dưới.", MessageType.Info);

            for (int i = 0; i < worldAssets.Count; i++)
            {
                var w = worldAssets[i];
                if (w == null) continue;

                EditorGUILayout.BeginVertical("HelpBox");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🏝️ Level {w.worldOrder}: {w.worldName}", EditorStyles.boldLabel, GUILayout.Width(200));

                if (GUILayout.Button("▶ Nghe BGM", EditorStyles.miniButton, GUILayout.Width(90)))
                {
                    if (w.customBgmClip != null) PlayAudioPreview(w.customBgmClip);
                }

                if (GUILayout.Button("🌊 Nghe Sóng/Gió", EditorStyles.miniButton, GUILayout.Width(110)))
                {
                    if (w.customAmbienceClip != null) PlayAudioPreview(w.customAmbienceClip);
                }

                if (GUILayout.Button("⏹ Dừng", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    StopAudioPreview();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                var newBgm = (AudioClip)EditorGUILayout.ObjectField("Nhạc nền (BGM)", w.customBgmClip, typeof(AudioClip), false);
                var newAmb = (AudioClip)EditorGUILayout.ObjectField("Âm thanh môi trường", w.customAmbienceClip, typeof(AudioClip), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(w, "Change World Audio Clips");
                    w.customBgmClip = newBgm;
                    w.customAmbienceClip = newAmb;
                    EditorUtility.SetDirty(w);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.EndVertical();

            // 4. VFX MANAGEMENT & LIVE TEST SPAWNER
            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("✨ 4. QUẢN LÝ & TEST HIỆU ỨNG HẠT (VFX PARTICLE SPAWNER)", sectionStyle);
            EditorGUILayout.HelpBox("Quản lý danh sách prefab VFX và bấm nút test spawn trực tiếp trong Scene / Game View khi đang Play Mode.", MessageType.None);

            if (vfxManager != null)
            {
                var vfxSo = new SerializedObject(vfxManager);
                vfxSo.Update();

                EditorGUILayout.LabelField("Danh sách Prefab Hiệu Ứng (VFX Prefabs):", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(vfxSo.FindProperty("waterSplashPrefab"), new GUIContent("Tóe nước (Splash)"));
                EditorGUILayout.PropertyField(vfxSo.FindProperty("waterRipplePrefab"), new GUIContent("Gợn sóng (Ripple)"));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(vfxSo.FindProperty("hitPunchPrefab"), new GUIContent("Đấm trúng (Hit)"));
                EditorGUILayout.PropertyField(vfxSo.FindProperty("stunStarsPrefab"), new GUIContent("Sao choáng (Stun)"));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(vfxSo.FindProperty("confettiPrefab"), new GUIContent("Pháo hoa (Confetti)"));
                EditorGUILayout.PropertyField(vfxSo.FindProperty("levelUpPrefab"), new GUIContent("Cột sáng (Level Up)"));
                EditorGUILayout.EndHorizontal();

                vfxSo.ApplyModifiedProperties();

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("🎮 Bắn Thử Hiệu Ứng Trực Tiếp (Live Test Spawner):", EditorStyles.boldLabel);

                if (Application.isPlaying)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("🌊 Tóe Nước (Splash)", GUILayout.Height(30)))
                    {
                        VFXManager.Instance.SpawnSplash(new Vector3(0f, 0f, 10.5f), 1.2f);
                    }
                    if (GUILayout.Button("💫 Sao Choáng (Stun)", GUILayout.Height(30)))
                    {
                        VFXManager.Instance.SpawnHitImpact(new Vector3(0f, 1.2f, 2f));
                    }
                    if (GUILayout.Button("✨ Nổ Vàng (Gold Burst)", GUILayout.Height(30)))
                    {
                        VFXManager.Instance.SpawnGoldBurst(new Vector3(0f, 1.2f, 2f), 30);
                    }
                    if (GUILayout.Button("🎉 Pháo Giấy (Confetti)", GUILayout.Height(30)))
                    {
                        VFXManager.Instance.SpawnConfetti(new Vector3(0f, 1.5f, 2f), 1.2f);
                    }
                    if (GUILayout.Button("⭐ Lên Cấp (Level Up)", GUILayout.Height(30)))
                    {
                        VFXManager.Instance.SpawnLevelUp(new Vector3(0f, 0.1f, 1.5f), 1.0f);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    string frenzyBtnText = isFrenzyAuraActive ? "🔥 Tắt Hào Quang Frenzy" : "🔥 Bật Hào Quang Cuồng Ngư (Frenzy)";
                    GUI.backgroundColor = isFrenzyAuraActive ? new Color(1f, 0.4f, 0.3f) : new Color(1f, 0.85f, 0.2f);
                    if (GUILayout.Button(frenzyBtnText, GUILayout.Height(28)))
                    {
                        isFrenzyAuraActive = !isFrenzyAuraActive;
                        VFXManager.Instance.SetFrenzyAura(isFrenzyAuraActive);
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("💡 Hãy bấm [Play] game để kích hoạt các nút bắn thử hạt VFX 3D trực tiếp trong không gian màn chơi!", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Không tìm thấy VFXManager trong Scene!", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSfxButton(string label, string assetPath)
        {
            if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Height(24)))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null) PlayAudioPreview(clip);
                else Debug.LogWarning($"[Dashboard] Không tìm thấy file âm thanh tại: {assetPath}");
            }
        }

        private void PlayAudioPreview(AudioClip clip)
        {
            if (clip == null) return;
            try
            {
                var asm = typeof(AudioImporter).Assembly;
                var audioUtil = asm.GetType("UnityEditor.AudioUtil");
                if (audioUtil != null)
                {
                    var playMethod = audioUtil.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                        ?? audioUtil.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
                    if (playMethod != null) playMethod.Invoke(null, new object[] { clip, 0, false });
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Dashboard] Không thể phát audio preview: {ex.Message}");
            }
        }

        private void StopAudioPreview()
        {
            try
            {
                var asm = typeof(AudioImporter).Assembly;
                var audioUtil = asm.GetType("UnityEditor.AudioUtil");
                if (audioUtil != null)
                {
                    var stopMethod = audioUtil.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)
                        ?? audioUtil.GetMethod("StopAllClips", BindingFlags.Static | BindingFlags.Public);
                    if (stopMethod != null) stopMethod.Invoke(null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Dashboard] Không thể dừng audio preview: {ex.Message}");
            }
        }

        // ==========================================
        // TAB 7: PRESETS & TOOLS
        // ==========================================
        private void DrawPresetsAndToolsTab()
        {
            EditorGUILayout.BeginVertical(cardBoxStyle);
            EditorGUILayout.LabelField("🛠️ CÁC BỘ CẤU HÌNH CÂN BẰNG SẴN (BALANCE PRESETS)", sectionStyle);
            EditorGUILayout.HelpBox("Chọn một preset để áp dụng nhanh bộ thông số phù hợp với mục đích kiểm thử hoặc phát hành.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();

            // Preset 1: Fast Testing
            GUI.backgroundColor = new Color(0.95f, 0.65f, 0.3f);
            if (GUILayout.Button("⚡ Preset: Test Nhanh (Fast Dev)\n- Câu cá cực nhanh (1.5s)\n- Mở đảo siêu rẻ\n- Nâng cấp rẻ x1.15", GUILayout.Height(75)))
            {
                ApplyPresetFastTesting();
            }

            // Preset 2: Standard Casual
            GUI.backgroundColor = new Color(0.4f, 0.85f, 0.45f);
            if (GUILayout.Button("⭐ Preset: Chuẩn Casual (Khuyên dùng)\n- Nhịp độ thư thái mượt mà\n- Độ khó tăng tiến tự nhiên\n- Cân đối thu chi 5 thế giới", GUILayout.Height(75)))
            {
                ApplyPresetStandardCasual();
            }

            // Preset 3: Slow Idle
            GUI.backgroundColor = new Color(0.7f, 0.5f, 0.95f);
            if (GUILayout.Button("⏳ Preset: Hardcore Idle\n- Thời gian câu lâu hơn (6.5s)\n- Chi phí nâng cấp nhân cao\n- Thử thách dài hơi", GUILayout.Height(75)))
            {
                ApplyPresetSlowIdle();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);

            EditorGUILayout.LabelField("🧹 DỌN DẸP TIẾN TRÌNH & DỮ LIỆU LƯU (SAVE DATA TOOLS)", sectionStyle);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🗑️ Xóa Save Data (Reset Save)", GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("Xóa Save Data", "Bạn có chắc chắn muốn xóa dữ liệu lưu hiện tại để test game lại từ đầu?", "Xóa", "Hủy"))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    ShowNotification(new GUIContent("Đã xóa sạch PlayerPrefs và Save Data!"));
                }
            }

            if (GUILayout.Button("💰 Thêm +10,000 Vàng Test", GUILayout.Height(32)))
            {
                if (Application.isPlaying && Economy.EconomyManager.Instance != null)
                {
                    Economy.EconomyManager.Instance.AddCoins(10000);
                    ShowNotification(new GUIContent("Đã cộng +10,000 Vàng trong Play Mode!"));
                }
                else
                {
                    ShowNotification(new GUIContent("Vui lòng bật Play Mode để cộng vàng trực tiếp!"));
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawBottomBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("💡 Mẹo: Nhấn nút bên phải hoặc phím tắt Ctrl+S để lưu mọi điều chỉnh vào file Asset.", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("💾 LƯU TẤT CẢ CHỈ SỐ (SAVE ASSETS)", GUILayout.Height(30), GUILayout.Width(240)))
            {
                SaveAllAssets();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void SaveAllAssets()
        {
            if (balanceConfig != null) EditorUtility.SetDirty(balanceConfig);
            if (upgradeConfig != null) EditorUtility.SetDirty(upgradeConfig);
            foreach (var w in worldAssets) if (w != null) EditorUtility.SetDirty(w);
            foreach (var item in fishItemAssets) if (item != null) EditorUtility.SetDirty(item);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("✅ Đã lưu thành công tất cả chỉ số vào project!"));
            Debug.Log("[GameBalanceDashboard] Successfully saved all balance assets to disk.");
        }

        private void ApplyPresetFastTesting()
        {
            if (balanceConfig != null)
            {
                balanceConfig.fishermanBaseCastTime = 1.8f;
                balanceConfig.bossSpawnChance = 0.25f;
                balanceConfig.treasureChestSpawnChance = 0.20f;
                balanceConfig.frenzyDuration = 30f;
                balanceConfig.frenzySpeedMultiplier = 3.5f;
                EditorUtility.SetDirty(balanceConfig);
            }

            if (upgradeConfig != null)
            {
                var up0 = upgradeConfig.GetEntry(UpgradeType.FishermanTeam);
                if (up0 != null) { up0.baseCost = 15; up0.costMultiplier = 1.18f; }
                var up1 = upgradeConfig.GetEntry(UpgradeType.WranglerTeam);
                if (up1 != null) { up1.baseCost = 20; up1.costMultiplier = 1.20f; }
                var up2 = upgradeConfig.GetEntry(UpgradeType.FishValue);
                if (up2 != null) { up2.baseCost = 25; up2.costMultiplier = 1.22f; }
                EditorUtility.SetDirty(upgradeConfig);
            }

            if (worldAssets.Count >= 5)
            {
                worldAssets[1].unlockRequiredCoins = 250;
                worldAssets[2].unlockRequiredCoins = 1000;
                worldAssets[3].unlockRequiredCoins = 5000;
                worldAssets[4].unlockRequiredCoins = 20000;
                foreach (var w in worldAssets) EditorUtility.SetDirty(w);
            }

            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("⚡ Đã kích hoạt Preset: Test Nhanh!"));
        }

        private void ApplyPresetStandardCasual()
        {
            if (balanceConfig != null)
            {
                balanceConfig.fishermanBaseCastTime = 5.5f;
                balanceConfig.bossSpawnChance = 0.10f;
                balanceConfig.bossBaseBonusGold = 150;
                balanceConfig.treasureChestSpawnChance = 0.07f;
                balanceConfig.treasureChestMinGold = 180;
                balanceConfig.treasureChestMaxGold = 350;
                balanceConfig.frenzyDuration = 20f;
                balanceConfig.frenzySpeedMultiplier = 2.0f;
                balanceConfig.offlineBaseCoinsPerSecond = 0.30f;
                EditorUtility.SetDirty(balanceConfig);
            }

            if (upgradeConfig != null)
            {
                var up0 = upgradeConfig.GetEntry(UpgradeType.FishermanTeam);
                if (up0 != null) { up0.baseCost = 75; up0.costMultiplier = 1.40f; up0.valueIncreasePerLevel = 0.12f; }
                var up1 = upgradeConfig.GetEntry(UpgradeType.WranglerTeam);
                if (up1 != null) { up1.baseCost = 85; up1.costMultiplier = 1.42f; up1.valueIncreasePerLevel = 0.15f; }
                var up2 = upgradeConfig.GetEntry(UpgradeType.FishValue);
                if (up2 != null) { up2.baseCost = 100; up2.costMultiplier = 1.44f; up2.valueIncreasePerLevel = 0.18f; }
                EditorUtility.SetDirty(upgradeConfig);
            }

            if (worldAssets.Count >= 5)
            {
                worldAssets[0].startingCoins = 30;
                worldAssets[0].unlockRequiredCoins = 0;
                worldAssets[0].unlockRequiredUpgrades = 0;

                worldAssets[1].startingCoins = 100;
                worldAssets[1].unlockRequiredCoins = 14000;
                worldAssets[1].unlockRequiredUpgrades = 15;

                worldAssets[2].startingCoins = 400;
                worldAssets[2].unlockRequiredCoins = 330000;
                worldAssets[2].unlockRequiredUpgrades = 24;

                worldAssets[3].startingCoins = 1500;
                worldAssets[3].unlockRequiredCoins = 2800000;
                worldAssets[3].unlockRequiredUpgrades = 32;

                worldAssets[4].startingCoins = 5000;
                worldAssets[4].unlockRequiredCoins = 18500000;
                worldAssets[4].unlockRequiredUpgrades = 38;

                foreach (var w in worldAssets) EditorUtility.SetDirty(w);
            }

            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("⭐ Đã kích hoạt Preset: Chuẩn 15p/30p Progression!"));
        }

        private void ApplyPresetSlowIdle()
        {
            if (balanceConfig != null)
            {
                balanceConfig.fishermanBaseCastTime = 6.5f;
                balanceConfig.bossSpawnChance = 0.08f;
                balanceConfig.treasureChestSpawnChance = 0.05f;
                balanceConfig.frenzyDuration = 15f;
                balanceConfig.frenzySpeedMultiplier = 2.0f;
                EditorUtility.SetDirty(balanceConfig);
            }

            if (upgradeConfig != null)
            {
                var up0 = upgradeConfig.GetEntry(UpgradeType.FishermanTeam);
                if (up0 != null) { up0.baseCost = 50; up0.costMultiplier = 1.45f; }
                var up1 = upgradeConfig.GetEntry(UpgradeType.WranglerTeam);
                if (up1 != null) { up1.baseCost = 65; up1.costMultiplier = 1.48f; }
                var up2 = upgradeConfig.GetEntry(UpgradeType.FishValue);
                if (up2 != null) { up2.baseCost = 85; up2.costMultiplier = 1.50f; }
                EditorUtility.SetDirty(upgradeConfig);
            }

            if (worldAssets.Count >= 5)
            {
                worldAssets[1].unlockRequiredCoins = 3000;
                worldAssets[2].unlockRequiredCoins = 35000;
                worldAssets[3].unlockRequiredCoins = 280000;
                worldAssets[4].unlockRequiredCoins = 2500000;
                foreach (var w in worldAssets) EditorUtility.SetDirty(w);
            }

            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("⏳ Đã kích hoạt Preset: Hardcore Idle!"));
        }
    }
}
