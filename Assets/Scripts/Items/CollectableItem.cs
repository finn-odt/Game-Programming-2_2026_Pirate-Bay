using System;
using System.Collections.Generic;
using GameEvents;
using UnityConstantsGenerator;
using UnityEngine;

public class CollectableItem : IInteractable
{
    public InventoryItemDataSO InventoryItemData;

    private ParticleSystem increaseVisibilityEffect;

    [SerializeField] private GameObject meshGameObject;

    public int amount;
    public bool showParticleEffect = false;
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    //private Rigidbody rb;
    //private float sleepVelocityThreshold = 0.2f;
    //private bool hasTouchedGround;

    public delegate void ColliderCallbackDelegate(GameObject actor, Collision other, bool isInside);

    private ColliderCallbackDelegate _delegate;
    
    public ItemUseBehaviourSO runtimeUseBehaviour;

    public void AddCollisionListener(ColliderCallbackDelegate callback)
    {
        _delegate = callback;
    }
    
    public void RemoveCollisionListener(ColliderCallbackDelegate callback)
    {
        if (_delegate != callback)
            return;

        _delegate = null;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision Detection ENTER - EXTERN");
        
        if(_delegate != null)
            _delegate(gameObject, collision, true);
    }
    
    private void OnCollisionExit(Collision collision)
    {
        Debug.Log("Collision Detection EXIT - EXTERN");
        if(_delegate != null)
            _delegate(gameObject, collision, false);
    }

    private void OnDestroy()
    {
        RemoveCollisionListener(null);
    }
    
    void Update()
    {
        // if item falls through ground when being placed
        if (transform.position.y < -20f)
        {
            Debug.LogWarning("Item was at y=-20 and therefore destroyed");
            // event for removal in placed items list (GameObjectFactory)
            GameEventManager.Raise(new DestroyItemEvent(gameObject));

            Destroy(gameObject);
        }
    }

    protected override void OnEnabled()
    {
        meshFilter = GetComponentInChildren<MeshFilter>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (meshFilter == null)
            meshFilter = meshGameObject.AddComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = meshGameObject.AddComponent<MeshRenderer>();
        
        //rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        ApplyItemData();

        increaseVisibilityEffect = GetComponentInChildren<ParticleSystem>();
        if (increaseVisibilityEffect == null)
            return;
        
        if (showParticleEffect)
            increaseVisibilityEffect.Play();
        else
            increaseVisibilityEffect.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if(showParticleEffect && increaseVisibilityEffect != null && !increaseVisibilityEffect.isPlaying)
            increaseVisibilityEffect.Play();
    }

    private void ApplyItemData()
    {
        if (InventoryItemData == null)
        {
            Debug.LogWarning($"{name} has no InventoryItemDataSO assigned.");
            return;
        }

        meshFilter.sharedMesh = InventoryItemData.Mesh;
        meshRenderer.sharedMaterial = InventoryItemData.Material;

        meshGameObject.transform.localScale = Vector3.one * InventoryItemData.MeshScale;
        if(InventoryItemData.MeshEulerRotation != Vector3.zero)
            meshGameObject.transform.localRotation = Quaternion.Euler(InventoryItemData.MeshEulerRotation);
        
        // set item to be usable by UseBehaviour
        if (InventoryItemData.HasUseBehaviour)
        {
            var runtimeUseBehaviour = Instantiate(InventoryItemData.UseBehaviour);
            runtimeUseBehaviour.item = this.gameObject;
            // Assign it somewhere, e.g. a field on this item
            this.runtimeUseBehaviour = runtimeUseBehaviour;
        }
    }

    protected override void OnPlayerInteraction(PlayerInteractionRequestEvent e)
    {
        // Collect Item
        if (playerInTrigger)
        {
            Inventory.Instance.Add(this);
            
            // deactivate interaction indicator
            GameEventManager.Raise(new InteractionPossibleEvent(false, gameObject));
            // event for removal in placed items list (GameObjectFactory)
            GameEventManager.Raise(new DestroyItemEvent(gameObject));
            
            Destroy(gameObject);
        }
    }
    
    /*private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == (int)LayerId.Ground)
            hasTouchedGround = true;
    }

    private void FixedUpdate()
    {
        if (!hasTouchedGround)
            return;

        if (rb.linearVelocity.sqrMagnitude <= sleepVelocityThreshold * sleepVelocityThreshold)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;  // continuous detection for laying still
            rb.isKinematic = true;
        }
    }*/
}