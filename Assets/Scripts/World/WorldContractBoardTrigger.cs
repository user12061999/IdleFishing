using System.Collections;
using UnityEngine;
using TMPro;
using IdleFishing.UI;

namespace IdleFishing.World
{
    [RequireComponent(typeof(Collider))]
    public class WorldContractBoardTrigger : MonoBehaviour
    {
        [Header("Billboard")]
        [SerializeField] private Transform billboardTransform;
        [SerializeField] private TextMeshProUGUI boardTitleText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Visual Juice")]
        [SerializeField] private Transform visualTransform;

        private Camera mainCamera;
        private Vector3 originalScale = Vector3.one;
        private bool isPunching = false;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (visualTransform != null)
            {
                originalScale = visualTransform.localScale;
            }
        }

        private void LateUpdate()
        {
            if (billboardTransform == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                billboardTransform.rotation = mainCamera.transform.rotation;
            }
        }

        public void Setup(Transform billboard, TextMeshProUGUI title, TextMeshProUGUI status, Transform visual = null)
        {
            billboardTransform = billboard;
            boardTitleText = title;
            statusText = status;
            visualTransform = visual;
            if (visualTransform != null)
            {
                originalScale = visualTransform.localScale;
            }
        }

        private void OnMouseDown()
        {
            OpenContractBoard();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OpenContractBoard();
            }
        }

        public void OpenContractBoard()
        {
            StartCoroutine(PunchRoutine());

            if (ContractBoardPopupUI.Instance != null)
            {
                ContractBoardPopupUI.Instance.Open();
            }
        }

        private IEnumerator PunchRoutine()
        {
            if (isPunching || visualTransform == null) yield break;
            isPunching = true;

            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float s = 1.0f + Mathf.Sin(t * Mathf.PI) * 0.15f;
                visualTransform.localScale = originalScale * s;
                yield return null;
            }

            visualTransform.localScale = originalScale;
            isPunching = false;
        }
    }
}
