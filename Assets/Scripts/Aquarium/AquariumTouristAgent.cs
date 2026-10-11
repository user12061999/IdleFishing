using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Economy;
using IdleFishing.UI;

namespace IdleFishing.Aquarium
{
    public class AquariumTouristAgent : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float viewingTime = 2.5f;

        private Vector3 viewSpot;
        private Transform exitPoint;
        private CrowdAvoidance crowdAgent;

        private void Awake()
        {
            crowdAgent = GetComponent<CrowdAvoidance>();
            if (crowdAgent == null)
            {
                crowdAgent = gameObject.AddComponent<CrowdAvoidance>();
                crowdAgent.Setup(radius: 0.42f, priority: 0.9f);
            }
        }

        public void Initialize(Vector3 spot, Transform exit)
        {
            // Spread tourists across the viewing deck width
            viewSpot = spot + new Vector3(Random.Range(-0.85f, 0.85f), 0, Random.Range(-0.3f, 0.3f));
            exitPoint = exit;
            StartCoroutine(TourRoutine());
        }

        private IEnumerator TourRoutine()
        {
            // 1. Walk to Aquarium Viewing Spot
            while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(viewSpot.x, 0, viewSpot.z)) > 0.35f)
            {
                Vector3 target = new Vector3(viewSpot.x, transform.position.y, viewSpot.z);
                Vector3 nextPos = crowdAgent != null
                    ? crowdAgent.CalculateStep(target, walkSpeed, Time.deltaTime)
                    : Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);

                Vector3 dir = (nextPos - transform.position).normalized;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
                }
                transform.position = nextPos;
                yield return null;
            }

            // 2. Face the Aquarium Tank
            if (AquariumTankVisual.Instance != null)
            {
                Vector3 lookDir = (AquariumTankVisual.Instance.transform.position - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }

            // 3. Admire the fish & Take photos
            yield return new WaitForSeconds(viewingTime * 0.4f);

            // Pop emoji
            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 1.8f,
                    0,
                    DamageStyle.GoldBonus,
                    "+TIP! ♥"
                );
            }

            // Add tourist ticket bonus
            if (AquariumManager.Instance != null && AquariumManager.Instance.CurrentFishCount > 0)
            {
                int tip = Random.Range(3, 8) * AquariumManager.Instance.TankDecorLevel;
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddCoins(tip);
                }

                if (FlyingCoinManager.Instance != null)
                {
                    FlyingCoinManager.Instance.PlayFlyingCoins(transform.position + Vector3.up * 1.5f, 2);
                }
            }

            yield return new WaitForSeconds(viewingTime * 0.6f);

            // 3b. Chance to visit Seaside Cafe for a refreshing drink
            if (IdleFishing.World.SeasideCafeController.Instance != null &&
                IdleFishing.World.SeasideCafeController.Instance.IsOpen &&
                Random.value < 0.65f)
            {
                if (IdleFishing.World.SeasideCafeController.Instance.TryGetAvailableSeat(out Transform cafeSeat))
                {
                    Vector3 seatPos = cafeSeat.position;
                    while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(seatPos.x, 0, seatPos.z)) > 0.35f)
                    {
                        Vector3 target = new Vector3(seatPos.x, transform.position.y, seatPos.z);
                        Vector3 nextPos = crowdAgent != null
                            ? crowdAgent.CalculateStep(target, walkSpeed, Time.deltaTime)
                            : Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);

                        Vector3 dir = (nextPos - transform.position).normalized;
                        dir.y = 0;
                        if (dir.sqrMagnitude > 0.001f)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
                        }
                        transform.position = nextPos;
                        yield return null;
                    }

                    // Relax and sip drink
                    yield return new WaitForSeconds(1.6f);
                    IdleFishing.World.SeasideCafeController.Instance.CustomerBuyDrink(transform.position);
                    IdleFishing.World.SeasideCafeController.Instance.ReleaseSeat(cafeSeat);
                }
            }

            // 4. Walk to exit
            if (exitPoint != null)
            {
                Vector3 exitPos = exitPoint.position;
                while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(exitPos.x, 0, exitPos.z)) > 0.4f)
                {
                    Vector3 target = new Vector3(exitPos.x, transform.position.y, exitPos.z);
                    Vector3 nextPos = crowdAgent != null
                        ? crowdAgent.CalculateStep(target, walkSpeed, Time.deltaTime)
                        : Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);

                    Vector3 dir = (nextPos - transform.position).normalized;
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
                    }
                    transform.position = nextPos;
                    yield return null;
                }
            }

            Destroy(gameObject);
        }
    }
}
