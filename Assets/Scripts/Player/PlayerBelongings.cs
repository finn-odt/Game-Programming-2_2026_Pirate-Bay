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
            Transform parent = e.leftHand
                ? leftHandEquipParent.transform
                : rightHandEquipParent.transform;

            e.item.transform.SetParent(parent, false);

            e.item.transform.localPosition = Vector3.zero;
            e.item.transform.localEulerAngles = Vector3.zero;  // !e.leftHand ? new Vector3(0, 0, 180f) :

            Debug.LogWarning($"WorldPos: {e.item.transform.position}");
            Debug.LogWarning($"LocalPos: {e.item.transform.localPosition}");
        }
        
        public void StipItemFromHand(PlayerStripItemEvent e)
        {
            GameObject parent = e.leftHand ? leftHandEquipParent : rightHandEquipParent;
            foreach (Transform child in parent.transform)
            {
                Destroy(child.gameObject);
            }
        }

        void OnEnable()
        {
            GameEventManager.AddListener<CollectedCoinEvent>(OnCoinCollection);
            GameEventManager.AddListener<SpentCoinsEvent>(OnCoinDispension);
            
            GameEventManager.AddListener<PlayerDamageEvent>(OnDamage);
            GameEventManager.AddListener<PlayerHealEvent>(OnHealing);
            
            GameEventManager.AddListener<PlayerEquipItemEvent>(EquipItemToHand);
            GameEventManager.AddListener<PlayerStripItemEvent>(StipItemFromHand);
            
            GameEventManager.AddListener<PlayerUseHandRequestEvent>(OnHandUseRequest);
        }

        void OnDisable()
        {
            GameEventManager.RemoveListener<CollectedCoinEvent>(OnCoinCollection);
            GameEventManager.RemoveListener<SpentCoinsEvent>(OnCoinDispension);
            
            GameEventManager.RemoveListener<PlayerDamageEvent>(OnDamage);
            GameEventManager.RemoveListener<PlayerHealEvent>(OnHealing);
            
            GameEventManager.RemoveListener<PlayerEquipItemEvent>(EquipItemToHand);
            GameEventManager.RemoveListener<PlayerStripItemEvent>(StipItemFromHand);
            
            GameEventManager.RemoveListener<PlayerUseHandRequestEvent>(OnHandUseRequest);
        }

        private void OnHandUseRequest(PlayerUseHandRequestEvent e)
        {
            GameObject parent = e.leftHand ? leftHandEquipParent : rightHandEquipParent;
            
            if(parent == null)
                return;
            
            CollectableItem itemScript = parent?.GetComponentInChildren<CollectableItem>();
            
            if(itemScript == null)
                return;
            
            InventoryItemDataSO itemData = itemScript.InventoryItemData;
            Transform position = parent.transform;
            
            UseItem(itemData, position);
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
        
        public void UseItem(InventoryItemDataSO itemData, Transform useOrigin)
        {
            if (itemData == null || !itemData.HasUseBehaviour)
                return;

            ItemUseContext context = new ItemUseContext(
                user: this,  // give this IHuman as reference
                itemData: itemData,
                aimDirection: transform.forward,
                useOrigin: useOrigin
            );

            itemData.Use(context);
        }

    }
}
