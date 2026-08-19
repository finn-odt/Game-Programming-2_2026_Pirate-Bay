using UnityEngine;

[CreateAssetMenu(menuName = "Variables/Float Variable")]
public class FloatSO : ScriptableObject
{
    [SerializeField]
    private float initialValue;
    
    private float runtimeValue;

    // Properties:
    public float InitialValue => initialValue;
    public float RuntimeValue => runtimeValue;

    public float Add(float amount)
    {
        return (runtimeValue += amount);
    }

    public float Subtract(float amount)
    {
        if (runtimeValue - amount < 0)
            return (runtimeValue = 0);
        else
            return (runtimeValue -= amount);
    }

    public float SetValue(float value)
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