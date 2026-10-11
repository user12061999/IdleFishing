using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Economy;
using IdleFishing.UI;

namespace IdleFishing.World
{
    public class SeasideCafeController : MonoBehaviour
    {
        public static SeasideCafeController Instance { get; private set; }

        [Header("Cafe Settings")]
        [SerializeField] private int cafeLevel = 1;
        [SerializeField] private float passiveIncomeInterval = 5.0f;
        [SerializeField] private int passiveIncomeAmount = 18;
        [SerializeField] private bool isOpen = true;

        [Header("Patio Seats")]
        [SerializeField] private List<Transform> seatWaypoints = new List<Transform>();

        private float nextPassiveTime = 0f;
        private readonly HashSet<Transform> occupiedSeats = new HashSet<Transform>();

        public bool IsOpen => isOpen;
        public int CafeLevel => cafeLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            nextPassiveTime = Time.time + passiveIncomeInterval;
        }

        private void Update()
        {
            // Doanh thu tự động từ quán nước bờ biển
            if (isOpen && Time.time >= nextPassiveTime)
            {
                nextPassiveTime = Time.time + passiveIncomeInterval;
                EarnPassiveIncome();
            }
        }

        public void SetupSeats(List<Transform> seats)
        {
            seatWaypoints = seats;
        }

        private void EarnPassiveIncome()
        {
            int earnings = passiveIncomeAmount * cafeLevel;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(earnings);
            }

            if (FlyingCoinManager.Instance != null)
            {
                FlyingCoinManager.Instance.PlayFlyingCoins(transform.position + Vector3.up * 1.5f, 2);
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 2.2f,
                    earnings,
                    DamageStyle.GoldBonus,
                    "+COFFEE SHOP"
                );
            }
        }

        public bool TryGetAvailableSeat(out Transform seat)
        {
            seat = null;
            if (!isOpen || seatWaypoints == null || seatWaypoints.Count == 0) return false;

            for (int i = 0; i < seatWaypoints.Count; i++)
            {
                Transform s = seatWaypoints[i];
                if (s != null && !occupiedSeats.Contains(s))
                {
                    occupiedSeats.Add(s);
                    seat = s;
                    return true;
                }
            }
            return false;
        }

        public void ReleaseSeat(Transform seat)
        {
            if (seat != null)
            {
                occupiedSeats.Remove(seat);
            }
        }

        public void CustomerBuyDrink(Vector3 customerPos)
        {
            int drinkPrice = Random.Range(20, 45) * cafeLevel;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(drinkPrice);
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    customerPos + Vector3.up * 1.8f,
                    drinkPrice,
                    DamageStyle.GoldBonus,
                    "COCONUT DRINK"
                );
            }

            if (FlyingCoinManager.Instance != null)
            {
                FlyingCoinManager.Instance.PlayFlyingCoins(customerPos + Vector3.up * 1.5f, 2);
            }
        }

        private void OnMouseDown()
        {
            // Chạm vào quán nước để pha chế đồ uống ngay lập tức!
            EarnPassiveIncome();
            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 2.5f,
                    0,
                    DamageStyle.GoldBonus,
                    "INSTANT BREW!"
                );
            }
        }
    }
}
