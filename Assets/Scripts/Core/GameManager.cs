using UnityEngine;

namespace IdleFishing.Core
{
    public enum GameZone
    {
        FishingOffshore,
        DockAndStorage,
        ProcessingKitchen,
        MarketStore
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Active Game Zone")]
        [SerializeField] private GameZone currentZone = GameZone.FishingOffshore;

        [Header("Game Balance Configuration")]
        [SerializeField] private IdleFishing.Data.GameBalanceConfig balanceConfig;

        public GameZone CurrentZone => currentZone;
        public IdleFishing.Data.GameBalanceConfig BalanceConfig => balanceConfig;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Time.timeScale = 1.0f;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;

            EnsureStandaloneInputModule();
        }

        private void EnsureStandaloneInputModule()
        {
            var es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es != null)
            {
                var inputModules = es.GetComponents<UnityEngine.EventSystems.BaseInputModule>();
                foreach (var module in inputModules)
                {
                    if (!(module is UnityEngine.EventSystems.StandaloneInputModule))
                    {
                        Destroy(module);
                    }
                }
                if (es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
                {
                    es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }
        }

        public void SwitchZone(GameZone newZone)
        {
            currentZone = newZone;
            Debug.Log($"[GameManager] Switched zone to: {newZone}");
        }
    }
}
