using System;
using System.Collections.Generic;
using GameEvents;
using SLTypes;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityServiceLocator;
using Random = UnityEngine.Random;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace ObjectFactory {
    
    [Flags]
    public enum GameObjectType
    {
        None = 0,
        Sharks = 1 << 0, // 1
        Items  = 1 << 1, // 2
    }
    
    [Serializable]
    public class DifficultySizes
    {
        [SerializeField] private int easy = 10;
        [SerializeField] private int normal = 10;
        [SerializeField] private int hard = 10;

        public int this[GameDifficulty difficulty]
        {
            get
            {
                return difficulty switch
                {
                    GameDifficulty.Easy => easy,
                    GameDifficulty.Normal => normal,
                    GameDifficulty.Hard => hard,
                    _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
                };
            }
        }
    }
    
    public class GameObjectFactory : MonoBehaviour
    {
        public static GameObjectFactory Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Initialize Pools
            foreach (ObjectPool pool in generationPools) {
                switch (pool.type)
                {
                    case GameObjectType.Items:
                        pool.ObjectPrepareFunction = PrepareItemAfterSpawn;  // set prepare function
                        break;
                    case GameObjectType.Sharks:
                        pool.ObjectPrepareFunction = PrepareSharkAfterSpawn;  // set prepare function
                        break;
                }
            }
            
        }

        public List<ObjectPool> generationPools;

        private Dictionary<GameObjectType, List<GameObject>> pooledObjects; 
        
        // GameObject objectToPool;
        //public int maxAmountToPool;
        
        //[Header("General")]
        //[SerializeField] private float spawnRadius;
        
        private bool maxNumbersInitialized = false;
        
        [Header("Sharks")]
        //[SerializeField] private GameObject parentForSharks;
        //[SerializeField] private GameObject sharkPrefab;
        //[SerializeField] private int easyNumberOfSharks = 50;
        //[SerializeField] private int normalNumberOfSharks = 100;
        //[SerializeField] private int hardNumberOfSharks = 150;
        //[SerializeField] private bool drawGizmoOnlyOnSelected;
        //private int maxNumberOfSharks = 0;
        //private int sharkAgentId;
        
        [Header("Items")]
        //[SerializeField] private GameObject parentForItems;
        //[SerializeField] private GameObject itemPrefab;
        [SerializeField] private InventoryItemDatabaseSO itemDatabase;
        //[SerializeField] private int easyNumberOfItems = 600;
        //[SerializeField] private int normalNumberOfItems = 400;
        //[SerializeField] private int hardNumberOfItems = 200;
        
        //[SerializeField] private float groundRaycastHeight = 50f;
        //[SerializeField] private float groundRaycastDistance = 100f;
        //[SerializeField] private float itemGroundOffset = 0.05f;
        
        //private int maxNumberOfItems = 0;
        
        [HideInInspector] private IPlayer player;
        
        void Start()
        {
            ServiceLocator.ForSceneOf(this).Get(out player);
            
            // initialize all generation pools
            foreach (var pool in generationPools)
                pool.Initialize();
            
            SpawnGameObjects(GameObjectType.Items | GameObjectType.Sharks);
        }
        
        void OnEnable()
        {
            GameEventManager.AddListener<DestroyItemEvent>(CleanupCollectedItems);

            // get agent ID for sharks (by string name)
            //sharkAgentId = GetAgentTypeIdByName("Shark");
            //humanoidAgentId = GetAgentTypeIdByName("Humanoid");
        }

        void OnDisable()
        {
            GameEventManager.RemoveListener<DestroyItemEvent>(CleanupCollectedItems);
        }
        
        private void OnDestroy()
        {
            // dispose all generation pools
            foreach (var pool in generationPools) {
                pool.Dispose();
            }
            
            if (Instance == this) {
                Instance = null;
            }
        }

        private void CleanupCollectedItems(DestroyItemEvent e)
        {
            if(pooledObjects == null)
                return;

            // remove picked up item
            pooledObjects[GameObjectType.Items].Remove(e.item);
        }

        /// <summary>
        /// Spawns the selected game object types.
        /// </summary>
        /// <param name="whichTypes">
        /// Flag mask that determines which object types should be spawned.
        /// Multiple values can be combined with the bitwise OR (|) operator.
        /// </param>
        public void SpawnGameObjects(GameObjectType whichTypes)
        {
            foreach (GameObjectType type in Enum.GetValues(typeof(GameObjectType)))
            {
                if (type == GameObjectType.None)
                    continue;

                if ((whichTypes & type) != 0)
                {
                    //SpawnGameObjectsByType(type);
                    
                    // spawn via pool
                    
                    ObjectPool currPool = generationPools.Find(x => x.type == type);
                    if (currPool == null)
                        return;

                    while(currPool.CanSpawn)
                        currPool.SpawnRandom();
                }
            }
        }

        /*private void SpawnGameObjectsByType(GameObjectType type)
        {
            switch (type)
            {
                case GameObjectType.Sharks:
                {
                    Spawn(sharkAgents, maxNumberOfSharks, SpawnShark);
                    break;
                }
                case GameObjectType.Items:
                {
                    Spawn(placedItems, maxNumberOfItems, SpawnItem);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private void Spawn(List<GameObject> list, int maxAmount, Action<Vector3> SpawnFunction)
        {
            Vector3 center = Vector3.zero;
            
            if (list == null)
                list = new List<GameObject>();
            
            list.RemoveAll(obj => obj == null);

            // should items be destroyed?
            if (list.Count >= maxAmount)
            {
                while (list.Count >= maxAmount)
                {
                    Destroy(list[list.Count - 1]);
                    list.RemoveAt(list.Count - 1);
                }
            }
                    
            // subtract number of already active objects (recycling)
            int endValue = maxAmount - list.Count;
            for (int i = 0; i < endValue; i++)
                SpawnFunction(center);
        }*/

        public void ClearGameObjects(GameObjectType whichTypes)
        {
            foreach (GameObjectType type in Enum.GetValues(typeof(GameObjectType)))
            {
                if (type == GameObjectType.None)
                    continue;

                if ((whichTypes & type) != 0)
                {
                    ClearObjectListByType(type);
                }
            }
        }
        
        private void ClearObjectListByType(GameObjectType type)
        {
            if (!pooledObjects.TryGetValue(type, out List<GameObject> list))
                return;
            if (list == null)
                return;
            
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Destroy(list[i]);
            }
            pooledObjects[type].Clear();
        }
        
        /*private int GetAgentTypeIdByName(string agentTypeName)
        {
            for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
            {
                NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(i);
                string name = NavMesh.GetSettingsNameFromID(settings.agentTypeID);

                if (name == agentTypeName)
                    return settings.agentTypeID;
            }

            Debug.LogError($"No NavMesh agent type found with name '{agentTypeName}'.");
            return -1;
        }
        
        private bool TryGetRandomSpawnPoint(Vector3 center, out Vector3 result, int agentID)
        {
            NavMeshQueryFilter filter = new NavMeshQueryFilter
            {
                agentTypeID = agentID,
                areaMask = NavMesh.AllAreas
            };

            int maxAttempts = 5;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;

                Vector3 randomPoint = center + new Vector3(
                    randomCircle.x,
                    0f,
                    randomCircle.y
                );

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, filter))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = center;
            return false;
        }*/

        private void SpawnShark(Vector3 center)
        {
            generationPools[1].ObjectPrepareFunction = PrepareSharkAfterSpawn;
            // set 
            GameObject shark = generationPools[1].SpawnRandom();


        }

        private void PrepareSharkAfterSpawn(GameObject shark)
        {
            // TODO: anpassen
            /*NavMeshAgent agent = shark.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(spawnPosition);
            }*/
        }
        
        private void SpawnItem(Vector3 center)
        {
            // TODO: not 0, but some complex calulcation ;)
            generationPools[0].ObjectPrepareFunction = PrepareItemAfterSpawn;  // set prepare function
            // set projectSpawnedObjectsOntoNavMesh = true
            GameObject item = generationPools[0].SpawnRandom();
            
            Debug.LogWarning("Spawned Item on NPC NavMesh.");
        }

        private void PrepareItemAfterSpawn(GameObject item)
        {
            // activate runtime data and behaviour
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.isKinematic = true;
            }

            CollectableItem itemScript = item.GetComponentInChildren<CollectableItem>();
            itemScript.InventoryItemData = itemDatabase.GetRandomItem();
            itemScript.amount = Random.Range(0f, 1f) < 0.1f ? Random.Range(3, 7) : Random.Range(1, 3);  // 10%: 3-6, 90%: 1 or 2
            itemScript.showParticleEffect = true;
        }
        
        /*private bool TryProjectToGround(Vector3 navMeshPosition, out Vector3 groundPosition)
        {
            Vector3 rayOrigin = navMeshPosition + Vector3.up * groundRaycastHeight;

            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    groundRaycastDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                groundPosition = hit.point + Vector3.up * itemGroundOffset;
                return true;
            }

            groundPosition = navMeshPosition;
            return false;
        }*/

        /*private void OnDrawGizmos()
        {
            if (generationPools == null)
                return;

            for (int i = 0; i < generationPools.Count; i++)
            {
                ObjectPool pool = generationPools[i];

                if (pool == null)
                    continue;

                float hue = i / (float)generationPools.Count;

                Color col = Color.HSVToRGB(hue, 0.8f, 1f);
                col.a = 0.5f;

                Gizmos.color = col;

                Gizmos.DrawWireSphere(Vector3.zero, pool.SpawnRadius);
                Gizmos.DrawSphere(Vector3.zero, pool.SpawnRadius);
            }
        }*/
        
        private void OnDrawGizmosSelected()
        {
            if (generationPools == null || generationPools.Count == 0)
                return;

            int segmentCount = generationPools.Count;
            float degreesPerSegment = 360f / segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                ObjectPool pool = generationPools[i];

                if (pool == null)
                    continue;

                float hue = i / (float)segmentCount;

                Color col = Color.HSVToRGB(hue, 0.8f, 1f);
                col.a = 0.8f;

                Gizmos.color = col;

                float startAngle = i * degreesPerSegment + 45;
                float endAngle = startAngle + degreesPerSegment;

                DrawCircleSegment(
                    Vector3.zero,
                    pool.SpawnRadius,
                    startAngle,
                    endAngle,
                    32
                );
            }
        }
        
        private void DrawCircleSegment(
            Vector3 center,
            float radius,
            float startAngleDegrees,
            float endAngleDegrees,
            int resolution = 32,
            bool drawRadialLines = true)
        {
            Vector3 startPoint = center + AngleToXZDirection(startAngleDegrees) * radius;
            Vector3 endPoint = center + AngleToXZDirection(endAngleDegrees) * radius;

            if (drawRadialLines)
            {
                Gizmos.DrawLine(center, startPoint);
                Gizmos.DrawLine(center, endPoint);
            }

            float angleStep = (endAngleDegrees - startAngleDegrees) / resolution;

            Vector3 previousPoint = startPoint;

            for (int i = 1; i <= resolution; i++)
            {
                float angle = startAngleDegrees + angleStep * i;
                Vector3 nextPoint = center + AngleToXZDirection(angle) * radius;

                Gizmos.DrawLine(previousPoint, nextPoint);
                Gizmos.DrawLine(center, nextPoint);

                previousPoint = nextPoint;
            }
        }

        private Vector3 AngleToXZDirection(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;

            return new Vector3(
                Mathf.Cos(radians),
                0f,
                Mathf.Sin(radians)
            );
        }
    }
}
