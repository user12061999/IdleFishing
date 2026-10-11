using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Data;
using IdleFishing.Fishing;

namespace IdleFishing.UI
{
    public class FishingMinigameUI : MonoBehaviour
    {
        [Header("Controller Reference")]
        [SerializeField] private FishingController controller;

        [Header("Minigame Panels")]
        [SerializeField] private GameObject minigameContainer;
        [SerializeField] private GameObject biteAlertBadge;
        [SerializeField] private GameObject catchSuccessBadge;
        [SerializeField] private TextMeshProUGUI catchFishNameText;

        [Header("Minigame Sliders")]
        [SerializeField] private Slider tensionSlider;
        [SerializeField] private Slider reelProgressSlider;
        [SerializeField] private RectTransform greenZoneRect;
        [SerializeField] private RectTransform tensionBarTrack;

        [Header("Reel Button")]
        [SerializeField] private Button reelTapButton;

        private void Awake()
        {
            if (reelTapButton != null)
            {
                reelTapButton.onClick.AddListener(OnReelClicked);
            }
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.OnStateChanged += OnStateChanged;
                controller.OnMinigameTick += OnMinigameTick;
                controller.OnReelProgressChanged += OnReelProgressChanged;
                controller.OnCatchSuccess += OnCatchSuccess;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.OnStateChanged -= OnStateChanged;
                controller.OnMinigameTick -= OnMinigameTick;
                controller.OnReelProgressChanged -= OnReelProgressChanged;
                controller.OnCatchSuccess -= OnCatchSuccess;
            }
        }

        private void OnReelClicked()
        {
            if (controller != null)
            {
                controller.TapReel();
            }
        }

        private void OnStateChanged(FishingState state)
        {
            if (minigameContainer != null)
            {
                minigameContainer.SetActive(state == FishingState.HookedMiniGame);
            }

            if (biteAlertBadge != null)
            {
                biteAlertBadge.SetActive(state == FishingState.WaitingForBite);
            }

            if (catchSuccessBadge != null && state != FishingState.ReelingSuccess)
            {
                catchSuccessBadge.SetActive(false);
            }
        }

        private void OnMinigameTick(float tensionValue, float greenMin, float greenMax)
        {
            if (tensionSlider != null)
            {
                tensionSlider.value = tensionValue;
            }

            // Update sweet spot rect visual position if track is configured
            if (greenZoneRect != null && tensionBarTrack != null)
            {
                float trackWidth = tensionBarTrack.rect.width;
                float startX = greenMin * trackWidth;
                float zoneWidth = (greenMax - greenMin) * trackWidth;

                greenZoneRect.sizeDelta = new Vector2(zoneWidth, greenZoneRect.sizeDelta.y);
                greenZoneRect.anchoredPosition = new Vector2(startX + (zoneWidth * 0.5f) - (trackWidth * 0.5f), greenZoneRect.anchoredPosition.y);
            }
        }

        private void OnReelProgressChanged(float progress)
        {
            if (reelProgressSlider != null)
            {
                reelProgressSlider.value = progress;
            }
        }

        private void OnCatchSuccess(FishChainData fish)
        {
            if (catchSuccessBadge != null)
            {
                catchSuccessBadge.SetActive(true);
            }

            if (catchFishNameText != null && fish != null)
            {
                catchFishNameText.text = $"CAUGHT: {fish.SpeciesName}!";
            }
        }
    }
}
