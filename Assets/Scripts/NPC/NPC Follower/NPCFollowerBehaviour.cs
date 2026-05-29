using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GameEvents;
using Player;
using SLTypes;
using TriInspector;
using Unity.VisualScripting.FullSerializer;
using UnityConstantsGenerator;
using UnityEngine;
using UnityEngine.AI;
using UnityServiceLocator;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

#if UNITY_EDITOR
    using UnityEditor;
#endif

public class NPCFollowerBehaviour : StatefulMonoBehaviour<NPCFollowerBehaviour>
{
    [InfoBox("Configuration")]
    [SerializeField, LabelText("Tag of waypoints for this NPC")] private string waypointTag;
    [LabelText("Vision Angle X - Patrol (one-sided)"), Range(0f, 180f)] public float visionAngleXPatrol = 70f;
    [LabelText("Vision Angle Y - Patrol (one-sided)"), Range(0f, 180f)] public float visionAngleYPatrol = 45f;
    [LabelText("Vision Angle X - Chase (one-sided)"), Range(0f, 180f)] public float visionAngleXChase = 85f;
    [LabelText("Vision Angle Y - Chase (one-sided)"), Range(0f, 180f)] public float visionAngleYChase = 55f;
    public float maxVisionDistance;

    public float heightOfEyes = 1.65f;
    
    [LabelText("Seconds NPC searches after loosing sight (Dif: EASY)")] public float maxTimeForSearch = 4f;
    private float initMaxTimeForSearch;

    [Header("Gizmos")]
    [SerializeField] private bool patrolOrChase;
    [SerializeField] private bool drawOnlyOnSelect;
    
    [InfoBox("Do not configure!")]

    [HideInInspector] public int currentWaypoint = 0;
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public List<Transform> waypoints = new();

    [HideInInspector] public float currentVisionAngleX;
    [HideInInspector] public float currentVisionAngleY;
    
    [HideInInspector] public IPlayer player;

    [HideInInspector] public bool isGamePaused = false;

    void Awake()
    {
        fsm = new FSM<NPCFollowerBehaviour>();
        fsm.Configure(this, new NPCPatrolState());

        // get navmesh agent
        agent = GetComponentInChildren<NavMeshAgent>();

        // get all waypoints by tag
        GameObject[] waypointParent = GameObject.FindGameObjectsWithTag(waypointTag);
        waypoints = new List<Transform>();
        if (waypointParent == null || waypointParent.Length == 0 || waypointParent.Length > 1)
        {
            Debug.LogWarning($"No or multiple waypoint parent found with tag '{waypointTag}' for NPC Follower called '{gameObject.name}'");
            return;
        }
        foreach (Transform child in waypointParent[0].transform)
        {
            waypoints.Add(child);
        }
        
        if(waypoints.Count > 0 && agent != null)
            agent.SetDestination(waypoints[currentWaypoint].position);

        initMaxTimeForSearch = maxTimeForSearch;
    }

    void Start()
    {
        ServiceLocator.Global.Get(out player);
    }
    
    public bool AgentReady()
    {
        return agent != null &&  agent.isActiveAndEnabled && agent.isOnNavMesh;
    }
    
    private void OnEnable()
    {
        GameEventManager.AddListener<GameStateChangedEvent>(OnGameStateChange);
        GameEventManager.AddListener<GameDifficultyChangedEvent>(OnGameDifficultyChange);
    }

    private void OnDisable()
    {
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
        GameEventManager.RemoveListener<GameDifficultyChangedEvent>(OnGameDifficultyChange);
    }

    private void OnGameDifficultyChange(GameDifficultyChangedEvent e)
    {
        // set search time after loosing sight according to difficulty
        switch (e.newDifficulty)
        {
            case GameDifficulty.Easy:
                maxTimeForSearch = initMaxTimeForSearch;
                break;
            case GameDifficulty.Normal:
                maxTimeForSearch = 1.5f * initMaxTimeForSearch;
                break;
            case GameDifficulty.Hard:
                maxTimeForSearch = 2.2f * initMaxTimeForSearch;
                break;
        }
    }

    private void OnGameStateChange(GameStateChangedEvent e)
    {
        isGamePaused = e.newState == GameStateChangedEvent.GameState.Paused;  // set new value
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == (int)LayerId.Player)
            GameEventManager.Raise(new GameOverEvent(player.Position, GameOverEvent.Killer.Npc));
    }

    private void OnDrawGizmos()
    {
        if(drawOnlyOnSelect)
            return;

        Vector3 npcPos = transform.position + new Vector3(0, heightOfEyes, 0);
        Debug.DrawRay(npcPos, transform.forward * maxVisionDistance, Color.green);

        currentVisionAngleX = patrolOrChase ? visionAngleXChase : visionAngleXPatrol;  // false: patrol, true: chase
        currentVisionAngleY = patrolOrChase ? visionAngleYChase : visionAngleYPatrol;  // false: patrol, true: chase

        Vector3 origin = npcPos;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        
        // horizontal view frustum
        Vector3 leftDir = Quaternion.AngleAxis(-currentVisionAngleX, Vector3.up) * forward;
        DrawArc(origin, Vector3.up, leftDir,  currentVisionAngleX * 2f, 3f);
        
        // vertical view frustum
        Vector3 downDir = Quaternion.AngleAxis(-currentVisionAngleY, right) * forward;
        DrawArc(origin, right, downDir, currentVisionAngleY * 2f, 3f);
    }
    
    private void OnDrawGizmosSelected()
    {
        if(!drawOnlyOnSelect)
            return;

        Vector3 npcPos = transform.position + new Vector3(0, heightOfEyes, 0);
        Debug.DrawRay(npcPos, transform.forward * maxVisionDistance, Color.green);
            
        currentVisionAngleX = patrolOrChase ? visionAngleXChase : visionAngleXPatrol;  // false: patrol, true: chase
        currentVisionAngleY = patrolOrChase ? visionAngleYChase : visionAngleYPatrol;  // false: patrol, true: chase

        Vector3 origin = npcPos;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        
        // horizontal view frustum
        Vector3 leftDir = Quaternion.AngleAxis(-currentVisionAngleX, Vector3.up) * forward;
        DrawArc(origin, Vector3.up, leftDir,  currentVisionAngleX * 2f, 3f);
        
        // vertical view frustum
        Vector3 downDir = Quaternion.AngleAxis(-currentVisionAngleY, right) * forward;
        DrawArc(origin, right, downDir, currentVisionAngleY * 2f, 3f);
    }

    private void DrawArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius, Color? c = null)
    {
#if UNITY_EDITOR
        Color color = c ?? Color.red;
        color.a = 0.2f;
        Handles.color = color;

        Handles.DrawSolidArc(
            center,
            normal,
            from,
            angle,
            radius
        );
        
        Handles.color = color;

        Handles.DrawWireArc(
            center,
            normal,
            from,
            angle,
            radius
        );
#endif
    }

}
