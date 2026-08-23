using System;
using System.Collections.Generic;
using Configurations;
using GameEvents;
using Opsive.UltimateCharacterController.AddOns.Climbing;
using Opsive.UltimateCharacterController.AddOns.Swimming;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.Traits;
using Attribute = Opsive.UltimateCharacterController.Traits.Attribute;
using EventHandler = Opsive.Shared.Events.EventHandler;
using Player;
using SLTypes;
using UnityConstantsGenerator;
using UnityEngine;
using UnityServiceLocator;

namespace Player
{
    public class PlayerBelongings : MonoBehaviour, IPlayer
    {
        [SerializeField] private IntegerSO collectedCoins;
        [SerializeField] private IntegerSO health;
        private Health _opsiveHealth;
        private UltimateCharacterLocomotion characterLocomotion;
        [SerializeField] private float playerHeight;
        [SerializeField] private GameObject leftHandEquipParent, rightHandEquipParent;
        [SerializeField] private Transform dropOrigin;
        
        [Header("Debug")]
        [SerializeField] private bool useSavedPlayerHealth = false;

        public Transform Transform => transform;
        public Vector3 Position => transform.position;
        public int CollectedCoins => collectedCoins.RuntimeValue;
        public int Health => health.RuntimeValue;
        public float HealthPercentage => health.RuntimeValue / (float)health.InitialValue;
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

        public Transform DropOrigin
        {
            get => dropOrigin;
        }

        public List<GameObject> CurrentGrounds { get; set; } = new();
        
        [SerializeField] private AttributeManager attributeManager;
        private Attribute healthAttribute;

        private void Awake()
        {
            // register (this) player
            ServiceLocator.ForSceneOf(this).Register<IPlayer>(this);
            
            // UCC Locomotion for Initial Positioning
            characterLocomotion = GetComponent<UltimateCharacterLocomotion>();
            
            // initialize attribute of character controller
            if (attributeManager == null)
                attributeManager = GetComponent<AttributeManager>();
        
            healthAttribute = attributeManager.GetAttribute("Health");

            if (healthAttribute == null)
            {
                Debug.LogError("Could not find Breath attribute on AttributeManager.");
            }
            
            // restore health
            if (useSavedPlayerHealth)
            {
                health.SetValue(GameConfiguration.Data.health);
            }
            else
            {
                // reset value every time to init-value
                health.SetValue(health.InitialValue);
                GameConfiguration.SaveHealthPoints(Health);
            }
            
            _opsiveHealth = GetComponent<Health>();
        }

        private void OnDestroy()
        {
            // Added this method manually to the ServiceLocator, to unregister on Scene Reload
            ServiceLocator.ForSceneOf(this).Unregister<IPlayer>(this);
        }

        private void Start()
        {
            Debug.Log($"Health Display: {health.RuntimeValue}");
            UIManager.Instance.DisplayHealth(health.RuntimeValue);
        }

        private void Update()
        {
            //GameEventManager.Raise(new UpdateHealthUIEvent((int)GetHealth_UCC()));
        }

        public void EquipItemToHand(PlayerEquipItemEvent e)
        {
            Transform parent = e.leftHand
                ? leftHandEquipParent.transform
                : rightHandEquipParent.transform;

            e.item.transform.SetParent(parent, false);

            // set layer to 'Player' -> no camera occlusion
            SetLayerRecursively(e.item.gameObject, (int)LayerId.Player);

            e.item.transform.localPosition = Vector3.zero;
            e.item.transform.localEulerAngles = Vector3.zero;  // !e.leftHand ? new Vector3(0, 0, 180f) :
        }
        
        private static void SetLayerRecursively(GameObject parent, int layer, bool includeParent = true)
        {
            if (parent == null)
                return;

            parent.layer = layer;  
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }
        
        /// <summary>
        /// This method removes the item in the specified hand
        /// by either setting parent = null or calling Destroy()
        /// on the Game Object.
        /// </summary>
        /// <param name="e">Event Data</param>
        /// <param name="doNotDestroy">[default: false] Deparent the item, but do not call Destroy() on it</param>
        public void StripItemFromHand(PlayerStripItemEvent e)
        {
            GameObject parent = e.leftHand ? leftHandEquipParent : rightHandEquipParent;
            foreach (Transform child in parent.transform)
            {
                // restore correct layer
                SetLayerRecursively(child.gameObject, (int)LayerId.Items);
                
                if (e.doNotDestroy)
                    child.parent = null;
                else
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
            GameEventManager.AddListener<PlayerStripItemEvent>(StripItemFromHand);
            
            GameEventManager.AddListener<PlayerUseHandRequestEvent>(OnHandUseRequest);
            
        }

        void OnDisable()
        {
            GameEventManager.RemoveListener<CollectedCoinEvent>(OnCoinCollection);
            GameEventManager.RemoveListener<SpentCoinsEvent>(OnCoinDispension);
            
            GameEventManager.RemoveListener<PlayerDamageEvent>(OnDamage);
            GameEventManager.RemoveListener<PlayerHealEvent>(OnHealing);
            
            GameEventManager.RemoveListener<PlayerEquipItemEvent>(EquipItemToHand);
            GameEventManager.RemoveListener<PlayerStripItemEvent>(StripItemFromHand);
            
            GameEventManager.RemoveListener<PlayerUseHandRequestEvent>(OnHandUseRequest);
            
        }
        
        public void OnCharacterAbilityActive(Ability ability, bool active)
        {
            if (!active)
                return;
            
            Debug.Log($"Ability Active: {ability.GetType()}");

            if (ability is Drown)
            {
                GameEventManager.Raise(
                    new GameOverEvent(
                        transform.position,
                        GameOverEvent.Killer.Drowned
                    )
                );
            }
            else if (ability is Die)
            {
                GameOverEvent.Killer killer;

                if (pendingDamageSource.HasValue)
                    killer = ConvertToKiller(pendingDamageSource.Value);
                else
                    killer = GameOverEvent.Killer.Falling;

                GameEventManager.Raise(
                    new GameOverEvent(
                        transform.position,
                        killer
                    )
                );
            }
        }
        
        private GameOverEvent.Killer ConvertToKiller(PlayerDamageEvent.DamagedBy damagedBy)
        {
            return damagedBy switch
            {
                PlayerDamageEvent.DamagedBy.NPC => GameOverEvent.Killer.Npc,
                PlayerDamageEvent.DamagedBy.Shark => GameOverEvent.Killer.Shark,
                PlayerDamageEvent.DamagedBy.Explosion => GameOverEvent.Killer.Explosion,
                PlayerDamageEvent.DamagedBy.Water => GameOverEvent.Killer.Drowned,
                _ => GameOverEvent.Killer.Unknown
            };
        }

        private void OnHandUseRequest(PlayerUseHandRequestEvent e)
        {
            GameObject parent = e.leftHand ? leftHandEquipParent : rightHandEquipParent;

            if (parent == null || parent.transform.childCount == 0)
            {
                Debug.LogWarning("Currently no usable item in player's hand");
                return;
            }

            CollectableItem itemScript = parent?.GetComponentInChildren<CollectableItem>();

            if (itemScript == null)
            {
                Debug.LogWarning("Currently no CollectableItem in player's hand");
                return;
            }

            ItemUseBehaviourSO useBehaviour = itemScript.runtimeUseBehaviour;
            InventoryItemDataSO itemData = itemScript.InventoryItemData;
            Transform position = parent.transform;
            
            UseItem(useBehaviour, e.leftHand, itemData, position);
        }

        private void OnCoinCollection(CollectedCoinEvent e)
        {
            AddCoins(e.amount > 0 ? e.amount : -1 * e.amount);
        }

        private void OnCoinDispension(SpentCoinsEvent e)
        {
            AddCoins(e.amount > 0 ? -1 * e.amount : e.amount);
        }

        
        private PlayerDamageEvent.DamagedBy? pendingDamageSource;
        private void OnDamage(PlayerDamageEvent e)
        {
            if (e.healthPoints <= 0)
                return;
            
            pendingDamageSource = e.damagedBy;
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
            // update ScriptableObject-Data & save new value
            int newHealth = health.Add(amount);
            
            // set health in Ultimate Character Controller
            _opsiveHealth.Heal(amount);
            
            // _opsiveHealth.ImmediateDeath();
            
            // set health in UI
            UIManager.Instance.DisplayHealth(HealthPercentage);
        }

        public void TakeDamage(int amount)
        {
            // update ScriptableObject-Data & save new value
            int newHealth = health.Subtract(amount);
            
            
            // set health in Ultimate Character Controller
            _opsiveHealth.Damage(amount);
            
            // set health in UI
            UIManager.Instance.DisplayHealth(HealthPercentage);
        }
        
        public void SetInitialPosition(Vector3 pos, Quaternion rotation)
        {
            characterLocomotion.SetPositionAndRotation(pos, rotation);
        }
        
        public void SetInitialCoins(int coins)
        {
            collectedCoins.SetValue(coins);
        }
        
        public void UseItem(ItemUseBehaviourSO useBehaviour, bool leftHand, InventoryItemDataSO itemData, Transform useOrigin)
        {
            if (itemData == null || !itemData.HasUseBehaviour)
            {
                Debug.LogWarning("Current held item has no behaviour");
                return;
            }

            ItemUseContext context = new ItemUseContext(
                user: this,  // give this IHuman as reference
                bodyside: (leftHand ? ItemUseContext.BodySide.Left : ItemUseContext.BodySide.Right),
                itemData: itemData,
                aimDirection: transform.forward + new Vector3(0, 0.5f, 0),
                useOrigin: useOrigin
            );

            useBehaviour.Use(context);
        }

    }
}
