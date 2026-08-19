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

    public int Add(int amount)
    {
        return (runtimeValue += amount);
    }

    public int Subtract(int amount)
    {
        if (runtimeValue - amount < 0)
            return (runtimeValue = 0);
        else
            return (runtimeValue -= amount);
    }

    public int SetValue(int value)
    {
        return (runtimeValue = value < 0 ? 0 : value);
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