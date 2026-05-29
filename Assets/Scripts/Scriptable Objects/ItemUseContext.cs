using SLTypes;
using UnityEngine;

public class ItemUseContext
{
    public IHuman User { get; }
    public InventoryItemDataSO ItemData { get; }
    public Vector3 AimDirection { get; }
    public Transform UseOrigin { get; }

    public ItemUseContext(IHuman user, InventoryItemDataSO itemData, Vector3 aimDirection, Transform useOrigin)
    {
        User = user;
        ItemData = itemData;
        AimDirection = aimDirection;
        UseOrigin = useOrigin;
    }
}