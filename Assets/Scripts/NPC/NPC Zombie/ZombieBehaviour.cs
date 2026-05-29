using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TriInspector;
using UnityEngine;
using UnityEngine.AI;

public class ZombieBehaviour : MonoBehaviour
{
    private NavMeshAgent agent;
    private List<Transform> waypoints = new();
    [SerializeField, LabelText("Tag of waypoints for this NPC")] private string waypointTag;

    private int currentWaypoint = 0;
    private bool isLookingAround = false;
    private Vector3 lastAgentPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastAgentPos = transform.position;

        // get navmesh agent
        agent = GetComponentInChildren<NavMeshAgent>();

        // get all waypoints by tag
        GameObject[] waypointParent = GameObject.FindGameObjectsWithTag(waypointTag);
        waypoints = new List<Transform>();
        if (waypointParent == null || waypointParent.Length == 0 || waypointParent.Length > 1)
        {
            Debug.LogWarning($"No or multiple waypoint parent found with tag '{waypointTag}' for NPC called '{gameObject.name}'");
            return;
        }
        foreach (Transform child in waypointParent[0].transform)
        {
            waypoints.Add(child);
        }

        if(waypoints.Count > 0 && agent != null)
            agent.SetDestination(waypoints[currentWaypoint].position);
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

    // Update is called once per frame
    void Update()
    {
        if(waypoints.Count == 0 || agent == null)
            return;

        if(HasReachedDestination() && !isLookingAround)
        {
            //Debug.Log(transform.position.y);
            isLookingAround = true;
            StartCoroutine(LookAround(waypoints[currentWaypoint].forward, 3f));
        } else
        {
            lastAgentPos = transform.position;  // save last position for later use
        }
    }

    private void NextTarget()
    {
        // increase waypoint index
        currentWaypoint = (currentWaypoint + 1) % waypoints.Count;
        // set new destination
        agent.SetDestination(waypoints[currentWaypoint].position);
    }

    private IEnumerator LookAround(Vector3 lookDir, float delay)
    {
        // Restore correct y position (agent looses control and character drops down by ~0.3)
        Vector3 pos = transform.position;
        pos.y = lastAgentPos.y;

        lookDir.y = 0f;

        if (lookDir.sqrMagnitude < 0.001f)
            yield break;

        lookDir.Normalize();

        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);

        float overshoot = 15f;  // how much the npc looks around (overshoots the target)
        float signedAngle = Vector3.SignedAngle(transform.forward, lookDir, Vector3.up);
    
        float overshootSign = Mathf.Sign(signedAngle);
        Quaternion overshootRot = targetRot * Quaternion.Euler(0f, overshoot * overshootSign, 0f);

        float undershootSign = overshootSign * -1f;  // invert sign
        Quaternion undershootRot = targetRot * Quaternion.Euler(0f, overshoot * undershootSign, 0f);

        float time = 0;
        while(time < delay)
        {
            if(time < 0.6f * delay)
            {
                float t = time / (delay * 0.6f);  // first 60% of the time
                transform.rotation = Quaternion.Slerp(startRot, overshootRot, t);
            } else
            {
                float t = (time - 0.6f * delay) / (delay * 0.4f);  // last 40% of the time
                transform.rotation = Quaternion.Slerp(overshootRot, undershootRot, t);
            }   
            transform.position = pos;

            time += Time.deltaTime;
            yield return null;
        }
        NextTarget();
        isLookingAround = false;
    }
}
