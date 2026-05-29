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

public class SharkBehaviour : StatefulMonoBehaviour<SharkBehaviour>
{
    [InfoBox("Configuration")]
    [LabelText("Vision Angle X - Patrol (one-sided)"), Range(0f, 180f)] public float visionAngleXPatrol = 140f;
    [LabelText("Vision Angle Y - Patrol (one-sided)"), Range(0f, 180f)] public float visionAngleYPatrol = 12f;
    [LabelText("Vision Angle X - Chase (one-sided)"), Range(0f, 180f)] public float visionAngleXChase = 160f;
    [LabelText("Vision Angle Y - Chase (one-sided)"), Range(0f, 180f)] public float visionAngleYChase = 18f;
    public float maxVisionDistance;
    public float maxTargetRadius;

    public float heightOfEyes = 0f;

    [Header("Gizmos")]
    [SerializeField] private bool patrolOrChase;
    [SerializeField] private bool drawOnlyOnSelect;
    
    [InfoBox("Do not configure!")]

    [HideInInspector] public IPlayer player;
    [HideInInspector] public int currentWaypoint = 0;
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public List<Transform> waypoints = new();

    [HideInInspector] public float currentVisionAngleX;
    [HideInInspector] public float currentVisionAngleY;
    
    [HideInInspector] public bool isGamePaused;

    void Awake()
    {
        fsm = new FSM<SharkBehaviour>();
        fsm.Configure(this, new SharkIdleState());

        // get navmesh agent
        agent = GetComponentInChildren<NavMeshAgent>();
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
    }

    private void OnDisable()
    {
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
    }

    private void OnGameStateChange(GameStateChangedEvent e)
    {
        isGamePaused = e.newState == GameStateChangedEvent.GameState.Paused;  // set new value
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == (int)LayerId.Player)
            GameEventManager.Raise(new GameOverEvent(player.Position, GameOverEvent.Killer.Shark));
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
