using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BoatNavMeshAgentHandler : MonoBehaviour
{
    private NavMeshAgent agent;
    private Vector3 lastPosition;

    public NavMeshAgent Agent => agent;
    public Vector3 DesiredVelocity => agent != null ? agent.desiredVelocity : Vector3.zero;
    public bool IsReady => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
    public bool HasPath => agent != null && agent.hasPath;
    public bool PathPending => agent != null && agent.pathPending;
    public NavMeshPathStatus PathStatus => agent != null ? agent.pathStatus : NavMeshPathStatus.PathInvalid;

    public Vector3 ConsumeDelta()
    {
        Vector3 currentPosition = transform.position;
        Vector3 delta = currentPosition - lastPosition;
        lastPosition = currentPosition;
        return delta;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        lastPosition = transform.position;
    }

    public void Warp(Vector3 position)
    {
        if (agent == null) {
            return;
        }

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 10f, NavMesh.AllAreas)) {
            agent.Warp(hit.position);
            lastPosition = hit.position;
        }
    }

    public bool SetDestination(Vector3 position)
    {
        if (!IsReady) {
            return false;
        }

        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 10f, NavMesh.AllAreas)) {
            Debug.LogWarning($"BoatNavMeshAgentHandler: Destination is not near NavMesh: {position}");
            return false;
        }

        return agent.SetDestination(hit.position);
    }

    public void Stop()
    {
        if (agent == null) {
            return;
        }

        agent.ResetPath();
        agent.isStopped = true;
        lastPosition = transform.position;
    }

    public void Resume()
    {
        if (agent == null) {
            return;
        }

        agent.isStopped = false;
        lastPosition = transform.position;
    }
    
    public bool HasReachedDestination()
    {
        if (agent == null)
            return false;

        if (agent.pathPending)
            return false;
        
        if (Single.IsPositiveInfinity(agent.remainingDistance))
            return false;

        if (agent.remainingDistance > agent.stoppingDistance)
            return false;

        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f)
            return false;

        return true;
    }
}