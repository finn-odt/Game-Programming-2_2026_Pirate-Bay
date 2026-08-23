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

public class NPCSailorBehaviour : StatefulMonoBehaviour<NPCSailorBehaviour>
{
    [InfoBox("Configuration")]
    [HideInInspector] public IPlayer player;

    [SerializeField] public int sailingCost = 10;
    [SerializeField] public float maxDistanceWhileConversation = 10;
    [SerializeField] public MovingShipBehaviour shipBehaviour;
    
    [SerializeField] private bool drawOnlyOnSelect;

    [HideInInspector] public bool isGamePaused = false;
    [HideInInspector] public bool playerInTrigger = false, playerWantsToTalk = false;
    [HideInInspector] public bool conversationAnswered = false, conversationAccepted = false;
    
    [SerializeField] private List<Transform> meshes = new();
    private Transform mesh;
    private int meshIdx;

    void Awake()
    {
        fsm = new FSM<NPCSailorBehaviour>();
        fsm.Configure(this, new SailorIdleState());
    }

    void Start()
    {
        ServiceLocator.ForSceneOf(this).Get(out player);

        if (meshes == null || meshes.Count == 0)
            return;

        // random visual (of given meshes)
        int entityHash = gameObject.GetEntityId().GetHashCode();
        int timeHash = System.DateTime.UtcNow.Ticks.GetHashCode();
        int seed = entityHash ^ timeHash;
        System.Random rng = new System.Random(seed);
        meshIdx = rng.Next(0, meshes.Count);

        // save used mesh
        mesh = meshes[meshIdx];
        // set meshes active/inactive
        for(int i = 0; i < meshes.Count; i++)
        {
            meshes[i].gameObject.SetActive(meshIdx == i);
        }
    }

    private void OnEnable()
    {
        GameEventManager.AddListener<GameStateChangedEvent>(OnGameStateChange);
        
        GameEventManager.AddListener<PlayerInteractionRequestEvent>(OnPlayerInteraction);  // called by ThirdPersonController
        GameEventManager.AddListener<ConversationAnswerEvent>(OnConversationAnswer);  // called by ConversationUI
    }

    private void OnDisable()
    {
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
        
        GameEventManager.RemoveListener<PlayerInteractionRequestEvent>(OnPlayerInteraction);  // called by ThirdPersonController
        GameEventManager.RemoveListener<ConversationAnswerEvent>(OnConversationAnswer);  // called by ConversationUI
    }

    private void OnPlayerInteraction(PlayerInteractionRequestEvent e)
    {
        playerWantsToTalk = true;
    }

    private void OnConversationAnswer(ConversationAnswerEvent e)
    {
        conversationAnswered = true;
        conversationAccepted = e.isAccepted;
    }

    private void OnGameStateChange(GameStateChangedEvent e)
    {
        isGamePaused = e.newState == GameStateChangedEvent.GameState.Paused;
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (isGamePaused)
            return;
        
        if(other.gameObject.layer == (int)LayerId.Player) {
            playerInTrigger = true;
            if (fsm.CurrentState.GetType() == typeof(SailorIdleState))
            {
                GameEventManager.Raise(new InteractionPossibleEvent(true, gameObject));
                GameEventManager.Raise(new UIInteractIndicatorEvent());
            }
        }
    }

    protected void OnTriggerExit(Collider other)
    {
        if (isGamePaused)
            return;
        
        if(other.gameObject.layer == (int)LayerId.Player) {
            playerInTrigger = false;
            playerWantsToTalk = false;
            GameEventManager.Raise(new InteractionPossibleEvent(false, gameObject));
        }
    }

    private void OnDrawGizmos()
    {
        if(drawOnlyOnSelect)
            return;

       
    }
    
    private void OnDrawGizmosSelected()
    {
        if(!drawOnlyOnSelect)
            return;

       
    }
}
