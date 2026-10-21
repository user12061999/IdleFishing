using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Aquarium;
using IdleFishing.Data;

namespace IdleFishing.UI
{
    public class PreviewFishIdleSway : MonoBehaviour
    {
        public float baseAngleY = -55f;
        public float swaySpeed = 1.8f;
        public float swayAngle = 10f;
        public float floatSpeed = 1.5f;
        public float floatAmp = 0.04f;
        private float seed;

        private void Awake()
        {
            seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            float t = Time.unscaledTime + seed;
            float sway = Mathf.Sin(t * swaySpeed) * swayAngle;
            float bob = Mathf.Sin(t * floatSpeed) * floatAmp;
            transform.localRotation = Quaternion.Euler(4f, baseAngleY + sway, 0f);
            transform.localPosition = new Vector3(0f, bob, 0f);
        }
    }

    public class AquariumFishPreviewManager : MonoBehaviour
    {
        public static AquariumFishPreviewManager Instance { get; private set; }

        public class PreviewHandle
        {
            public int slotIndex;
            public RenderTexture texture;
            public GameObject stationObj;
            public Camera previewCamera;

            public void Release()
            {
                if (previewCamera != null)
                {
                    previewCamera.targetTexture = null;
                }
                if (texture != null)
                {
                    texture.Release();
                    Object.Destroy(texture);
                    texture = null;
                }
                if (stationObj != null)
                {
                    Object.Destroy(stationObj);
                    stationObj = null;
                }
            }
        }

        private readonly List<PreviewHandle> activeHandles = new List<PreviewHandle>();
        private readonly Vector3 baseOrigin = new Vector3(2000f, 500f, 2000f);
        private int nextSlotIndex = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            ClearAll();
        }

        public PreviewHandle RequestPreview(string speciesId, bool isBoss, float weightKg)
        {
            int slot = nextSlotIndex++;
            Vector3 stationPos = baseOrigin + new Vector3(slot * 12f, 0f, 0f);

            GameObject station = new GameObject($"FishPreviewStation_{speciesId}_{slot}");
            station.transform.position = stationPos;

            // 1. Studio Lighting Rig
            GameObject lightObj = new GameObject("KeyLight");
            lightObj.transform.SetParent(station.transform, false);
            lightObj.transform.localPosition = new Vector3(1.2f, 2.0f, -1.8f);
            var keyLight = lightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1.0f, 0.96f, 0.90f);
            keyLight.intensity = 1.35f;
            lightObj.transform.LookAt(stationPos);

            GameObject rimObj = new GameObject("RimLight");
            rimObj.transform.SetParent(station.transform, false);
            rimObj.transform.localPosition = new Vector3(-1.8f, -0.6f, 1.5f);
            var rimLight = rimObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = new Color(0.35f, 0.70f, 1.0f);
            rimLight.intensity = 0.90f;
            rimObj.transform.LookAt(stationPos);

            // 2. Camera Setup (Closer to make 3D fish models large and prominent)
            GameObject camObj = new GameObject("PreviewCam");
            camObj.transform.SetParent(station.transform, false);
            camObj.transform.localPosition = new Vector3(0f, 0.02f, -1.95f);
            camObj.transform.localRotation = Quaternion.Euler(2f, 0f, 0f);

            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 29f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 12f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.08f, 0.15f, 1f);

            var rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            rt.name = $"RT_FishPreview_{speciesId}_{slot}";
            rt.Create();
            cam.targetTexture = rt;

            // 3. 3D Model Spawning
            GameObject prefab = GetFishPrefab(speciesId);
            GameObject fishModel;
            if (prefab != null)
            {
                fishModel = Instantiate(prefab, station.transform);
                fishModel.name = "Model_" + speciesId;
                fishModel.transform.localPosition = Vector3.zero;
                fishModel.transform.localRotation = Quaternion.Euler(4f, -55f, 0f);

                float baseScale = GetSpeciesBaseScale(speciesId);
                float bossFactor = isBoss ? 1.35f : 1.0f;
                fishModel.transform.localScale = Vector3.one * (baseScale * bossFactor);

                foreach (var col in fishModel.GetComponentsInChildren<Collider>())
                {
                    Destroy(col);
                }

                var animator = fishModel.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.enabled = true;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                fishModel.AddComponent<PreviewFishIdleSway>();
            }
            else
            {
                fishModel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fishModel.name = "FallbackBody";
                fishModel.transform.SetParent(station.transform, false);
                fishModel.transform.localRotation = Quaternion.Euler(90f, -55f, 0f);
                fishModel.transform.localScale = new Vector3(0.3f, 0.6f, 0.25f);
                var col = fishModel.GetComponent<Collider>();
                if (col != null) Destroy(col);
                fishModel.AddComponent<PreviewFishIdleSway>();
            }

            var handle = new PreviewHandle
            {
                slotIndex = slot,
                texture = rt,
                stationObj = station,
                previewCamera = cam
            };

            activeHandles.Add(handle);
            return handle;
        }

        public void ReleasePreview(PreviewHandle handle)
        {
            if (handle == null) return;
            activeHandles.Remove(handle);
            handle.Release();
        }

        public void ClearAll()
        {
            for (int i = activeHandles.Count - 1; i >= 0; i--)
            {
                activeHandles[i]?.Release();
            }
            activeHandles.Clear();
            nextSlotIndex = 0;
        }

        public static GameObject GetFishPrefab(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId)) return null;

            if (BestiaryManager.Instance != null)
            {
                var entry = BestiaryManager.Instance.GetEntry(speciesId);
                if (entry != null && entry.chainData != null && entry.chainData.WholeFish != null && entry.chainData.WholeFish.Prefab != null)
                {
                    return entry.chainData.WholeFish.Prefab;
                }
            }

#if UNITY_EDITOR
            string path = null;
            switch (speciesId.ToLowerInvariant())
            {
                case "salmon": path = "Assets/Prefabs/Fish/Fish_Salmon_3D.prefab"; break;
                case "cod": path = "Assets/Prefabs/Fish/Fish_Cod_3D.prefab"; break;
                case "herring": path = "Assets/Prefabs/Fish/Fish_Herring_3D.prefab"; break;
                case "arctic_tuna": path = "Assets/Prefabs/Fish/Fish_ArcticTuna_3D.prefab"; break;
                case "yellowfin_tuna": path = "Assets/Prefabs/Fish/Fish_YellowfinTuna_3D.prefab"; break;
                case "snow_crab": path = "Assets/Prefabs/Fish/Fish_SnowCrab_3D.prefab"; break;
                case "lobster": path = "Assets/Prefabs/Fish/Fish_Lobster_3D.prefab"; break;
                case "green_turtle": path = "Assets/Prefabs/Fish/Fish_GreenTurtle_3D.prefab"; break;
                case "clownfish": path = "Assets/Prefabs/Fish/Fish_Clownfish_3D.prefab"; break;
                case "pufferfish": path = "Assets/Prefabs/Fish/Fish_Pufferfish_3D.prefab"; break;
                case "blue_tang": path = "Assets/Prefabs/Fish/Fish_BlueTang_3D.prefab"; break;
                case "green_chromis": path = "Assets/Prefabs/Fish/Fish_GreenChromis_3D.prefab"; break;
                case "yellow_tang": path = "Assets/Prefabs/Fish/Fish_YellowTang_3D.prefab"; break;
                case "sailfin_tang": path = "Assets/Prefabs/Fish/Fish_SailfinTang_3D.prefab"; break;
                case "flame_angelfish": path = "Assets/Prefabs/Fish/Fish_FlameAngelfish_3D.prefab"; break;
                case "queen_angelfish": path = "Assets/Prefabs/Fish/Fish_QueenAngelfish_3D.prefab"; break;
                case "longnose_butterfly": path = "Assets/Prefabs/Fish/Fish_LongnoseButterfly_3D.prefab"; break;
                case "panther_grouper": path = "Assets/Prefabs/Fish/Fish_PantherGrouper_3D.prefab"; break;
                case "rose_goby": path = "Assets/Prefabs/Fish/Fish_RoseGoby_3D.prefab"; break;
                case "zebra_seahorse": path = "Assets/Prefabs/Fish/Fish_ZebraSeahorse_3D.prefab"; break;
                case "reef_shark": path = "Assets/Prefabs/Fish/Fish_ReefShark_Boss_3D.prefab"; break;
                case "eagle_ray": path = "Assets/Prefabs/Fish/Fish_EagleRay_Boss_3D.prefab"; break;
                case "thresher_shark": path = "Assets/Prefabs/Fish/Fish_ThresherShark_Boss_3D.prefab"; break;
                case "manta_ray": path = "Assets/Prefabs/Fish/Fish_MantaRay_Boss_3D.prefab"; break;
                case "great_white_shark": path = "Assets/Prefabs/Fish/Fish_GreatWhiteShark_Boss_3D.prefab"; break;
            }

            if (!string.IsNullOrEmpty(path))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
#endif
            return null;
        }

        public static float GetSpeciesBaseScale(string speciesId)
        {
            switch (speciesId != null ? speciesId.ToLowerInvariant() : "")
            {
                case "salmon": return 0.65f;
                case "cod": return 0.62f;
                case "herring": return 0.58f;
                case "arctic_tuna": return 0.65f;
                case "yellowfin_tuna": return 0.65f;
                case "snow_crab": return 0.55f;
                case "lobster": return 0.55f;
                case "green_turtle": return 0.55f;
                case "clownfish": return 0.60f;
                case "pufferfish": return 0.60f;
                case "blue_tang": return 0.60f;
                case "green_chromis": return 0.58f;
                case "yellow_tang": return 0.60f;
                case "sailfin_tang": return 0.60f;
                case "flame_angelfish": return 0.58f;
                case "queen_angelfish": return 0.60f;
                case "longnose_butterfly": return 0.58f;
                case "panther_grouper": return 0.60f;
                case "rose_goby": return 0.56f;
                case "zebra_seahorse": return 0.55f;
                case "reef_shark": return 0.60f;
                case "eagle_ray": return 0.56f;
                case "thresher_shark": return 0.60f;
                case "manta_ray": return 0.56f;
                case "great_white_shark": return 0.62f;
                default: return 0.60f;
            }
        }
    }
}
