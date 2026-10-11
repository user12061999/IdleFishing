using System.Collections.Generic;
using UnityEngine;

namespace IdleFishing.World
{
    public class HarborAtmosphereDecor : MonoBehaviour
    {
        [Header("Seagull Ambient Flock")]
        [SerializeField] private int seagullCount = 3;
        [SerializeField] private Vector3 flockCenter = new Vector3(0f, 6.5f, 15.0f);
        [SerializeField] private float flightRadius = 5.5f;
        [SerializeField] private float flightSpeed = 35f;

        private readonly List<Transform> seagulls = new List<Transform>();
        private readonly List<float> flightOffsets = new List<float>();

        private class OceanSwimmer
        {
            public Transform transform;
            public Vector3 center;
            public float radiusX;
            public float radiusZ;
            public float speed;
            public float phaseOffset;
            public float baseDepth;
        }

        private readonly List<OceanSwimmer> oceanSwimmers = new List<OceanSwimmer>();

        private void Start()
        {
            SpawnSeagulls();
            SpawnOceanSeaLife();
        }

        private void Update()
        {
            UpdateSeagulls();
            UpdateOceanSeaLife();
        }

        private void SpawnSeagulls()
        {
            for (int i = 0; i < seagullCount; i++)
            {
                GameObject bird = new GameObject($"Ambient_Seagull_{i + 1}");
                bird.transform.SetParent(transform);

                // Body
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(bird.transform);
                body.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
                body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                SetObjectColor(body, new Color(0.95f, 0.95f, 0.95f));
                RemoveCollider(body);

                // Wings
                GameObject wingL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wingL.name = "Wing_L";
                wingL.transform.SetParent(bird.transform);
                wingL.transform.localPosition = new Vector3(-0.35f, 0.05f, 0f);
                wingL.transform.localScale = new Vector3(0.55f, 0.02f, 0.18f);
                wingL.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
                SetObjectColor(wingL, new Color(0.85f, 0.85f, 0.85f));
                RemoveCollider(wingL);

                GameObject wingR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wingR.name = "Wing_R";
                wingR.transform.SetParent(bird.transform);
                wingR.transform.localPosition = new Vector3(0.35f, 0.05f, 0f);
                wingR.transform.localScale = new Vector3(0.55f, 0.02f, 0.18f);
                wingR.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
                SetObjectColor(wingR, new Color(0.85f, 0.85f, 0.85f));
                RemoveCollider(wingR);

                // Beak
                GameObject beak = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beak.name = "Beak";
                beak.transform.SetParent(bird.transform);
                beak.transform.localPosition = new Vector3(0f, 0f, 0.24f);
                beak.transform.localScale = new Vector3(0.06f, 0.12f, 0.06f);
                beak.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                SetObjectColor(beak, new Color(1.0f, 0.75f, 0.1f)); // Orange beak
                RemoveCollider(beak);

                seagulls.Add(bird.transform);
                flightOffsets.Add(i * (360f / seagullCount));
            }
        }

        private void UpdateSeagulls()
        {
            float baseAngle = Time.time * flightSpeed;
            for (int i = 0; i < seagulls.Count; i++)
            {
                if (seagulls[i] == null) continue;

                float angle = (baseAngle + flightOffsets[i]) * Mathf.Deg2Rad;
                float x = flockCenter.x + Mathf.Cos(angle) * (flightRadius + (i * 0.4f));
                float z = flockCenter.z + Mathf.Sin(angle) * (flightRadius * 0.7f);
                float y = flockCenter.y + Mathf.Sin(Time.time * 2f + i) * 0.4f;

                Vector3 targetPos = new Vector3(x, y, z);
                Vector3 moveDir = (targetPos - seagulls[i].position).normalized;

                if (moveDir.sqrMagnitude > 0.001f)
                {
                    seagulls[i].rotation = Quaternion.Slerp(seagulls[i].rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 6f);
                }
                seagulls[i].position = targetPos;
            }
        }

        private void SpawnOceanSeaLife()
        {
#if UNITY_EDITOR
            string basePath = "Assets/Layer Lab/3D Props/3D Props Fish/3D Props Fish";
            var turtleCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{basePath}/Ani/turtle.controller");
            var rayCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{basePath}/Ani/ray.controller");
            var dolphinCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{basePath}/Ani/dolphin.controller");
            var fishCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{basePath}/Ani/fish.controller");

            CreateSwimmer($"{basePath}/Prefabs/GreenTurtle.prefab", turtleCtrl, new Vector3(-2.8f, -0.12f, 16.5f), 2.2f, 1.4f, 0.42f, 0f, 0.85f);
            CreateSwimmer($"{basePath}/Prefabs/MantaRay.prefab", rayCtrl, new Vector3(2.8f, -0.15f, 17.8f), 2.6f, 1.6f, 0.38f, 2.1f, 0.72f);
            CreateSwimmer($"{basePath}/Prefabs/PinkDolphin.prefab", dolphinCtrl, new Vector3(0.0f, -0.05f, 20.2f), 4.2f, 1.8f, 0.55f, 1.2f, 0.78f);
            CreateSwimmer($"{basePath}/Prefabs/BlueTang.prefab", fishCtrl, new Vector3(-3.8f, -0.10f, 15.2f), 1.5f, 1.1f, 0.75f, 0.5f, 2.2f);
            CreateSwimmer($"{basePath}/Prefabs/YellowTang.prefab", fishCtrl, new Vector3(3.6f, -0.10f, 15.4f), 1.5f, 1.1f, 0.72f, 3.5f, 2.2f);
            CreateSwimmer($"{basePath}/Prefabs/Clownfish.prefab", fishCtrl, new Vector3(0.4f, -0.10f, 16.2f), 1.4f, 1.0f, 0.80f, 4.8f, 2.5f);
#endif
        }

        private void CreateSwimmer(string prefabPath, RuntimeAnimatorController ctrl, Vector3 center, float rx, float rz, float speed, float phase, float scale)
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            GameObject obj = Instantiate(prefab, transform);
            obj.name = "Ambient_" + prefab.name;
            obj.transform.position = center;
            obj.transform.localScale = Vector3.one * scale;

            if (ctrl != null)
            {
                var anim = obj.GetComponent<Animator>();
                if (anim == null) anim = obj.AddComponent<Animator>();
                anim.runtimeAnimatorController = ctrl;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            oceanSwimmers.Add(new OceanSwimmer
            {
                transform = obj.transform,
                center = center,
                radiusX = rx,
                radiusZ = rz,
                speed = speed,
                phaseOffset = phase,
                baseDepth = center.y
            });
#endif
        }

        private void UpdateOceanSeaLife()
        {
            for (int i = 0; i < oceanSwimmers.Count; i++)
            {
                var s = oceanSwimmers[i];
                if (s == null || s.transform == null) continue;

                float t = Time.time * s.speed + s.phaseOffset;
                float x = s.center.x + Mathf.Cos(t) * s.radiusX;
                float z = s.center.z + Mathf.Sin(t * 2f) * (s.radiusZ * 0.55f); // Figure-8 path
                float y = s.baseDepth + Mathf.Sin(t * 1.5f) * 0.04f;

                Vector3 targetPos = new Vector3(x, y, z);
                Vector3 dir = targetPos - s.transform.position;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.0001f)
                {
                    s.transform.rotation = Quaternion.Slerp(s.transform.rotation, Quaternion.LookRotation(dir.normalized), Time.deltaTime * 5f);
                }
                s.transform.position = targetPos;
            }
        }

        private static void SetObjectColor(GameObject obj, Color color)
        {
            var r = obj.GetComponent<Renderer>();
            if (r != null && r.material != null)
            {
                if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", color);
                else if (r.material.HasProperty("_Color")) r.material.color = color;
            }
        }

        private static void RemoveCollider(GameObject obj)
        {
            var c = obj.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);
        }
    }
}
