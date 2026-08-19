using System;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using TriInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace ObjectFactory
{
    [Serializable]
    public class ObjectPool
    {

        public GameObjectType type;
        [SerializeField] private Transform parent;
        [SerializeField] private GameObject prefab;
        [SerializeField] private DifficultySizes maxAmounts;
        [SerializeField] private float spawnRadius = 340f;

        private GameObject inactivePrefabTemplate;

        private bool objectsSpawned = false;
        
        public float SpawnRadius => spawnRadius;
        
        // for placing objects on walkable NavMesh
        [SerializeField, LabelText("NavMesh Agent Type"), Dropdown(nameof(GetNavMeshAgentTypes))]
        private int navMeshAgentTypeId;
        
        private TriDropdownList<int> GetNavMeshAgentTypes()
        {
            return NavMeshAgentTypeDropdown.GetAgentTypes();
        }
        
        public Action<GameObject> ObjectPrepareFunction;  // is set by GameObjectFactory
        
        [SerializeField] private bool projectObjectsOntoNavMesh = false;
        [SerializeField, ShowIf(nameof(projectObjectsOntoNavMesh))] private float groundRaycastHeight = 50f;
        [SerializeField, ShowIf(nameof(projectObjectsOntoNavMesh))] private float groundRaycastDistance = 100f;
        [SerializeField, ShowIf(nameof(projectObjectsOntoNavMesh))] private float groundPlacementOffset = 0.05f;

        private readonly Queue<GameObject> availableObjects = new();
        private readonly List<GameObject> activeObjects = new();

        private GameDifficulty CurrDifficulty = GameDifficulty.Easy;
        
        private int MaxAmount => maxAmounts[CurrDifficulty];  // get max amount always depended on current game difficulty
        public int ActiveCount => activeObjects.Count;
        public int AvailableCount => availableObjects.Count;
        public bool CanSpawn => ActiveCount < MaxAmount;
        
        public void Initialize()
        {
            GameEventManager.AddListener<GameDifficultyChangedEvent>(OnDifficultyChange);

            // if OnDifficultyChange was never called, we do its job manually
            if (GameManager.Instance.gameDifficulty != CurrDifficulty)
                CurrDifficulty = GameManager.Instance.gameDifficulty;
            
            CreateInactivePrefabTemplate();
            Setup();
        }

        public void Setup()
        {
            availableObjects.Clear();
            activeObjects.Clear();

            Vector3 initialPosition = Vector3.zero;

            if (!TryGetRandomSpawnPoint(Vector3.zero, out initialPosition, false))
            {
                Debug.LogWarning($"Could not find NavMesh position for pool {type}. Using fallback position.");
            }

            for (int i = 0; i < MaxAmount; i++)
            {
                GameObject obj = UnityEngine.Object.Instantiate(
                    inactivePrefabTemplate,
                    initialPosition,
                    Quaternion.identity,
                    parent
                );

                obj.SetActive(false);
                availableObjects.Enqueue(obj);
            }
        }
        
        public void Dispose()
        {
            GameEventManager.RemoveListener<GameDifficultyChangedEvent>(OnDifficultyChange);

            // cleanup scene references
            for(int i = 0; i < activeObjects.Count; i++) {
                if (activeObjects[i] != null)
                    UnityEngine.Object.Destroy(activeObjects[i]);
                
                activeObjects[i] = null;
            }
            activeObjects.Clear();
            
            while (availableObjects.Count > 0) {
                var obj = availableObjects.Dequeue();
                if (obj != null) {
                    UnityEngine.Object.Destroy(obj);
                }
            }
            availableObjects.Clear();

            if (inactivePrefabTemplate != null) {
                UnityEngine.Object.Destroy(inactivePrefabTemplate);
                inactivePrefabTemplate = null;
            }

            objectsSpawned = false;
            ObjectPrepareFunction = null;
        }
        
        private void CreateInactivePrefabTemplate()
        {
            if (inactivePrefabTemplate != null)
                return;

            inactivePrefabTemplate = UnityEngine.Object.Instantiate(prefab);
            inactivePrefabTemplate.name = $"{prefab.name}_InactiveTemplate";
            inactivePrefabTemplate.SetActive(false);

            if (parent != null)
                inactivePrefabTemplate.transform.SetParent(parent, false);
        }
        
        private void OnDifficultyChange(GameDifficultyChangedEvent e)
        {
            if (CurrDifficulty == e.newDifficulty)
                return;

            CurrDifficulty = e.newDifficulty;
            EnsureCapacity();
        }

        private void EnsureCapacity()
        {
            if (!objectsSpawned)
                return;

            if (ActiveCount < MaxAmount)
            {
                while (ActiveCount < MaxAmount)
                {
                    SpawnRandom();
                }
            } else if (ActiveCount > MaxAmount)
            {

                // sort by distance from origin (despawn high origin-distance objects)
                GameObject[] tempArr = new GameObject[ActiveCount];
                activeObjects.CopyTo(tempArr);
                List<GameObject> sortedByOriginDistance =
                    tempArr.OrderByDescending(obj => obj.transform.position.sqrMagnitude).ToList();

                int i = 0;
                while (ActiveCount > MaxAmount)
                {
                    Despawn(sortedByOriginDistance[i]);
                    // despawning should not have any effect on tempArr
                }
            }
            // should end with ActiveCount == MaxAmount
        }
        
        /// <summary>
        /// This function spawns an instance of the class-attribute
        /// <i>prefab</i> at a random position on the given NavMesh
        /// and attached to the given parent.
        /// (rotation is <i>Quaternion.identity</i>)
        /// 
        /// After initializing the GameObject, the
        /// <i>ObjectPrepareFunction</i> is called with the
        /// instantiated Object.
        /// </summary>
        /// <param name="position">Position of the instantiated prefab-GameObject</param>
        /// <returns>Returns the instantiated GameObject</returns>
        public GameObject SpawnRandom()
        {
            if(TryGetRandomSpawnPoint(Vector3.zero, out Vector3 spawnPostion, projectObjectsOntoNavMesh))
                return Spawn(spawnPostion, Quaternion.identity);
            
            return null;
        }

        /// <summary>
        /// This function spawns an instance of the class-attribute
        /// <i>prefab</i> at the given position and
        /// attached to the given parent.
        /// (rotation is <i>Quaternion.identity</i>)
        /// 
        /// After initializing the GameObject, the
        /// <i>ObjectPrepareFunction</i> is called with the
        /// instantiated Object.
        /// </summary>
        /// <param name="position">Position of the instantiated prefab-GameObject</param>
        /// <returns>Returns the instantiated GameObject</returns>
        public GameObject Spawn(Vector3 position)
        {
            objectsSpawned = true;
            return Spawn(position, Quaternion.identity);
        }

        /// <summary>
        /// This function spawns an instance of the class-attribute
        /// <i>prefab</i> at the given position, rotation and
        /// attached to the given parent.
        /// 
        /// After initializing the GameObject, the
        /// <i>ObjectPrepareFunction</i> is called with the
        /// instantiated Object.
        /// </summary>
        /// <param name="position">Position of the instantiated prefab-GameObject</param>
        /// <param name="rotation">Rotation of the instantiated prefab-GameObject</param>
        /// <returns>the instantiated GameObject</returns>
        public GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            if (!CanSpawn)
                return null;

            GameObject obj;  // object that is spawned

            if (AvailableCount > 0)
            {
                obj = availableObjects.Dequeue();  // recycle inactive game object from before
            }
            else
            {
                obj = UnityEngine.Object.Instantiate(inactivePrefabTemplate, parent);
            }

            obj.transform.SetPositionAndRotation(position, rotation);  // set initial transforms
            obj.transform.SetParent(parent, true);  // set new parent
            obj.SetActive(true);
            activeObjects.Add(obj);
            
            // prepare object (e.g. setting item-data, runtime configs or whatever)
            ObjectPrepareFunction?.Invoke(obj);
            
            objectsSpawned = true;

            return obj;
        }
        
        private bool TryGetRandomSpawnPoint(Vector3 center, out Vector3 result, bool projectOntoGround=false)
        {
            result = center;  // default return value
            
            NavMeshQueryFilter filter = new NavMeshQueryFilter
            {
                agentTypeID = navMeshAgentTypeId,
                areaMask = NavMesh.AllAreas
            };

            bool posFound = false;

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
                    result = hit.position;  // save navmesh-position
                    posFound = true;
                    break;  // no need to continue the search
                }
            }

            if (projectOntoGround && posFound)
            {
                if (!TryProjectToGround(result, out result))
                {
                    posFound = false;  // projection was a failure
                    result = center;  // reset result, as navMeshPosition from for-loop could have the wrong y-position
                }
                // else: posFound stays true
            }
            
            return posFound;
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
                groundPosition = hit.point + Vector3.up * groundPlacementOffset;
                return true;
            }

            groundPosition = navMeshPosition;
            return false;
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!activeObjects.Remove(obj))  // try to remove from active queue
                return;

            obj.SetActive(false);  // set inactive
            obj.transform.SetParent(parent);  // deparent and parent to this.parent

            availableObjects.Enqueue(obj);  // put inactive element into queue for respawning
        }
        
        /*
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
         */
    }
}