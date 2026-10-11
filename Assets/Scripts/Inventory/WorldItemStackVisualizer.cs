using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Data;

namespace IdleFishing.Inventory
{
    [RequireComponent(typeof(Inventory))]
    public class WorldItemStackVisualizer : MonoBehaviour
    {
        [Header("Target Inventory")]
        [SerializeField] private Inventory targetInventory;

        [Header("Stack Grid Layout")]
        [SerializeField] private Vector3 stackOriginOffset = Vector3.zero;
        [SerializeField] private int columnsX = 2;
        [SerializeField] private int rowsZ = 2;
        [SerializeField] private float spacingX = 0.4f;
        [SerializeField] private float spacingZ = 0.4f;
        [SerializeField] private float layerHeightY = 0.25f;
        [SerializeField] private Vector3 itemVisualScale = Vector3.one * 0.8f;
        [SerializeField] private Vector3 itemRotationEuler = Vector3.zero;

        [Header("Animation")]
        [SerializeField] private bool animatePopOnAdd = true;

        public bool AnimatePopOnAdd => animatePopOnAdd;

        public void ConfigureLayout(Vector3 originOffset, int cols, int rows, float spaceX, float spaceZ, Vector3 visualScale)
        {
            stackOriginOffset = originOffset;
            columnsX = cols;
            rowsZ = rows;
            spacingX = spaceX;
            spacingZ = spaceZ;
            itemVisualScale = visualScale;
            if (targetInventory != null)
            {
                RefreshVisuals(targetInventory);
            }
        }

        private readonly List<GameObject> spawnedVisuals = new List<GameObject>();

        private void Awake()
        {
            if (targetInventory == null)
            {
                targetInventory = GetComponent<Inventory>();
            }
        }

        private void OnEnable()
        {
            if (targetInventory != null)
            {
                targetInventory.OnInventoryChanged += RefreshVisuals;
            }
            RefreshVisuals(targetInventory);
        }

        private void OnDisable()
        {
            if (targetInventory != null)
            {
                targetInventory.OnInventoryChanged -= RefreshVisuals;
            }
            ClearVisuals();
        }

        public void RefreshVisuals(Inventory inventory)
        {
            if (inventory == null) return;

            ClearVisuals();

            int visualIndex = 0;
            int itemsPerLayer = Mathf.Max(1, columnsX * rowsZ);

            foreach (var stack in inventory.Stacks)
            {
                if (stack == null || stack.Item == null || stack.Item.Prefab == null) continue;

                for (int i = 0; i < stack.Quantity; i++)
                {
                    int layer = visualIndex / itemsPerLayer;
                    int slotInLayer = visualIndex % itemsPerLayer;
                    int col = slotInLayer % columnsX;
                    int row = slotInLayer / columnsX;

                    Vector3 localPos = stackOriginOffset + new Vector3(
                        (col - (columnsX - 1) * 0.5f) * spacingX,
                        layer * layerHeightY,
                        (row - (rowsZ - 1) * 0.5f) * spacingZ
                    );

                    GameObject visualObj = Instantiate(stack.Item.Prefab, transform);
                    visualObj.transform.localPosition = localPos;

                    bool isFlatBody = !string.IsNullOrEmpty(stack.Item.ItemId) &&
                        (stack.Item.ItemId.Contains("crab") || stack.Item.ItemId.Contains("lobster") || stack.Item.ItemId.Contains("ray"));
                    Vector3 finalEuler = isFlatBody
                        ? itemRotationEuler
                        : (itemRotationEuler + new Vector3(0f, 90f, 90f));

                    visualObj.transform.localRotation = Quaternion.Euler(finalEuler);
                    visualObj.transform.localScale = itemVisualScale;

                    // Disable skeletal animators on stacked storage/market items
                    Animator[] animators = visualObj.GetComponentsInChildren<Animator>();
                    for (int a = 0; a < animators.Length; a++)
                    {
                        if (animators[a] != null) animators[a].enabled = false;
                    }

                    // Disable colliders on visual items to prevent unwanted physics jitter
                    Collider[] colliders = visualObj.GetComponentsInChildren<Collider>();
                    for (int c = 0; c < colliders.Length; c++)
                    {
                        colliders[c].enabled = false;
                    }

                    spawnedVisuals.Add(visualObj);
                    visualIndex++;
                }
            }
        }

        private void ClearVisuals()
        {
            for (int i = 0; i < spawnedVisuals.Count; i++)
            {
                if (spawnedVisuals[i] != null)
                {
                    Destroy(spawnedVisuals[i]);
                }
            }
            spawnedVisuals.Clear();
        }
    }
}
