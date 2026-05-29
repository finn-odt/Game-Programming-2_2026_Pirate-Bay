using System;
using System.Collections.Generic;
using GameEvents;
using Player;
using SLTypes;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityServiceLocator;
using Random = UnityEngine.Random;

public class GameObjectFactory : MonoBehaviour
{
    [Flags]
    public enum GameObjectType
    {
        None = 0,
        Sharks = 1 << 0, // 1
        Items  = 1 << 1, // 2
    }
    
    public static GameObjectFactory Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    
    [Header("General")]
    [SerializeField] private float spawnRadius;

    private bool maxNumbersInitialized = false;
    
    [Header("Sharks")]
    [SerializeField] private GameObject parentForSharks;
    [SerializeField] private GameObject sharkPrefab;
    [SerializeField] private int easyNumberOfSharks = 50;
    [SerializeField] private int normalNumberOfSharks = 100;
    [SerializeField] private int hardNumberOfSharks = 150;
    [SerializeField] private bool drawGizmoOnlyOnSelected;
    private int maxNumberOfSharks = 0;
    private int sharkAgentId;
    
    private List<GameObject> sharkAgents = new();
    
    [Header("Items")]
    [SerializeField] private GameObject parentForItems;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private InventoryItemDatabaseSO itemDatabase;
    [SerializeField] private int easyNumberOfItems = 600;
    [SerializeField] private int normalNumberOfItems = 400;
    [SerializeField] private int hardNumberOfItems = 200;
    
    [SerializeField] private float groundRaycastHeight = 50f;
    [SerializeField] private float groundRaycastDistance = 100f;
    [SerializeField] private float itemGroundOffset = 0.05f;
    
    
    [HideInInspector] private IPlayer player;
    
    private int maxNumberOfItems = 0;
    private int humanoidAgentId;  // for placing items on walkable NavMesh
    
    private List<GameObject> placedItems = new();
    
    void Start()
    {
        ServiceLocator.Global.Get(out player);
    }
    
    void OnEnable()
    {
        GameEventManager.AddListener<GameDifficultyChangedEvent>(OnDifficultyChange);
        GameEventManager.AddListener<DestroyItemEvent>(CleanupCollectedItems);

        // get agent ID for sharks (by string name)
        sharkAgentId = GetAgentTypeIdByName("Shark");
        humanoidAgentId = GetAgentTypeIdByName("Humanoid");
    }

    void OnDisable()
    {
        GameEventManager.RemoveListener<GameDifficultyChangedEvent>(OnDifficultyChange);
        GameEventManager.RemoveListener<DestroyItemEvent>(CleanupCollectedItems);
    }

    private void CleanupCollectedItems(DestroyItemEvent e)
    {
        if(placedItems == null)
            return;
        
        // remove picked up item
        placedItems.Remove(e.item);
    }

    private GameDifficulty _gameDifficulty = GameDifficulty.Easy;
    private void OnDifficultyChange(GameDifficultyChangedEvent e)
    {
        if (_gameDifficulty == e.newDifficulty)
            return;
        
        SetMaxValuesForSpawning();
        SpawnGameObjects(GameObjectType.Sharks | GameObjectType.Items);

        _gameDifficulty = e.newDifficulty;
    }

    private void SetMaxValuesForSpawning()
    {
        // set number of sharks/items/... according to difficulty
        switch (GameManager.Instance?.gameDifficulty)
        {
            case GameDifficulty.Easy:
                maxNumberOfSharks = easyNumberOfSharks;
                maxNumberOfItems = easyNumberOfItems;
                break;
            case GameDifficulty.Normal:
                maxNumberOfSharks = normalNumberOfSharks;
                maxNumberOfItems = normalNumberOfItems;
                break;
            case GameDifficulty.Hard:
                maxNumberOfSharks = hardNumberOfSharks;
                maxNumberOfItems = hardNumberOfItems;
                break;
        }

        maxNumbersInitialized = true;
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
        if (!maxNumbersInitialized)
            SetMaxValuesForSpawning();
        
        foreach (GameObjectType type in Enum.GetValues(typeof(GameObjectType)))
        {
            if (type == GameObjectType.None)
                continue;

            if ((whichTypes & type) != 0)
            {
                SpawnGameObjectsByType(type);
            }
        }
    }

    private void SpawnGameObjectsByType(GameObjectType type)
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
    }

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
        switch (type)
        {
            case GameObjectType.Sharks:
            {
                if (sharkAgents == null)
                    return;
        
                for (int i = sharkAgents.Count - 1; i >= 0; i--)
                {
                    Destroy(sharkAgents[i]);
                }
                sharkAgents.Clear();
                break;
            }

            case GameObjectType.Items:
            {
                if (placedItems == null)
                    return;
        
                for (int i = placedItems.Count - 1; i >= 0; i--)
                {
                    Destroy(placedItems[i]);
                }
                placedItems.Clear();
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    
    private int GetAgentTypeIdByName(string agentTypeName)
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
    }

    private void SpawnShark(Vector3 center)
    {
        if (!TryGetRandomSpawnPoint(center, out Vector3 spawnPosition, sharkAgentId))
        {
            Debug.LogWarning("Could not find shark spawn point on Shark NavMesh.");
            return;
        }

        GameObject shark = Instantiate(sharkPrefab, spawnPosition, Quaternion.identity);
        sharkAgents.Add(shark);
        SharkBehaviour sb = shark.GetComponentInChildren<SharkBehaviour>();
        sb.player = player;
        
        shark.transform.parent = parentForSharks.transform;

        NavMeshAgent agent = shark.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(spawnPosition);
        }
    }
    
    private void SpawnItem(Vector3 center)
    {
        const int maxAttempts = 5;
        bool spawnPositionFound = false;
        Vector3 spawnPosition = Vector3.zero;
        for (int i = 0; i < maxAttempts; i++)
        {
            if (!TryGetRandomSpawnPoint(center, out Vector3 navMeshPosition, humanoidAgentId))  // random point in world on NavMesh
                continue;

            if (!TryProjectToGround(navMeshPosition, out spawnPosition))  // project onto ground
                continue;

            spawnPositionFound = true;
            break;
        }
        if (!spawnPositionFound)
        {
            Debug.LogWarning("Could not find item spawn point on NPC NavMesh / ground collider.");
            return;
        }

        GameObject item = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
        placedItems.Add(item);
        
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
        
        item.transform.parent = parentForItems.transform;
        Debug.LogWarning("Spawned Item on NPC NavMesh.");
    }
    
    private bool TryProjectToGround(Vector3 navMeshPosition, out Vector3 groundPosition)
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
    }

    private void OnDrawGizmos()
    {
        if (drawGizmoOnlyOnSelected)
            return;
        
        Gizmos.color = Color.purple;
        Gizmos.DrawSphere(Vector3.zero, spawnRadius);
        Gizmos.DrawWireSphere(Vector3.zero, spawnRadius);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmoOnlyOnSelected)
            return;
        
        Gizmos.color = Color.purple;
        Gizmos.DrawSphere(Vector3.zero, spawnRadius);
        Gizmos.DrawWireSphere(Vector3.zero, spawnRadius);
    }
}
