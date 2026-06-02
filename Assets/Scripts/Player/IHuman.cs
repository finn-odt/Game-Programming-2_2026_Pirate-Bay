using System.Collections.Generic;
using UnityEngine;

namespace SLTypes
{
    public interface IHuman
    {
        Transform Transform { get; }
        Vector3 Position { get; }
        
        float Height { get; }
        int Health { get; }

        GameObject LeftHandEquip { get; set; }
        GameObject RightHandEquip { get; set; }

        void AddHealth(int amount);
        void TakeDamage(int amount);

        void SetInitialPosition(Vector3 pos);

        public void UseItem(ItemUseBehaviourSO useBehaviour, InventoryItemDataSO itemData, Transform useOrigin);
    }
}