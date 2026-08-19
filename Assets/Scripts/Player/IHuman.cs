using System.Collections.Generic;
using UnityEngine;

namespace SLTypes
{
    public interface IHuman : ILife
    {
        float Height { get; }

        GameObject LeftHandEquip { get; set; }
        GameObject RightHandEquip { get; set; }
        Transform DropOrigin { get; }

        public void UseItem(ItemUseBehaviourSO useBehaviour, bool leftHand, InventoryItemDataSO itemData, Transform useOrigin);
    }
}