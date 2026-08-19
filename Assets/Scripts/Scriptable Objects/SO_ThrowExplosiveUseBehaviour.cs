using System.Collections;
using System.Collections.Generic;
using GameEvents;
using SLTypes;
using TriInspector;
using UnityConstantsGenerator;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Inventory/Use Behaviours/Throw Explosive")]
public class ThrowExplosiveUseBehaviourSO : ItemUseBehaviourSO
{
    [SerializeField] private bool explosionEffectEnabled;
    [SerializeField] private bool smokeEffectEnabled;
    [SerializeField, ShowIf(nameof(explosionEffectEnabled))] private GameObject explosionEffectPrefab;
    [SerializeField, ShowIf(nameof(smokeEffectEnabled))] private GameObject smokeEffectPrefab;
    
    [SerializeField] private AudioClip soundOnImpact;
    
    //[SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float throwForce = 15f;
    
    [SerializeField, ShowIf(nameof(explosionEffectEnabled))] private int damageInCenter = 50;
    [SerializeField, ShowIf(nameof(explosionEffectEnabled))] private float explosionRadius = 3f;  // full attenuation at radius-edge
    [SerializeField, ShowIf(nameof(explosionEffectEnabled))] private LayerMask lifeformLayers;
    

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
            
            // apply force in forward direction with RigidBody
            rb.isKinematic = false;
            if (item.TryGetComponent(out BoxCollider col))  // enable BoxCollider again
                col.enabled = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.AddForce(context.AimDirection.normalized * throwForce, ForceMode.VelocityChange);

            ci.StartCoroutine(StartCollisionDetectionForExplosion(0.1f, ci));
        }
    }

    private IEnumerator StartCollisionDetectionForExplosion(float delay, CollectableItem ci)
    {
        yield return new WaitForSecondsRealtime(delay);
        ci.AddCollisionListener(OnExternalCollision);  // delegate for OnCollisionEnter
    }

    protected override void OnExternalCollision(GameObject actor, Collision collision, bool isInside)
    {
        if (collision.gameObject.layer == (int)LayerId.Player || !IsReadyToUse())
            return;

        if(soundOnImpact != null)
            GameEventManager.Raise(new OneShotAudioEvent(soundOnImpact, item.transform.position, 1f));
        
        if (explosionEffectEnabled)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, item.transform.position, Quaternion.identity);
            explosion.transform.parent = null; // explosion stays where it started

            if (explosion.TryGetComponent(out ParticleSystem ps))
            {
                ps.Play(true); // true = include child particle systems
                Destroy(explosion, ps.main.duration); // Destroy GameObject after particle finishes
            }
            
            List<ExplosionQuery.Lifeform> lifeforms = ExplosionQuery.GetLifeformsInRadius(item.transform.position, explosionRadius, lifeformLayers);
            foreach (ExplosionQuery.Lifeform being in lifeforms)
            {
                Debug.Log("Affected lifeform: " + being.life.Transform.gameObject.name);
                
                int damage = (int)(damageInCenter * (1 - being.distance / explosionRadius));
                
                if(being.human != null && being.human.GetType() == typeof(IPlayer))  // Player
                    GameEventManager.Raise(new PlayerDamageEvent(damage, PlayerDamageEvent.DamagedBy.Explosion));
                else
                    being.life.TakeDamage(damage);  // Animal or Human
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