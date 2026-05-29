using System;
using System.Collections.Generic;
using System.Linq;
using StarterAssets;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;

public class NPCPatrolState : IFSMState<NPCFollowerBehaviour>
{
    
    public void Enter(NPCFollowerBehaviour e)
    {
        e.currentVisionAngleX = e.visionAngleXPatrol;
        e.currentVisionAngleY = e.visionAngleYPatrol;

        if(e.waypoints.Count > 0 && e.AgentReady()) {
            e.agent.SetDestination(e.waypoints[e.currentWaypoint].position);

            ThirdPersonControllerAI controller = e.GetComponentInChildren<ThirdPersonControllerAI>();
            controller.Sprinting = false;
        }
    }

    public void Reason(NPCFollowerBehaviour e)
    {
        if (e.isGamePaused)
            return;
        
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
        
        if(distance < e.maxVisionDistance)
            Debug.DrawRay(npcPos, dir * distance, Color.blue);

        bool directionAngleOkay = 
            DirectionAngleInBounds(yawDelta, e.currentVisionAngleX)
            && DirectionAngleInBounds(pitchDelta, e.currentVisionAngleY);

        // first hit object Player && NPC looks in direction (+-45° of Player)
        if(castDidHit && hitInfo.collider.gameObject.layer == e.player.Transform.gameObject.layer && directionAngleOkay)
            e.ChangeState(new NPCChaseState());
    }

    private bool DirectionAngleInBounds(float deltaAngle, float symmetricBounds)
    {
        return deltaAngle >= -symmetricBounds && deltaAngle <= symmetricBounds;
    }

    public void Update(NPCFollowerBehaviour e)
    {
        if(e.waypoints.Count == 0 || !e.AgentReady() || e.isGamePaused)
            return;

        if(HasReachedDestination(e))
        {
            NextTarget(e);
        }
    }

    public void Exit(NPCFollowerBehaviour e) {}

    private void NextTarget(NPCFollowerBehaviour e)
    {
        if(!e.AgentReady() || e.isGamePaused)
            return;

        // increase waypoint index
        e.currentWaypoint = (e.currentWaypoint + 1) % e.waypoints.Count;
        // set new destination
        e.agent.SetDestination(e.waypoints[e.currentWaypoint].position);
    }
    
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