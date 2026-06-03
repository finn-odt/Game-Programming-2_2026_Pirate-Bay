using System;
using SLTypes;
using UnityEngine;

public class ItemUseContext
{
    [Flags]
    public enum BodySide
    {
        Left,
        Right
    }
    
    public IHuman User { get; }
    public BodySide Side { get; }
    public InventoryItemDataSO ItemData { get; }
    public Vector3 AimDirection { get; }
    public Transform UseOrigin { get; }

    public ItemUseContext(IHuman user, BodySide bodyside, InventoryItemDataSO itemData, Vector3 aimDirection, Transform useOrigin)
    {
        User = user;
        Side = bodyside;
        ItemData = itemData;
        AimDirection = aimDirection;
        UseOrigin = useOrigin;
    }
}