using UnityEngine;
using IdleFishing.UI;

namespace IdleFishing.World
{
    public class WorldGateTrigger : MonoBehaviour
    {
        private void OnMouseDown()
        {
            if (WorldSelectionPopupUI.Instance != null)
            {
                WorldSelectionPopupUI.Instance.Open();
            }
        }
    }
}
