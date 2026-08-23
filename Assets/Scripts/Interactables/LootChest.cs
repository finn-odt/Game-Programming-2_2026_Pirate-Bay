using System;
using System.Collections.Generic;
using GameEvents;
using MoreMountains.Feedbacks;
using Unity.VisualScripting;
using UnityEngine;

public class LootChest : IInteractable
{
    [SerializeField] private string nameOfMeshFolder = "Meshes";
    private List<Transform> meshes = new();
    private Transform mesh;
    private Transform lid;
    private int meshIdx;

    private ParticleSystem openingParticleEffect;

    private bool isOpen = false, isDeactivated = false;
    [SerializeField] private float lidSpeed = 40f;
    [SerializeField] private int coinAmount = 15;
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private MMF_Player feedbackPlayer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // collect the three different meshes (in children)
        Transform meshFolder = transform.Find(nameOfMeshFolder);
        foreach (Transform child in meshFolder)
        {
            meshes.Add(child);
        }
        
        openingParticleEffect = GetComponentInChildren<ParticleSystem>();

        // random visual (of given meshes)
        int entityHash = gameObject.GetEntityId().GetHashCode();
        int timeHash = System.DateTime.UtcNow.Ticks.GetHashCode();
        int seed = entityHash ^ timeHash;
        System.Random rng = new System.Random(seed);
        meshIdx = rng.Next(0, meshes.Count);

        // save used mesh
        mesh = meshes[meshIdx];
        // save lid transform
        foreach (Transform child in mesh)
        {
            if(child.name.Contains("Lid"))
                lid = child;
        }

        for(int i = 0; i < meshes.Count; i++)
        {
            meshes[i].gameObject.SetActive(meshIdx == i);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!isOpen || lid == null || isDeactivated || isGamePaused)
            return;

        // lid rotation x = 0 (start)
        // lid rotation x = -90 (goal)
        // ROTATION
        float targetPitch = -90;
        Vector3 lidRot = lid.rotation.eulerAngles;
        float currentPitch = lidRot.x;

        // Signed shortest difference, always between -180 and +180
        float delta = Mathf.DeltaAngle(currentPitch, targetPitch);

        // rotate lid as long as target is not reached
        if(Math.Abs(delta) > 0.1f) {
            currentPitch = Mathf.MoveTowardsAngle(
                currentPitch,
                targetPitch,
                lidSpeed * Time.deltaTime
            );
            lid.rotation = Quaternion.Euler(currentPitch, lidRot.y, lidRot.z);
        } else
        {
            // invoke event
            GameEventManager.Raise(new CollectedCoinEvent(coinAmount));
            GameEventManager.Raise(new OneShotAudioEvent(coinSound, transform.position, 1f));
            // deactivate for no reuse
            isDeactivated = true;
        }
    }
    
    protected override void OnPlayerInteraction(PlayerInteractionRequestEvent e)
    {
        if(playerInTrigger && !isDeactivated && !isOpen) {
            isOpen = true;
            canBeInteractedWith = false;
            feedbackPlayer?.PlayFeedbacks();
            // deactivate interaction indicator
            GameEventManager.Raise(new InteractionPossibleEvent(false, gameObject));
            if (openingParticleEffect != null) {
                openingParticleEffect.time = 0;
                openingParticleEffect.Play();
            }
        }
    }
}
