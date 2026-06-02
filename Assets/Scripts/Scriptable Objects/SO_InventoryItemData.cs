using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Data")]
public class InventoryItemDataSO : ScriptableObject
{
    [SerializeField] private string itemId;

    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    [SerializeField] private float meshScale = 1f;
    [SerializeField] private Vector3 meshEulerRotation = Vector3.zero;

    [SerializeField] private string itemName;
    [SerializeField] private string description;
    [SerializeField] private float shopCost;
    [SerializeField, Range(0, 5)] private int weight;
    [SerializeField, Range(0, 5)] private int efficiency;
    [SerializeField] private Texture2D icon;
    
    [SerializeField] private ItemUseBehaviourSO useBehaviour;
    public bool HasUseBehaviour => useBehaviour != null;
    public ItemUseBehaviourSO UseBehaviour => useBehaviour;

    public string ItemId => itemId;
    public Mesh Mesh => mesh;
    public float MeshScale => meshScale;
    public Vector3 MeshEulerRotation => meshEulerRotation;
    public Material Material => material;
    public string ItemName => itemName;
    public string Description => description;
    public float ShopCost => shopCost;
    public int Weight => weight;
    public int Efficiency => efficiency;
    public Texture2D Icon => icon;
}