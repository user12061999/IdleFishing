using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Audio;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Inventory;
using IdleFishing.UI;

namespace IdleFishing.World
{
    public class CargoShipEventManager : MonoBehaviour
    {
        public static CargoShipEventManager Instance { get; private set; }

        [Header("Ship Motion")]
        [SerializeField] private float bobbingSpeed = 1.6f;
        [SerializeField] private float bobbingHeight = 0.14f;
        [SerializeField] private float rollAngle = 2.8f;

        [Header("Ship Delivery Event")]
        [SerializeField] private float deliveryInterval = 40.0f;
        [SerializeField] private int bulkCoinReward = 220;
        [SerializeField] private Inventory.Inventory harborStorage;
        [SerializeField] private List<ItemData> possibleFishCargo = new List<ItemData>();

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float nextDeliveryTime = 0f;
        private float lastTapTime = -10f;
        private const float TapCooldown = 8.0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Start()
        {
            nextDeliveryTime = Time.time + deliveryInterval;
        }

        private void Update()
        {
            // Hiệu ứng dập dềnh sóng biển mượt mà
            float time = Time.time * bobbingSpeed;
            float offsetY = Mathf.Sin(time) * bobbingHeight;
            float pitch = Mathf.Sin(time * 0.7f) * (rollAngle * 0.6f);
            float roll = Mathf.Cos(time * 0.9f) * rollAngle;

            transform.position = initialPosition + new Vector3(0f, offsetY, 0f);
            transform.rotation = initialRotation * Quaternion.Euler(pitch, 0f, roll);

            // Chu kỳ tàu hàng cập bến định kỳ
            if (Time.time >= nextDeliveryTime)
            {
                nextDeliveryTime = Time.time + deliveryInterval;
                DeliverCargo(isManualTap: false);
            }
        }

        public void Setup(Inventory.Inventory storage, List<ItemData> cargoItems)
        {
            harborStorage = storage;
            possibleFishCargo = cargoItems;
        }

        public void DeliverCargo(bool isManualTap = false)
        {
            // 1. Thưởng tiền vàng từ chuyến hàng thương mại
            int coins = isManualTap ? Mathf.RoundToInt(bulkCoinReward * 0.65f) : bulkCoinReward;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(coins);
            }

            // 2. Chuyển thêm 2 khay/con cá trực tiếp vào kho cảng nếu còn chỗ
            int itemsDelivered = 0;
            if (harborStorage != null && possibleFishCargo != null && possibleFishCargo.Count > 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (harborStorage.IsFull) break;
                    ItemData randomItem = possibleFishCargo[Random.Range(0, possibleFishCargo.Count)];
                    if (randomItem != null && harborStorage.TryAdd(randomItem, 1, out _))
                    {
                        itemsDelivered++;
                    }
                }
            }

            // 3. Hiệu ứng còi tàu và hiển thị thưởng
            string hornTag = isManualTap ? "TOOT! SHIP HORN BONUS!" : "CARGO SHIP DOCKED!";
            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 2.8f,
                    coins,
                    DamageStyle.GoldBonus,
                    hornTag
                );
            }

            if (FlyingCoinManager.Instance != null)
            {
                FlyingCoinManager.Instance.PlayFlyingCoins(transform.position + Vector3.up * 2.0f, 4);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayChestOpen();
            }

            Debug.Log($"[CargoShip] Cargo ship docked: +{coins} G earned, loaded +{itemsDelivered} fish into harbor storage!");
        }

        private void OnMouseDown()
        {
            if (Time.time - lastTapTime >= TapCooldown)
            {
                lastTapTime = Time.time;
                DeliverCargo(isManualTap: true);
            }
            else
            {
                float remain = TapCooldown - (Time.time - lastTapTime);
                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        transform.position + Vector3.up * 2.2f,
                        0,
                        DamageStyle.FishAttack,
                        $"OUT AT SEA ({remain:0.0}s)"
                    );
                }
            }
        }
    }
}
