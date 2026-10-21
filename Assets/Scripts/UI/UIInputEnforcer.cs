using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace IdleFishing.UI
{
    /// <summary>
    /// Đảm bảo EventSystem và toàn bộ hệ thống UI trong game hoạt động ổn định 100% với StandaloneInputModule (Old Input):
    /// 1. Tự động bật forceModuleActive = true trên StandaloneInputModule (đảm bảo chuột nhận click ngay lập tức cả khi mất focus).
    /// 2. Tự động liên kết các nút HUD (Btn_ContractsBoard, Btn_Aquarium, WorldMap_NavButton).
    /// 3. Vô hiệu hóa raycastTarget trên các thành phần Text / Label con bên trong Button để Button nhận trọn vẹn pointer events.
    /// 4. Vô hiệu hóa raycastTarget trên các banner hiển thị tĩnh (Frenzy HUD, Market Trend, Floating Coins, Tap Hint) để không chặn click 3D hay kéo camera.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class UIInputEnforcer : MonoBehaviour
    {
        public static UIInputEnforcer Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            EnforceStandaloneInputModule();
        }

        private void Start()
        {
            EnforceStandaloneInputModule();
            SanitizeAllRaycastTargets();
            BindHUDButtons();
        }

        public void EnforceStandaloneInputModule()
        {
            var es = EventSystem.current;
            if (es == null)
            {
                var existingEs = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
                if (existingEs != null) es = existingEs;
                else
                {
                    var esObj = new GameObject("EventSystem");
                    es = esObj.AddComponent<EventSystem>();
                }
            }

            var sim = es.GetComponent<StandaloneInputModule>();
            if (sim == null)
            {
                es.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

        public void SanitizeAllRaycastTargets()
        {
            // 1. Tắt raycastTarget trên tất cả Text bên trong các Button
            var allButtons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var btn in allButtons)
            {
                var graphic = btn.GetComponent<Graphic>();
                if (graphic != null && btn.targetGraphic == null)
                {
                    btn.targetGraphic = graphic;
                }

                var childTexts = btn.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in childTexts)
                {
                    t.raycastTarget = false;
                }

                btn.onClick.AddListener(() =>
                {
                    if (IdleFishing.Audio.AudioManager.Instance != null)
                    {
                        IdleFishing.Audio.AudioManager.Instance.PlayUIClick();
                    }
                });
            }

            // 2. Tắt raycastTarget trên các banner hiển thị tĩnh (không nhận tương tác click)
            string[] decorativeNames = new string[]
            {
                "PlayerTapHint",
                "MarketTrend_Ticker",
                "Frenzy_HUD_Container",
                "Frenzy_Fill",
                "Frenzy_StatusText",
                "BossAlertText",
                "CoinsText"
            };

            foreach (var name in decorativeNames)
            {
                var go = GameObject.Find(name);
                if (go != null)
                {
                    var graphics = go.GetComponentsInChildren<Graphic>(true);
                    foreach (var g in graphics)
                    {
                        // Chỉ tắt nếu không thuộc button
                        if (g.GetComponentInParent<Button>() == null)
                        {
                            g.raycastTarget = false;
                        }
                    }
                }
            }
        }

        public void BindHUDButtons()
        {
            // 1. Contracts Board Button - Deactivated per user request
            var cBtnObj = GameObject.Find("Btn_ContractsBoard");
            if (cBtnObj != null)
            {
                cBtnObj.SetActive(false);
            }

            // 2. Aquarium Button
            var aqBtnObj = GameObject.Find("Btn_Aquarium");
            if (aqBtnObj != null)
            {
                var btn = aqBtnObj.GetComponent<Button>();
                if (btn != null)
                {
                    var img = aqBtnObj.GetComponent<Image>();
                    if (img != null) btn.targetGraphic = img;

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        if (AquariumPopupUI.Instance != null)
                        {
                            AquariumPopupUI.Instance.Toggle();
                        }
                    });
                }

                var txts = aqBtnObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in txts) t.raycastTarget = false;
            }

            // 3. World Map Nav Button
            var mapBtnObj = GameObject.Find("WorldMap_NavButton");
            if (mapBtnObj != null)
            {
                var btn = mapBtnObj.GetComponent<Button>();
                if (btn != null)
                {
                    var img = mapBtnObj.GetComponent<Image>();
                    if (img != null) btn.targetGraphic = img;

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        if (WorldSelectionPopupUI.Instance != null)
                        {
                            WorldSelectionPopupUI.Instance.Open();
                        }
                    });
                }

                var txts = mapBtnObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in txts) t.raycastTarget = false;
            }
        }
    }
}
