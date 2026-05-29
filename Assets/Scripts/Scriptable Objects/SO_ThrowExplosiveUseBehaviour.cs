using GameEvents;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Use Behaviours/Throw Explosive")]
public class ThrowExplosiveUseBehaviourSO : ItemUseBehaviourSO
{
    //[SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float throwForce = 15f;

    public override bool CanUse(ItemUseContext context)
    {
        return item != null && context.UseOrigin != null;
    }

    public override void Use(ItemUseContext context)
    {
        Debug.Log("KABUUMMMMMMMM");
        item.transform.parent = null;
        if (item.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.AddForce(context.AimDirection.normalized * throwForce, ForceMode.VelocityChange);
            
            GameEventManager.Raise(new RemoveItemFromHandForUseEvent(context.ItemData));
        }
    }
}