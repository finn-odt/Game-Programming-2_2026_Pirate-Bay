using GameEvents;
using UnityConstantsGenerator;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Use Behaviours/Throw Explosive")]
public class ThrowExplosiveUseBehaviourSO : ItemUseBehaviourSO
{
    [SerializeField] private bool explosionEffectEnabled, smokeEffectEnabled;
    //[SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private GameObject explosionEffectPrefab, smokeEffectPrefab;

    public override bool CanUse(ItemUseContext context)
    {
        return item != null && context.UseOrigin != null;
    }

    public override void Use(ItemUseContext context)
    {
        if (item.TryGetComponent(out CollectableItem ci) && item.TryGetComponent(out Rigidbody rb) && IsReadyToUse())
        {
            GameEventManager.Raise(new RemoveItemFromHandForUseEvent(context.ItemData));
            
            ci.amount = 1;  // set amount to 1
            ci.AddCollisionListener(OnExternalCollision);  // delegate for OnCollisionEnter
            
            // apply force in forward direction with RigidBody
            rb.isKinematic = false;
            if (item.TryGetComponent(out BoxCollider col))  // enable BoxCollider again
                col.enabled = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.AddForce(context.AimDirection.normalized * throwForce, ForceMode.VelocityChange);
        }
    }

    protected override void OnExternalCollision(GameObject actor, Collision collision, bool isInside)
    {
        if (collision.gameObject.layer == (int)LayerId.Player || !IsReadyToUse())
            return;
        
        Debug.Log("Collision Detection INTERN");

        if (explosionEffectEnabled)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, item.transform.position, Quaternion.identity);
            explosion.transform.parent = null; // explosion stays where it started

            if (explosion.TryGetComponent(out ParticleSystem ps))
            {
                ps.Play(true); // true = include child particle systems
                Destroy(explosion, ps.main.duration); // Destroy GameObject after particle finishes
            }
        }
        if (smokeEffectEnabled)
        {
            GameObject smoke = Instantiate(smokeEffectPrefab, item.transform.position, Quaternion.identity);
            smoke.transform.parent = null; // smoke stays where it started

            if (smoke.TryGetComponent(out ParticleSystem ps))
            {
                ps.Play(true); // true = include child particle systems
                Destroy(smoke, ps.main.duration); // Destroy GameObject after particle finishes
            }
        }

        wasUsed = true;
        Destroy(item);  // explosive item is not needed anymore
    }
}