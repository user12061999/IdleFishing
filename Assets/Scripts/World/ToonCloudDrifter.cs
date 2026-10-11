using UnityEngine;

namespace IdleFishing.World
{
    /// <summary>
    /// Tạo hiệu ứng mây trôi bồng bềnh nhẹ nhàng trên bầu trời của màn chơi.
    /// </summary>
    public class ToonCloudDrifter : MonoBehaviour
    {
        [Header("Drift Settings")]
        [SerializeField] private float driftSpeed = 0.6f;
        [SerializeField] private float minX = -26f;
        [SerializeField] private float maxX = 26f;

        [Header("Bobbing Settings")]
        [SerializeField] private float bobHeight = 0.35f;
        [SerializeField] private float bobFrequency = 0.8f;

        private float startY;
        private float randomOffset;

        private void Start()
        {
            startY = transform.position.y;
            randomOffset = Random.Range(0f, 10f);
        }

        private void Update()
        {
            Vector3 pos = transform.position;
            pos.x += driftSpeed * Time.deltaTime;

            if (driftSpeed > 0 && pos.x > maxX)
            {
                pos.x = minX;
            }
            else if (driftSpeed < 0 && pos.x < minX)
            {
                pos.x = maxX;
            }

            pos.y = startY + Mathf.Sin((Time.time + randomOffset) * bobFrequency) * bobHeight;
            transform.position = pos;
        }
    }
}
