using UnityEngine;

[CreateAssetMenu(menuName = "Variables/Integer Variable")]
public class IntegerSO : ScriptableObject
{
    [SerializeField]
    private int initialValue;
    
    private int runtimeValue;

    // Properties:
    public int InitialValue => initialValue;
    public int RuntimeValue => runtimeValue;

    public void Add(int amount)
    {
        runtimeValue += amount;
    }

    public void Subtract(int amount)
    {
        if (runtimeValue - amount < 0)
            runtimeValue = 0;
        else
            runtimeValue -= amount;
    }

    public void SetValue(int value)
    {
        runtimeValue = value < 0 ? 0 : value;
    }

    public void ResetToInitialValue()
    {
        runtimeValue = initialValue;
    }

    private void OnEnable()
    {
        runtimeValue = initialValue;
    }
}