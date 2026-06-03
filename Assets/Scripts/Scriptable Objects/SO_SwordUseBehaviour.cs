using System.Collections;
using GameEvents;
using TriInspector;
using Unity.VisualScripting;
using UnityConstantsGenerator;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Use Behaviours/Sword")]
public class SwordUseBehaviourSO : ItemUseBehaviourSO
{
    [SerializeField] private float cooldownTime;

    public override bool CanUse(ItemUseContext context)
    {
        return item != null && context.UseOrigin != null;
    }

    public override void Use(ItemUseContext context)
    {
        // animation is only for right side
        if (context.Side == ItemUseContext.BodySide.Left)
            return;
        
        if (IsReadyToUse())
        {
            GameEventManager.Raise(new PlayerSwordAttackEvent());
            
            Debug.Log("Sword Used");
            
            wasUsed = true;
            CoroutineRunner.Instance.RunCoroutine(Cooldown());  // cooldown for being usable again
        }
    }

    private IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(cooldownTime);
        wasUsed = false;
        Debug.Log("Sword Usable Again");
    }

    protected override void OnExternalCollision(GameObject actor, Collision collision, bool isInside)
    {
        // not needed;
    }
}