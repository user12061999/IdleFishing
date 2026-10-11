using System.Collections.Generic;
using UnityEngine;

namespace IdleFishing.Core
{
    public class CrowdAvoidance : MonoBehaviour
    {
        private static readonly List<CrowdAvoidance> activeAgents = new List<CrowdAvoidance>();

        [Header("Agent Collision Profile")]
        [SerializeField] private float agentRadius = 0.45f;
        [SerializeField] private bool isPushable = true;
        [SerializeField] private float priorityWeight = 1.0f; // e.g. Transporter xe đẩy = 1.5, Wrangler = 1.1, Khách = 1.0, Ngồi câu = 5.0
        [SerializeField] private bool isStationary = false;   // Ngồi yên hoặc đang bốc dỡ hàng cố định

        public float Radius => agentRadius;
        public bool IsPushable => isPushable;
        public float PriorityWeight => priorityWeight;
        public bool IsStationary { get => isStationary; set => isStationary = value; }

        private void OnEnable()
        {
            if (!activeAgents.Contains(this))
            {
                activeAgents.Add(this);
            }
        }

        private void OnDisable()
        {
            activeAgents.Remove(this);
        }

        public void Setup(float radius, float priority = 1.0f, bool stationary = false)
        {
            agentRadius = radius;
            priorityWeight = priority;
            isStationary = stationary;
        }

        /// <summary>
        /// Tính toán hướng di chuyển được bẻ lái mượt mà (Tangential Steering / Side-stepping)
        /// Luôn duy trì vận tốc hướng tới mục tiêu, chủ động lượn sang hai bên khi gặp người đối diện hoặc vật cản.
        /// </summary>
        public Vector3 GetSteeredDirection(Vector3 targetPos)
        {
            if (isStationary) return Vector3.zero;

            Vector3 currentPos = transform.position;
            Vector3 desiredVector = targetPos - currentPos;
            desiredVector.y = 0f;

            float distToTarget = desiredVector.magnitude;
            if (distToTarget < 0.05f) return Vector3.zero;

            Vector3 desiredDir = desiredVector / distToTarget;
            // Trục vuông góc sang bên phải của hướng di chuyển (Lateral vector)
            Vector3 rightDir = new Vector3(desiredDir.z, 0f, -desiredDir.x);

            Vector3 lateralAvoidance = Vector3.zero;
            int neighborCount = 0;

            for (int i = 0; i < activeAgents.Count; i++)
            {
                CrowdAvoidance other = activeAgents[i];
                if (other == null || other == this || !other.isActiveAndEnabled) continue;

                Vector3 toOther = other.transform.position - currentPos;
                toOther.y = 0f;
                float dist = toOther.magnitude;

                // Khoảng cách phát hiện & phản xạ né tránh
                float combinedRadius = agentRadius + other.agentRadius;
                float safeDist = combinedRadius + 0.45f;

                if (dist > safeDist || dist < 0.0001f) continue;

                // Kiểm tra góc phía trước: chỉ né các agent ở phía trước hoặc bên hông (không né người đi phía sau lưng)
                float forwardDot = Vector3.Dot(desiredDir, toOther / dist);
                if (forwardDot < -0.2f && dist > combinedRadius * 0.9f)
                {
                    // Agent kia đang ở phía sau lưng và không chạm đè, bỏ qua
                    continue;
                }

                // Chiếu vị trí agent kia lên trục vuông góc (bên phải / bên trái)
                float lateralOffset = Vector3.Dot(rightDir, toOther);

                // Quyết định hướng bẻ lái né tránh (Side-stepping)
                Vector3 evadeSide;
                if (lateralOffset > 0.06f)
                {
                    // Đối phương nằm bên phải -> ta lách sang trái
                    evadeSide = -rightDir;
                }
                else if (lateralOffset < -0.06f)
                {
                    // Đối phương nằm bên trái -> ta lách sang phải
                    evadeSide = rightDir;
                }
                else
                {
                    // Đi đối đầu trực diện hoặc cùng làn thẳng:
                    // Quy tắc giao thông quốc tế: Cả hai cùng lách sang bên phải của mình (keep right)
                    evadeSide = rightDir;
                }

                // Cường độ né tránh: tỉ lệ nghịch với khoảng cách (càng gần bẻ lái càng rõ)
                float proximityWeight = Mathf.Clamp01(1.0f - (dist / safeDist));
                // Điều chỉnh theo trọng số ưu tiên giữa 2 agent (xe đẩy / thợ ưu tiên hơn khách)
                float relativePriority = other.priorityWeight / Mathf.Max(0.2f, priorityWeight);

                lateralAvoidance += evadeSide * (proximityWeight * 1.5f * relativePriority);
                neighborCount++;
            }

            if (neighborCount > 0)
            {
                // Giữ lực đẩy tới đích phía trước (ít nhất 0.85f) và thêm lực lượn ngang bên hông
                Vector3 blended = (desiredDir * 0.85f + lateralAvoidance).normalized;
                // Đảm bảo không bao giờ bị lực cản đẩy giật lùi ngược hướng mục tiêu
                if (Vector3.Dot(blended, desiredDir) < 0.25f)
                {
                    // Nếu lực né ép ngang quá mạnh, bẻ chéo 45 độ về phía trước-ngang
                    Vector3 sideOnly = Vector3.Project(blended, rightDir).normalized;
                    if (sideOnly == Vector3.zero) sideOnly = rightDir;
                    blended = (desiredDir * 0.45f + sideOnly * 0.85f).normalized;
                }
                return blended;
            }

            return desiredDir;
        }

        /// <summary>
        /// Tính toán bước di chuyển tiếp theo kèm cơ chế chống lún mềm sang hai bên (Soft lateral separation).
        /// </summary>
        public Vector3 CalculateStep(Vector3 targetPos, float speed, float deltaTime)
        {
            if (isStationary) return transform.position;

            Vector3 currentPos = transform.position;
            Vector3 desiredVector = targetPos - currentPos;
            desiredVector.y = 0f;
            float distToTarget = desiredVector.magnitude;
            if (distToTarget < 0.02f) return currentPos;

            Vector3 steeredDir = GetSteeredDirection(targetPos);
            if (steeredDir == Vector3.zero) return currentPos;

            float maxStep = Mathf.Min(speed * deltaTime, distToTarget);
            Vector3 nextPos = currentPos + steeredDir * maxStep;

            // Cơ chế cản va chạm mềm (Soft separation): Đẩy dạt sang hai bên thay vì kéo giật lùi
            Vector3 rightDir = new Vector3(steeredDir.z, 0f, -steeredDir.x);

            for (int i = 0; i < activeAgents.Count; i++)
            {
                CrowdAvoidance other = activeAgents[i];
                if (other == null || other == this || !other.isActiveAndEnabled) continue;

                Vector3 diff = nextPos - other.transform.position;
                diff.y = 0f;
                float dist = diff.magnitude;
                float hardRadius = (agentRadius + other.agentRadius) * 0.92f;

                if (dist < hardRadius)
                {
                    float overlap = hardRadius - dist;
                    Vector3 pushDir;

                    if (dist > 0.001f)
                    {
                        pushDir = diff / dist;
                    }
                    else
                    {
                        // Trùng tọa độ hoàn toàn: đẩy dạt sang bên phải
                        pushDir = rightDir;
                    }

                    // Ưu tiên đẩy dạt ngang (lateral separation) để không triệt tiêu bước tiến tới
                    float lateralProj = Vector3.Dot(pushDir, rightDir);
                    Vector3 lateralPush = rightDir * (Mathf.Sign(lateralProj != 0 ? lateralProj : 1f) * overlap * 0.55f);
                    nextPos += lateralPush;
                }
            }

            nextPos.y = currentPos.y;
            return nextPos;
        }
    }
}
