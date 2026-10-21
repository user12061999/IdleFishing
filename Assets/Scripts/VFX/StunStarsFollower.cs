using UnityEngine;

namespace IdleFishing.VFX
{
    /// <summary>
    /// Smoothly anchors a VFX (such as circling stun stars) to a target transform with a world offset,
    /// without inheriting the target's rotation/tilt (e.g. when a character is knocked out on their back).
    /// </summary>
    public class StunStarsFollower : MonoBehaviour
    {
        private Transform target;
        private Vector3 worldOffset;

        public void Setup(Transform targetTransform, Vector3 offset)
        {
            target = targetTransform;
            worldOffset = offset;
            if (target != null)
            {
                transform.position = target.position + worldOffset;
            }
        }

        private void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = target.position + worldOffset;
        }
    }
}
