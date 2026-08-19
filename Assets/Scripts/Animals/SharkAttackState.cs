using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameEvents;
using StarterAssets;
using TriInspector;
using UnityEngine;
using UnityEngine.AI;

public class SharkAttackState : IFSMState<SharkBehaviour>
{

    private float timeSinceLastEncounter = 0;
    [SerializeField, LabelText("Seconds Shark waits til patroling (when losing visual)")] private float backToPatrolTime = 1.5f;

    
    public void Enter(SharkBehaviour e)
    {
        e.currentVisionAngleX = e.visionAngleXChase;
        e.currentVisionAngleY = e.visionAngleYChase;

        timeSinceLastEncounter = 0;

        // set destination (player)
        if(e.AgentReady()) {
            e.agent.SetDestination(e.player.Position);
        }
    }

    public void Reason(SharkBehaviour e)
    {        
        if(e.isGamePaused)
            return;
        
        Vector3 npcForward = e.transform.forward;
        Vector3 playerPos = e.player.Position + new Vector3(0, 1f, 0);  // account hight of model
        Vector3 npcPos = e.transform.position + e.meshOffset + new Vector3(0, e.heightOfEyes, 0);

        // Raycast from NPC to Player
        Vector3 dir = playerPos - npcPos;
        dir.Normalize();
        float distance = Vector3.Distance(npcPos, playerPos);

        // for horizontal axis
        float dirYaw = Quaternion.LookRotation(dir).eulerAngles.y;
        float forwardYaw = Quaternion.LookRotation(npcForward).eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(dirYaw, forwardYaw);  // angle has to be between +45/-45° | Signed shortest difference, always between -180 and +180
        
        // for vertical axis
        float dirPitch = Quaternion.LookRotation(dir).eulerAngles.x;
        float forwardPitch = Quaternion.LookRotation(npcForward).eulerAngles.x;
        float pitchDelta = Mathf.DeltaAngle(dirPitch, forwardPitch);  // angle has to be between +45/-45° | Signed shortest difference, always between -180 and +180

        bool castDidHit = Physics.SphereCast(
            npcPos,
            0.1f,  // radius of Ray
            dir,
            out RaycastHit hitInfo,
            Math.Min(distance + 5f, e.maxVisionDistance)  // distance between Player and NPC +5 (tolerance) OR maxDistance
        );
        
        Debug.DrawRay(npcPos, dir * distance, Color.blue);

        bool directionAngleOkay = 
            DirectionAngleInBounds(yawDelta, e.currentVisionAngleX)
            && DirectionAngleInBounds(pitchDelta, e.currentVisionAngleY);
        
        // first hit object is Player? && NPC looks in direction (e.g. +-45° direction angle to Player)
        if(castDidHit && hitInfo.collider.gameObject.layer == e.player.Transform.gameObject.layer && directionAngleOkay)
            timeSinceLastEncounter = 0;  // reset time, because NPC sees Player
        else
            timeSinceLastEncounter += Time.deltaTime;  // increase time NPC doesn't see Player

        NavMeshPathStatus pathState = e.agent.pathStatus;

        // back to Patrol if Player was not seen for backToPatrolTime OR path-destination is not reachable
        if(timeSinceLastEncounter > backToPatrolTime)  //  || pathState == NavMeshPathStatus.PathPartial || pathState == NavMeshPathStatus.PathInvalid
            e.ChangeState(new SharkIdleState());
    }

    private bool DirectionAngleInBounds(float deltaAngle, float symmetricBounds)
    {
        return deltaAngle >= -symmetricBounds && deltaAngle <= symmetricBounds;
    }

    public void Update(SharkBehaviour e)
    {
        if(!e.AgentReady() || e.isGamePaused)
            return;

        // set destination (player)
        e.agent.SetDestination(e.player.Position);
    }

    public void Exit(SharkBehaviour e) {}
    
    private bool HasReachedDestination(SharkBehaviour e)
    {
        if (e.agent.pathPending)
            return false;

        if (e.agent.remainingDistance > e.agent.stoppingDistance)
            return false;

        if (e.agent.hasPath && e.agent.velocity.sqrMagnitude > 0.01f)
            return false;

        return true;
    }
}