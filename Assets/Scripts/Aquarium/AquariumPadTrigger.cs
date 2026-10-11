using UnityEngine;
using IdleFishing.UI;

namespace IdleFishing.Aquarium
{
    public class AquariumPadTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
            {
                if (AquariumPopupUI.Instance != null)
                {
                    AquariumPopupUI.Instance.Open();
                }
            }
        }

        private void OnMouseDown()
        {
            if (AquariumPopupUI.Instance != null)
            {
                AquariumPopupUI.Instance.Open();
            }
        }
    }
}
