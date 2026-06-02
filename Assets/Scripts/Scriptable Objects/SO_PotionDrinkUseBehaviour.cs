using GameEvents;
using UnityConstantsGenerator;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Use Behaviours/Potion")]
public class PotionDrinkUseBehaviourSO : ItemUseBehaviourSO
{
    [SerializeField] private GameObject emptyPotionPrefab;
    [SerializeField] private bool healingAbility;
    [SerializeField] private bool strengthAbility;
    [SerializeField] private int healingAmount;
    [SerializeField] private float strengthFactor;
    [SerializeField] private float strengthDuration;

    public override bool CanUse(ItemUseContext context)
    {
        return item != null && context.UseOrigin != null;
    }

    public override void Use(ItemUseContext context)
    {
        if (IsReadyToUse())
        {
            GameEventManager.Raise(new RemoveItemFromHandForUseEvent(context.ItemData));
            
            if(healingAbility)
                GameEventManager.Raise(new PlayerHealEvent(healingAmount));
            if(strengthAbility)
                GameEventManager.Raise(new PlayerBerserkEvent(strengthFactor, strengthDuration));
            
            wasUsed = true;
            // in the future: start drink animation and wait for it to finish
            Destroy(item);

            // Instantiate empty bottle, not interactable
            if (emptyPotionPrefab != null)
            {
                GameObject emptyPotion = Instantiate(emptyPotionPrefab, context.UseOrigin.transform.position, Quaternion.identity);
                emptyPotion.transform.parent = null;
            }
        }
    }

    protected override void OnExternalCollision(GameObject actor, Collision collision, bool isInside)
    {
        // not needed;
    }
}