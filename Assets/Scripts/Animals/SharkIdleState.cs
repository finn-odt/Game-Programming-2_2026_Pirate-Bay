using System;
using System.Collections.Generic;
using System.Linq;
using StarterAssets;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;

public class SharkIdleState : IFSMState<SharkBehaviour>
{
    
    public void Enter(SharkBehaviour e)
    {
        e.currentVisionAngleX = e.visionAngleXPatrol;
        e.currentVisionAngleY = e.visionAngleYPatrol;

        if (e.AgentReady() && TryGetRandomNavMeshPoint(e, out Vector3 idleTarget))
        {
            e.agent.SetDestination(idleTarget);
        }
    }
    
    private bool TryGetRandomNavMeshPoint(SharkBehaviour e, out Vector3 result)
    {
        const int attempts = 20;

        for (int i = 0; i < attempts; i++)
        {
            if (TryGetSingleRandomNavMeshPoint(e, out result))
                return true;
        }

        result = e.transform.position;
        return false;
    }

    private bool TryGetSingleRandomNavMeshPoint(SharkBehaviour e, out Vector3 result)
    {
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * e.maxTargetRadius;
        Vector3 randomPoint = e.transform.position + new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );

        Vector3 navSearchPoint = randomPoint; //hits[0].point;

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = e.agent.agentTypeID,
            areaMask = NavMesh.AllAreas
        };
        if (NavMesh.SamplePosition(navSearchPoint, out NavMeshHit navHit, e.maxTargetRadius, filter))
        {
            // suitable Target found, return new target position
            result = navHit.position;
            return true;
        }

        // No suitable Target found, return current position
        result = e.transform.position;
        return false;
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
        
        if(distance < e.maxVisionDistance)
            Debug.DrawRay(npcPos, dir * distance, Color.blue);

        bool directionAngleOkay = 
            DirectionAngleInBounds(yawDelta, e.currentVisionAngleX)
            && DirectionAngleInBounds(pitchDelta, e.currentVisionAngleY);

        // first hit object Player && NPC looks in direction (+-45° of Player)
        if(castDidHit && hitInfo.collider.gameObject.layer == e.player.Transform.gameObject.layer && directionAngleOkay)
            e.ChangeState(new SharkAttackState());
    }

    private bool DirectionAngleInBounds(float deltaAngle, float symmetricBounds)
    {
        return deltaAngle >= -symmetricBounds && deltaAngle <= symmetricBounds;
    }

    public void Update(SharkBehaviour e)
    {
        if(!e.AgentReady() || e.isGamePaused)
            return;

        if(HasReachedDestination(e))
        {
            NextTarget(e);
        }
    }

    public void Exit(SharkBehaviour e) {}

    private void NextTarget(SharkBehaviour e)
    {
        if (e.AgentReady() && TryGetRandomNavMeshPoint(e, out Vector3 idleTarget))
        {
            e.agent.SetDestination(idleTarget);
        }
    }
    
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