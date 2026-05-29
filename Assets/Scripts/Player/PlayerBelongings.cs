using System;
using System.Collections.Generic;
using Configurations;
using GameEvents;
using Player;
using SLTypes;
using UnityEngine;
using UnityServiceLocator;

namespace Player
{
    public class PlayerBelongings : MonoBehaviour, IPlayer
    {
        [SerializeField] private IntegerSO collectedCoins;
        [SerializeField] private IntegerSO health;
        [SerializeField] private float playerHeight;
        [SerializeField] private GameObject leftHandEquipParent, rightHandEquipParent;

        public Transform Transform => transform;
        public Vector3 Position => transform.position;
        public int CollectedCoins => collectedCoins.RuntimeValue;
        public int Health => health.RuntimeValue;
        public float Height => playerHeight;

        public GameObject LeftHandEquip
        {
            get => leftHandEquipParent;
            set => leftHandEquipParent = value;
        }
        public GameObject RightHandEquip
        {
            get => rightHandEquipParent;
            set => rightHandEquipParent = value;
        }

        public List<GameObject> CurrentGrounds { get; set; } = new();

        private void Awake()
        {
            ServiceLocator.Global.Register<IPlayer>(this);
        }

        private void Start()
        {
        }

        public void EquipItemToHand(PlayerEquipItemEvent e)
        {
            if (!e.leftHand)
                e.item.transform.parent = rightHandEquipParent.transform;
            else
                e.item.transform.parent = leftHandEquipParent.transform;
            
            e.item.transform.localPosition = Vector3.zero;
        }

        void OnEnable()
        {
            GameEventManager.AddListener<CollectedCoinEvent>(OnCoinCollection);
            GameEventManager.AddListener<SpentCoinsEvent>(OnCoinDispension);
            
            GameEventManager.AddListener<PlayerDamageEvent>(OnDamage);
            GameEventManager.AddListener<PlayerHealEvent>(OnHealing);
            
            GameEventManager.AddListener<PlayerEquipItemEvent>(EquipItemToHand);
        }

        void OnDisable()
        {
            GameEventManager.RemoveListener<CollectedCoinEvent>(OnCoinCollection);
            GameEventManager.RemoveListener<SpentCoinsEvent>(OnCoinDispension);
            
            GameEventManager.RemoveListener<PlayerDamageEvent>(OnDamage);
            GameEventManager.RemoveListener<PlayerHealEvent>(OnHealing);
            
            GameEventManager.RemoveListener<PlayerEquipItemEvent>(EquipItemToHand);
        }

        private void OnCoinCollection(CollectedCoinEvent e)
        {
            AddCoins(e.amount > 0 ? e.amount : -1 * e.amount);
        }

        private void OnCoinDispension(SpentCoinsEvent e)
        {
            AddCoins(e.amount > 0 ? -1 * e.amount : e.amount);
        }

        private void OnDamage(PlayerDamageEvent e)
        {
            if(e.healthPoints > 0)
                TakeDamage(e.healthPoints);
        }

        private void OnHealing(PlayerHealEvent e)
        {
            if(e.healthPoints > 0)
                AddHealth(e.healthPoints);
        }

        public void AddCoins(int amount)
        {
            collectedCoins.Add(amount);
        }

        public void AddHealth(int amount)
        {
            health.Add(amount);
        }

        public void TakeDamage(int amount)
        {
            health.Subtract(amount);
        }
        
        public void SetInitialPosition(Vector3 pos)
        {
            transform.position = pos;
        }
        
        public void SetInitialCoins(int coins)
        {
            collectedCoins.SetValue(coins);
        }

    }
}
