using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.Game;
using SLTypes;
using TriInspector;
using UnityConstantsGenerator;
using UnityEngine;
using UnityEngine.AI;
using UnityServiceLocator;

[RequireComponent(typeof(KinematicObject))]
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

    private readonly HashSet<Collider> playerCollidersOnBoard = new();
    private UltimateCharacterLocomotion playerLocomotion;
    
    private bool playerOnBoard = false;
    [HideInInspector] public bool sailingActive = false, reachedDestination = false;

    [SerializeField] private BoatNavMeshAgentHandler navAgentHandler;
    [SerializeField] private float rotationSpeed = 180f;
    
    private List<Transform> waypoints = new();
    [SerializeField, LabelText("Tag of waypoints for this Boat (Parent Object)")] private string waypointTag;
    private int currentWaypoint = 0;
    private bool waypointIdxRaising = true;
    private bool destinationSet;
    
    protected bool isGamePaused = false;
    
    private Transform currentMovingPlatform;

    private KinematicObject _kinematicObject;

    private void Awake()
    {
        _kinematicObject = GetComponent<KinematicObject>();
        
        if (navAgentHandler == null) {
            Debug.LogError($"No BoatNavMeshAgentHandler assigned for '{gameObject.name}'.");
            enabled = false;
            return;
        }

        navAgentHandler.Warp(transform.position);
    }

    void Start()
    {
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
        ClearMovingPlatform();
    }

    private void OnDestroy()
    {
        ClearMovingPlatform();
    }

    private void OnGameStateChange(GameStateChangedEvent e)
    {
        isGamePaused = e.newState == GameStateChangedEvent.GameState.Paused;  // set new value
    }

    private void FixedUpdate()
    {
        if (isGamePaused)
            return;

        if (!sailingActive || reachedDestination)
        {
            destinationSet = false;
            return;
        }

        if (navAgentHandler == null || !navAgentHandler.IsReady)
            return;

        if (waypoints == null || waypoints.Count == 0)
            return;

        if (!destinationSet)
        {
            destinationSet = navAgentHandler.SetDestination(waypoints[currentWaypoint].position);

            if (!destinationSet)
            {
                Debug.LogWarning("Initial boat destination couldn't be registered on NavMeshAgent.");
                return;
            }

            Debug.Log($"Initial boat destination set: {waypoints[currentWaypoint].name}");
        }
        
        /*
        Debug.Log(
            $"NAV DEBUG | " +
            $"agentPos={navAgentHandler.transform.position}, " +
            $"boatPos={transform.position}, " +
            $"isReady={navAgentHandler.IsReady}, " +
            $"hasPath={navAgentHandler.Agent.hasPath}, " +
            $"pathPending={navAgentHandler.Agent.pathPending}, " +
            $"pathStatus={navAgentHandler.Agent.pathStatus}, " +
            $"isStopped={navAgentHandler.Agent.isStopped}, " +
            $"speed={navAgentHandler.Agent.speed}, " +
            $"acceleration={navAgentHandler.Agent.acceleration}, " +
            $"velocity={navAgentHandler.Agent.velocity}, " +
            $"desiredVelocity={navAgentHandler.Agent.desiredVelocity}, " +
            $"remainingDistance={navAgentHandler.Agent.remainingDistance}, " +
            $"stoppingDistance={navAgentHandler.Agent.stoppingDistance}, " +
            $"updatePosition={navAgentHandler.Agent.updatePosition}, " +
            $"updateRotation={navAgentHandler.Agent.updateRotation}"
        );
        */

        Vector3 delta = navAgentHandler.ConsumeDelta();
        delta.y = 0f;  // no vertical movement

        transform.position += delta;

        Vector3 desiredVelocity = navAgentHandler.DesiredVelocity;
        desiredVelocity.y = 0f;

        if (desiredVelocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredVelocity.normalized, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        if (navAgentHandler.HasReachedDestination())
        {
            NextTarget();
        }
    }

    private void NextTarget()
    {
        if (isGamePaused || !sailingActive || reachedDestination || waypoints == null || waypoints.Count == 0)
            return;

        if ((waypointIdxRaising && currentWaypoint == waypoints.Count - 1) ||
            (!waypointIdxRaising && currentWaypoint == 0))
        {
            waypointIdxRaising = !waypointIdxRaising;
            reachedDestination = true;
            destinationSet = false;
            return;
        }

        currentWaypoint += waypointIdxRaising ? 1 : -1;

        destinationSet = navAgentHandler.SetDestination(waypoints[currentWaypoint].position);

        if (!destinationSet)
        {
            Debug.LogWarning("Destination couldn't be registered on NavMeshAgent in MovingShipBehaviour");
        }
    }
    
    public string GetHierarchyPath(GameObject obj)
    {
        if (obj == null)
            return string.Empty;

        Transform current = obj.transform;
        string path = current.name;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != (int)LayerId.Player)
            return;

        var locomotion = other.GetComponentInParent<UltimateCharacterLocomotion>();
        if (locomotion == null)
            return;

        playerCollidersOnBoard.Add(other);
        playerLocomotion = locomotion;
        playerOnBoard = true;

        if (!playerLocomotion.SetMovingPlatform(transform))
            Debug.LogWarning($"Moving Platform could not be set: {GetHierarchyPath(gameObject)}");

        Debug.Log("Player now on Board!");
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer != (int)LayerId.Player)
            return;

        var locomotion = other.GetComponentInParent<UltimateCharacterLocomotion>();
        if (locomotion == null)
            return;

        playerCollidersOnBoard.Add(other);
        playerLocomotion = locomotion;
        playerOnBoard = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != (int)LayerId.Player)
            return;

        playerCollidersOnBoard.Remove(other);

        if (playerCollidersOnBoard.Count > 0)
            return;

        playerOnBoard = false;

        if (playerLocomotion != null) {
            if (!playerLocomotion.SetMovingPlatform(null))
                Debug.LogWarning($"Moving Platform could not be set to null: {GetHierarchyPath(gameObject)}");
            
            playerLocomotion = null;
        }

        Debug.Log("Player left Board!");
    }
    
    private void ClearMovingPlatform()
    {
        if (playerLocomotion != null) {
            playerLocomotion.SetMovingPlatform(null);
            playerLocomotion = null;
        }

        playerCollidersOnBoard.Clear();
        playerOnBoard = false;
    }
}