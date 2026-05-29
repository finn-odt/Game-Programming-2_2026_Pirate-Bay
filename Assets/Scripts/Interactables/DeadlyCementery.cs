using System.Collections.Generic;
using GameEvents;
using TriInspector;
using UnityEngine;
using UnityEngine.AI;

public class DeadlyCementery : IInteractable
{
    [SerializeField, LabelText("Tag of graves where Zombies spawn")] private string gravesTag;
    [SerializeField] private GameObject zombiePrefab;

    private List<Transform> spawnPoints = new();
    
    private bool zombiesSpawned = false;
    
    void Awake()
    {
        // collect transforms of graves in 'gravesTag'
        GameObject[] graveyard = GameObject.FindGameObjectsWithTag(gravesTag);
        if (graveyard == null || graveyard.Length == 0 || graveyard.Length > 1)
        {
            Debug.LogWarning($"No or multiple graves found with tag '{gravesTag}' for Deadly Cementery called '{gameObject.name}'");
            return;
        }
        foreach (Transform child in graveyard[0].transform)
        {
            spawnPoints.Add(child);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isGamePaused)
            return;
    }

    protected override void OnPlayerInteraction(PlayerInteractionRequestEvent e)
    {
        if (playerInTrigger)
        {
            zombiesSpawned = true;
            foreach (Transform grave in spawnPoints)
            {
                SpawnZombie(grave);
            }
        }
    }
    
    private void SpawnZombie(Transform t)
    {
        Vector3 modifiedPos = t.position + new Vector3(0f, -1.5f, 0f);  // below the ground
        
        GameObject zombie = Instantiate(zombiePrefab, modifiedPos, Quaternion.LookRotation(t.forward));
        
        // TODO: implement modifying zombie script for e.g. Agent Deactivating
        //ZombieBehaviour sb = zombie.GetComponentInChildren<ZombieBehaviour>();
        //sb.player = player;
        
        zombie.transform.parent = this.gameObject.transform;

        NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            //agent.Warp(modifiedPos);  // this would instantly beam the zombie there, right?
        }
    }
}
