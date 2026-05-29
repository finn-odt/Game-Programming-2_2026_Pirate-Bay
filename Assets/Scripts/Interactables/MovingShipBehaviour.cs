using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using TriInspector;
using UnityConstantsGenerator;
using UnityEngine;
using UnityEngine.AI;

public class MovingShipBehaviour : MonoBehaviour
{
    /*
    [SerializeField] private float speed = 2f;
    [InfoBox("turns with X degrees per second")] [SerializeField, LabelText("Turn Speed")] private float turnSpeed = 20f;
    private float currentYaw;
    [SerializeField] private float targetTriggerDistance = 5f;
    [SerializeField] private Transform[] targets;
    private int targetIndex = 0;
    */

    
    private bool playerOnBoard = false;
    [HideInInspector] public bool sailingActive = false, reachedDestination = false;

    private NavMeshAgent agent;
    private List<Transform> waypoints = new();
    [SerializeField, LabelText("Tag of waypoints for this Boat (Parent Object)")] private string waypointTag;
    private int currentWaypoint = 0;
    private bool waypointIdxRaising = true;
    
    protected bool isGamePaused = false;

    void Start()
    {
        //currentYaw = transform.rotation.eulerAngles.y;

        // get navmesh agent
        agent = GetComponentInChildren<NavMeshAgent>();

        // get all waypoints by tag
        GameObject[] waypointParent = GameObject.FindGameObjectsWithTag(waypointTag);
        waypoints = new List<Transform>();
        if (waypointParent == null || waypointParent.Length == 0 || waypointParent.Length > 1)
        {
            Debug.LogWarning($"No or multiple waypoint parent found with tag '{waypointTag}' for Moving Boat called '{gameObject.name}'");
            return;
        }
        foreach (Transform child in waypointParent[0].transform)
        {
            waypoints.Add(child);
        }
    }
    
    private void OnEnable()
    {
        GameEventManager.AddListener<GameStateChangedEvent>(OnGameStateChange);
    }

    private void OnDisable()
    {
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
    }

    private void OnGameStateChange(GameStateChangedEvent e)
    {
        isGamePaused = e.newState == GameStateChangedEvent.GameState.Paused;  // set new value
    }

    // Update is called once per frame
    void Update()
    {
        if (isGamePaused)
            return;
        
        if (!AgentReady())
        {
            Debug.Log($"NavMeshAgent not ready of moving ship '{gameObject.name}'");
            return;
        }

        // drive to end when player is not on board anymore, his loss
        if (!sailingActive || reachedDestination || waypoints == null || waypoints.Count == 0)
        {
            if (agent.hasPath || agent.pathPending)
            {
                agent.ResetPath();
            }
            return;
        }

        if (HasReachedDestination())
        {
            NextTarget();
        }
    }
    
    private bool AgentReady()
    {
        return agent != null &&  agent.isActiveAndEnabled && agent.isOnNavMesh;
    }
    
    private bool HasReachedDestination()
    {
        if (agent.pathPending)
            return false;

        if (agent.remainingDistance > agent.stoppingDistance)
            return false;

        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f)
            return false;

        return true;
    }

    private void NextTarget()
    {
        if (isGamePaused || !sailingActive || reachedDestination)
            return;

        if (waypointIdxRaising && currentWaypoint == waypoints.Count - 1  // reached end of waypoints
            || !waypointIdxRaising && currentWaypoint == 0)  // reached end of waypoints (while going backwards)
        {
            waypointIdxRaising = !waypointIdxRaising;
            reachedDestination = true;
            return;
        }
        currentWaypoint += waypointIdxRaising ? 1 : -1;

        // set new destination
        agent.SetDestination(waypoints[currentWaypoint].position);
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer != (int)LayerId.Player)
            return;

        Debug.Log("Player now on Board!");
        playerOnBoard = true;
    }

    void OnTriggerStay(Collider other)
    {
        if(other.gameObject.layer != (int)LayerId.Player)
            return;
        
        playerOnBoard = true;  
    }

    void OnTriggerExit(Collider other)
    {
        if(other.gameObject.layer != (int)LayerId.Player)
            return;

        playerOnBoard = false;
    }
}



/*
if(!playerOnBoard || targets.Length == 0 || isDeactivated)
    return;

Vector3 shipPos = transform.position;
Vector3 targetPos = targets[targetIndex].position;
targetPos.y = shipPos.y;  // no vertical movement

// is current target reached?
if(Vector3.Distance(targetPos, shipPos) < targetTriggerDistance) {
    targetIndex = UpdateIndex(targetIndex, targetIdxRaising);
    return;
}

Vector3 dir = targetPos - shipPos;  // Direction(A to B) = B - A
dir.Normalize();

// ROTATION
float targetYaw = Quaternion.LookRotation(dir).eulerAngles.y;

// Signed shortest difference, always between -180 and +180
float delta = Mathf.DeltaAngle(currentYaw, targetYaw);

// rotate boat, when target is not in look direction
if(Math.Abs(delta) > 0.1f) {
    currentYaw = Mathf.MoveTowardsAngle(
        currentYaw,
        targetYaw,
        turnSpeed * Time.deltaTime
    );
    transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
}

float currSpeed = speed;
// move slower, when rotation
currSpeed *= (1 - (Math.Abs(delta) / 90f));
currSpeed = currSpeed < 0 ? 0 : currSpeed;  // no negative movement

// MOVEMENT — no overshooting
transform.position = Vector3.MoveTowards(
    transform.position,
    targetPos,
    currSpeed * Time.deltaTime
);
*/