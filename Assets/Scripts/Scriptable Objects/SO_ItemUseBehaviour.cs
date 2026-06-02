using UnityEngine;

public abstract class ItemUseBehaviourSO : ScriptableObject
{
    
    // DO NOT SET MANUALLY, will be set in runtime copy/instance
    public GameObject item;  // GameObject of the respective CollectableItem-Prefab
    public bool wasUsed = false;
    
    public abstract bool CanUse(ItemUseContext context);
    public abstract void Use(ItemUseContext context);

    public bool IsReadyToUse()
    {
        return !wasUsed;
    }

    protected abstract void OnExternalCollision(GameObject actor, Collision collision, bool isInside);
}