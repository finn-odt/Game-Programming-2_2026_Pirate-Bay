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

    public void Add(float amount)
    {
        runtimeValue += amount;
    }

    public void Subtract(float amount)
    {
        if (runtimeValue - amount < 0)
            runtimeValue = 0;
        else
            runtimeValue -= amount;
    }

    public void SetValue(float value)
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