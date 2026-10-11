using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;

namespace IdleFishing.World
{
    /// <summary>
    /// Đồng bộ theme vật liệu và Decor đặc trưng của 5 Vùng Đất / 5 Level khác nhau
    /// </summary>
    public class ToonEnvironmentThemeController : MonoBehaviour
    {
        [Header("5 Season Materials")]
        [SerializeField] private Material springMaterial;
        [SerializeField] private Material summerMaterial;
        [SerializeField] private Material fallMaterial;
        [SerializeField] private Material winterMaterial;
        [SerializeField] private Material desertMaterial;

        [Header("5 Distinct Land Decor Groups")]
        [SerializeField] private GameObject decorLevel1_Temperate;
        [SerializeField] private GameObject decorLevel2_Tropical;
        [SerializeField] private GameObject decorLevel3_Autumn;
        [SerializeField] private GameObject decorLevel4_Arctic;
        [SerializeField] private GameObject decorLevel5_Desert;

        [Header("Managed Renderers")]
        [SerializeField] private List<Renderer> managedRenderers = new List<Renderer>();

        private void OnEnable()
        {
            GameEvents.WorldChanged += HandleWorldChanged;
        }

        private void OnDisable()
        {
            GameEvents.WorldChanged -= HandleWorldChanged;
        }

        private void Awake()
        {
            EnsurePropsGrounded();
            ResolveDecorGroups();
        }

        private void Start()
        {
            EnsurePropsGrounded();
            ResolveDecorGroups();
            if (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null)
            {
                ApplyWorldTheme(WorldManager.Instance.CurrentWorld);
            }
        }

        public void RegisterDecorGroups(
            GameObject d1, GameObject d2, GameObject d3, GameObject d4, GameObject d5)
        {
            decorLevel1_Temperate = d1;
            decorLevel2_Tropical = d2;
            decorLevel3_Autumn = d3;
            decorLevel4_Arctic = d4;
            decorLevel5_Desert = d5;
        }

        private void ResolveDecorGroups()
        {
            if (decorLevel1_Temperate == null)
            {
                var t = transform.Find("--- DECOR LEVEL 1 (TEMPERATE) ---");
                if (t != null) decorLevel1_Temperate = t.gameObject;
            }
            if (decorLevel2_Tropical == null)
            {
                var t = transform.Find("--- DECOR LEVEL 2 (TROPICAL CORAL) ---");
                if (t != null) decorLevel2_Tropical = t.gameObject;
            }
            if (decorLevel3_Autumn == null)
            {
                var t = transform.Find("--- DECOR LEVEL 3 (GOLDEN AUTUMN) ---");
                if (t != null) decorLevel3_Autumn = t.gameObject;
            }
            if (decorLevel4_Arctic == null)
            {
                var t = transform.Find("--- DECOR LEVEL 4 (ARCTIC FROST) ---");
                if (t != null) decorLevel4_Arctic = t.gameObject;
            }
            if (decorLevel5_Desert == null)
            {
                var t = transform.Find("--- DECOR LEVEL 5 (DESERT OASIS) ---");
                if (t != null) decorLevel5_Desert = t.gameObject;
            }
        }

        [ContextMenu("Ensure Props Grounded")]
        public void EnsurePropsGrounded()
        {
            var treesGroup = transform.Find("--- FORESTS & COASTAL TREES ---");
            if (treesGroup != null)
            {
                for (int i = 0; i < treesGroup.childCount; i++)
                {
                    var child = treesGroup.GetChild(i);
                    if (child.position.y > -0.5f)
                    {
                        float scale = child.localScale.y;
                        Vector3 pos = child.position;
                        pos.y = 0.0f - (1.0f * scale) - 0.05f;
                        child.position = pos;
                    }
                }
            }

            var cabinsGroup = transform.Find("--- CABINS & OUTPOSTS ---");
            if (cabinsGroup != null)
            {
                for (int i = 0; i < cabinsGroup.childCount; i++)
                {
                    var child = cabinsGroup.GetChild(i);
                    if (child.position.y > -0.5f)
                    {
                        float scale = child.localScale.y;
                        Vector3 pos = child.position;
                        pos.y = 0.0f - (1.0f * scale) - 0.02f;
                        child.position = pos;
                    }
                }
            }
        }

        private void HandleWorldChanged(WorldData newWorld)
        {
            ApplyWorldTheme(newWorld);
        }

        public void ApplyWorldTheme(WorldData world)
        {
            if (world == null) return;

            ResolveDecorGroups();

            int order = world.worldOrder;
            Material targetMat = springMaterial;

            switch (order)
            {
                case 1:
                    targetMat = springMaterial != null ? springMaterial : targetMat;
                    SetDecorActive(decorLevel1_Temperate, true);
                    SetDecorActive(decorLevel2_Tropical, false);
                    SetDecorActive(decorLevel3_Autumn, false);
                    SetDecorActive(decorLevel4_Arctic, false);
                    SetDecorActive(decorLevel5_Desert, false);
                    break;

                case 2:
                    targetMat = summerMaterial != null ? summerMaterial : springMaterial;
                    SetDecorActive(decorLevel1_Temperate, false);
                    SetDecorActive(decorLevel2_Tropical, true);
                    SetDecorActive(decorLevel3_Autumn, false);
                    SetDecorActive(decorLevel4_Arctic, false);
                    SetDecorActive(decorLevel5_Desert, false);
                    break;

                case 3:
                    targetMat = fallMaterial != null ? fallMaterial : springMaterial;
                    SetDecorActive(decorLevel1_Temperate, false);
                    SetDecorActive(decorLevel2_Tropical, false);
                    SetDecorActive(decorLevel3_Autumn, true);
                    SetDecorActive(decorLevel4_Arctic, false);
                    SetDecorActive(decorLevel5_Desert, false);
                    break;

                case 4:
                    targetMat = winterMaterial != null ? winterMaterial : springMaterial;
                    SetDecorActive(decorLevel1_Temperate, false);
                    SetDecorActive(decorLevel2_Tropical, false);
                    SetDecorActive(decorLevel3_Autumn, false);
                    SetDecorActive(decorLevel4_Arctic, true);
                    SetDecorActive(decorLevel5_Desert, false);
                    break;

                case 5:
                    targetMat = desertMaterial != null ? desertMaterial : springMaterial;
                    SetDecorActive(decorLevel1_Temperate, false);
                    SetDecorActive(decorLevel2_Tropical, false);
                    SetDecorActive(decorLevel3_Autumn, false);
                    SetDecorActive(decorLevel4_Arctic, false);
                    SetDecorActive(decorLevel5_Desert, true);
                    break;

                default:
                    targetMat = springMaterial;
                    break;
            }

            if (targetMat != null)
            {
                SetMaterialOnAll(targetMat);
            }

            Debug.Log($"[ToonTheme] Successfully applied Theme & Decor for Level {order}: {world.worldName} (Material: {targetMat?.name})");
        }

        private void SetDecorActive(GameObject decorObj, bool active)
        {
            if (decorObj != null && decorObj.activeSelf != active)
            {
                decorObj.SetActive(active);
            }
        }

        public void SetMaterialOnAll(Material mat)
        {
            if (mat == null) return;
            for (int i = 0; i < managedRenderers.Count; i++)
            {
                if (managedRenderers[i] != null)
                {
                    managedRenderers[i].sharedMaterial = mat;
                }
            }
        }

        [ContextMenu("Collect All Child Renderers")]
        public void CollectAllRenderers()
        {
            managedRenderers.Clear();
            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (r != null && r.sharedMaterial != null && !r.sharedMaterial.name.StartsWith("Color"))
                {
                    managedRenderers.Add(r);
                }
            }
        }

        public void SetThemeMaterials(Material spring, Material summer, Material fall, Material winter, Material desert)
        {
            springMaterial = spring;
            summerMaterial = summer;
            fallMaterial = fall;
            winterMaterial = winter;
            desertMaterial = desert;
        }
    }
}
