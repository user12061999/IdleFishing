using UnityEngine;
using UnityEngine.UI;
using IdleFishing.Fishing;

namespace IdleFishing.UI
{
    public class FishHealthBarUI : MonoBehaviour
    {
        [Header("Target Fish")]
        [SerializeField] private LiveFish targetFish;

        [Header("UI Slider")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Vector3 offset = new Vector3(0, 1.2f, 0);

        private Camera mainCamera;

        private void Awake()
        {
            if (targetFish == null)
            {
                targetFish = GetComponentInParent<LiveFish>();
            }
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (targetFish != null)
            {
                targetFish.OnHealthChanged += UpdateHealth;
                targetFish.OnDefeated += OnFishDefeated;
            }
        }

        private void OnDisable()
        {
            if (targetFish != null)
            {
                targetFish.OnHealthChanged -= UpdateHealth;
                targetFish.OnDefeated -= OnFishDefeated;
            }
        }

        private void LateUpdate()
        {
            if (mainCamera != null && targetFish != null)
            {
                transform.position = targetFish.transform.position + offset;
                transform.rotation = mainCamera.transform.rotation;
            }
        }

        private void UpdateHealth(int current, int max)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = max;
                healthSlider.value = current;
            }
        }

        private void OnFishDefeated(LiveFish fish)
        {
            gameObject.SetActive(false);
        }
    }
}
