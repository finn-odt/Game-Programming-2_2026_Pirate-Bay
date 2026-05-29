using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameEvents;
using StarterAssets;
using TriInspector;
using UnityEngine;
using UnityEngine.AI;

public class NPCChaseState : IFSMState<NPCFollowerBehaviour>
{

    private float timeSinceLastEncounter = 0;

    
    public void Enter(NPCFollowerBehaviour e)
    {
        e.currentVisionAngleX = e.visionAngleXChase;
        e.currentVisionAngleY = e.visionAngleYChase;

        timeSinceLastEncounter = 0;

        // set destination (player)
        if(e.AgentReady()) {
            e.agent.SetDestination(e.player.Position);

            ThirdPersonControllerAI controller = e.GetComponentInChildren<ThirdPersonControllerAI>();
            controller.Sprinting = true;
        }
    }

    public void Reason(NPCFollowerBehaviour e)
    {        
        Vector3 npcForward = e.transform.forward;
        Vector3 playerPos = e.player.Position + new Vector3(0, e.heightOfEyes, 0);  // account height of model
        Vector3 npcPos = e.transform.position + new Vector3(0, e.heightOfEyes, 0);

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
        
        // first hit object is Player? && NPC looks in direction (e.g. +-45° direction angle to Player) [or distance<1f]
        if(((castDidHit && hitInfo.collider.gameObject.layer == e.player.Transform.gameObject.layer) || distance < 1f) && directionAngleOkay)
            timeSinceLastEncounter = 0;  // reset time, because NPC sees Player
        else
            timeSinceLastEncounter += Time.deltaTime;  // increase time NPC doesn't see Player

        NavMeshPathStatus pathState = e.agent.pathStatus;

        // back to Patrol if Player was not seen for backToPatrolTime OR path-destination is not reachable
        if(timeSinceLastEncounter > e.maxTimeForSearch || pathState == NavMeshPathStatus.PathPartial)
            e.ChangeState(new NPCPatrolState());
    }

    private bool DirectionAngleInBounds(float deltaAngle, float symmetricBounds)
    {
        return deltaAngle >= -symmetricBounds && deltaAngle <= symmetricBounds;
    }

    public void Update(NPCFollowerBehaviour e)
    {
        if(!e.AgentReady() || e.isGamePaused)
            return;

        // set destination (player)
        e.agent.SetDestination(e.player.Position);
    }

    public void Exit(NPCFollowerBehaviour e) {}
    
    private bool HasReachedDestination(NPCFollowerBehaviour e)
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