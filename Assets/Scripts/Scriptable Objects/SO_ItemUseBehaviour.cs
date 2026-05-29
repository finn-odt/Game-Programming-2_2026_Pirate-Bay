using UnityEngine;

public abstract class ItemUseBehaviourSO : ScriptableObject
{

    public GameObject item;
    
    public abstract bool CanUse(ItemUseContext context);
    public abstract void Use(ItemUseContext context);
}