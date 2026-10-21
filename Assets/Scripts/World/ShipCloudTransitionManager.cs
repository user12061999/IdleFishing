using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IdleFishing.World
{
    /// <summary>
    /// Quản lý hiệu ứng Mây Toon 3D trong World Space:
    /// - Khi thuyền nhổ neo: Các cụm mây trắng Toon xốp bồng bềnh sinh ra bao phủ kín mít vị trí con thuyền trên biển.
    /// - Khi đến thế giới mới: Các cụm mây tại vị trí con thuyền mới bắt đầu tản dạt sang hai bên và tan biến dần, để lộ con thuyền mới.
    /// </summary>
    public class ShipCloudTransitionManager : MonoBehaviour
    {
        private static ShipCloudTransitionManager _instance;
        public static ShipCloudTransitionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<ShipCloudTransitionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("--- SHIP CLOUD TRANSITION MANAGER ---");
                        _instance = go.AddComponent<ShipCloudTransitionManager>();
                    }
                }
                return _instance;
            }
        }

        private GameObject cloudBoxPrefab;
        private GameObject cloudLongPrefab;
        private Material cloudMaterial;

        private readonly List<GameObject> activeCloudPuffs = new List<GameObject>();
        private Coroutine activeCloudCoroutine;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            LoadCloudAssets();
        }

        private void LoadCloudAssets()
        {
#if UNITY_EDITOR
            cloudBoxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Prefabs/Props/Cloud_Box.prefab");
            cloudLongPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Prefabs/Props/Cloud_Long.prefab");
            cloudMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Materials/Summer.mat");
#endif
        }

        /// <summary>
        /// Tạo cụm mây Toon trắng bồng bềnh nở to từ mặt nước bao phủ lấy vị trí con thuyền (Đã tắt theo yêu cầu)
        /// </summary>
        public void SpawnCloudsEngulfing(Vector3 shipCenter, float duration = 1.6f)
        {
            ClearActiveClouds();
        }

        /// <summary>
        /// Mây tại vị trí thuyền đến bắt đầu tản dạt sang hai bên và tan biến dần (Đã tắt theo yêu cầu)
        /// </summary>
        public void PartAndDisperseClouds(Vector3 arrivalCenter, float duration = 1.4f)
        {
            ClearActiveClouds();
        }

        private IEnumerator EngulfRoutine(Vector3 center, float duration)
        {
            int cloudCount = 12;
            var cloudData = new List<CloudInstanceData>();

            for (int i = 0; i < cloudCount; i++)
            {
                // Mây xuất phát từ vòng tròn rộng xung quanh (R = 6-9m), bay hướng tâm vào giữa con thuyền
                float angle = (i / (float)cloudCount) * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
                Vector3 radialDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                float outerRadius = Random.Range(6.0f, 9.5f);
                Vector3 outerPos = center + radialDir * outerRadius + Vector3.up * Random.Range(0.4f, 1.8f);

                float targetRadius = Random.Range(0.8f, 2.4f);
                Vector3 targetPos = center + radialDir * targetRadius + Vector3.up * Random.Range(0.2f, 1.2f);

                GameObject cloudObj = CreateSingleToonCloud(i);
                cloudObj.transform.position = outerPos;
                cloudObj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                cloudObj.transform.localScale = Vector3.zero;

                activeCloudPuffs.Add(cloudObj);

                float finalScale = Random.Range(1.8f, 2.7f);
                cloudData.Add(new CloudInstanceData
                {
                    obj = cloudObj,
                    startPos = outerPos,
                    targetPos = targetPos,
                    targetScale = new Vector3(finalScale * 1.2f, finalScale * 0.9f, finalScale * 1.1f),
                    rotationSpeed = Random.Range(-22f, 22f),
                    bobPhase = Random.Range(0f, Mathf.PI * 2f)
                });
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float moveT = EaseOutQuad(t);
                float scaleT = EaseOutBack(t);

                foreach (var cd in cloudData)
                {
                    if (cd.obj != null)
                    {
                        cd.obj.transform.position = Vector3.Lerp(cd.startPos, cd.targetPos, moveT) + Vector3.up * Mathf.Sin(Time.time * 2f + cd.bobPhase) * 0.08f;
                        cd.obj.transform.localScale = cd.targetScale * scaleT;
                        cd.obj.transform.Rotate(Vector3.up, cd.rotationSpeed * Time.deltaTime, Space.World);
                    }
                }
                yield return null;
            }

            // Giữ nguyên vị trí ở giữa bao quanh thuyền
            foreach (var cd in cloudData)
            {
                if (cd.obj != null)
                {
                    cd.obj.transform.localScale = cd.targetScale;
                }
            }
        }

        private IEnumerator DisperseRoutine(Vector3 arrivalCenter, float duration)
        {
            // 1. Luôn dọn sạch hoàn toàn các đám mây cũ của bến xuất phát
            ClearActiveClouds();

            // 2. Sinh cụm mây Toon bọc kín con thuyền mới tại vị trí bến đến (arrivalCenter)
            int cloudCount = 14;
            var disperseData = new List<DisperseInstanceData>();

            for (int i = 0; i < cloudCount; i++)
            {
                // Phân bổ đều theo hình tròn 360 độ xung quanh tâm thuyền đến
                float angle = (i / (float)cloudCount) * Mathf.PI * 2f + Random.Range(-0.1f, 0.1f);
                Vector3 radialDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                float radius = Random.Range(0.8f, 2.6f);
                Vector3 initialCloudPos = arrivalCenter + radialDir * radius + Vector3.up * Random.Range(0.2f, 1.3f);

                GameObject cloudObj = CreateSingleToonCloud(i);
                cloudObj.transform.position = initialCloudPos;
                cloudObj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float s = Random.Range(2.0f, 2.9f);
                Vector3 startScale = new Vector3(s * 1.2f, s * 0.9f, s * 1.1f);
                cloudObj.transform.localScale = startScale;
                activeCloudPuffs.Add(cloudObj);

                // Mây bay tỏa đều ra xung quanh theo hình tròn (Radial burst outward 360 degrees)
                Vector3 endPos = initialCloudPos + radialDir * Random.Range(11.0f, 18.0f) + Vector3.up * Random.Range(0.3f, 1.2f);

                disperseData.Add(new DisperseInstanceData
                {
                    obj = cloudObj,
                    startPos = initialCloudPos,
                    endPos = endPos,
                    startScale = startScale,
                    rotSpeed = Random.Range(20f, 40f) * ((i % 2 == 0) ? 1f : -1f)
                });
            }

            // 3. Animate mây bay tỏa đều ra xung quanh theo hình tròn và scale giảm mượt về 0
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float moveT = EaseInOutQuad(t);
                float scaleT = Mathf.Clamp01(1f - Mathf.Pow(t, 1.4f));

                foreach (var dd in disperseData)
                {
                    if (dd.obj != null)
                    {
                        dd.obj.transform.position = Vector3.Lerp(dd.startPos, dd.endPos, moveT);
                        dd.obj.transform.localScale = dd.startScale * scaleT;
                        dd.obj.transform.Rotate(Vector3.up, dd.rotSpeed * Time.deltaTime, Space.World);
                    }
                }
                yield return null;
            }

            ClearActiveClouds();
            activeCloudCoroutine = null;
        }

        private GameObject CreateSingleToonCloud(int index)
        {
            GameObject basePrefab = (index % 2 == 0) ? cloudBoxPrefab : cloudLongPrefab;
            GameObject cloudObj;

            if (basePrefab != null)
            {
                cloudObj = Instantiate(basePrefab, transform);
            }
            else
            {
                // Fallback procedural puffy cluster
                cloudObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cloudObj.transform.SetParent(transform, false);
                var col = cloudObj.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            cloudObj.name = $"Transition_CloudPuff_{index}";
            var mr = cloudObj.GetComponentInChildren<MeshRenderer>();
            if (mr != null && cloudMaterial != null)
            {
                mr.sharedMaterial = cloudMaterial;
            }

            return cloudObj;
        }

        public void ClearActiveClouds()
        {
            foreach (var go in activeCloudPuffs)
            {
                if (go != null) Destroy(go);
            }
            activeCloudPuffs.Clear();
        }

        private float EaseOutBack(float x)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private float EaseOutQuad(float x)
        {
            return 1f - (1f - x) * (1f - x);
        }

        private float EaseInOutQuad(float x)
        {
            return x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;
        }

        private struct CloudInstanceData
        {
            public GameObject obj;
            public Vector3 startPos;
            public Vector3 targetPos;
            public Vector3 targetScale;
            public float rotationSpeed;
            public float bobPhase;
        }

        private struct DisperseInstanceData
        {
            public GameObject obj;
            public Vector3 startPos;
            public Vector3 endPos;
            public Vector3 startScale;
            public float rotSpeed;
        }
    }
}
