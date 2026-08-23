using UnityEngine;

public class TentacleFluid : MonoBehaviour
{
    [Header("Tentacle Setup")]
    [Tooltip("Assign your joints here in order, from the root (0) to the tip.")]
    public Transform[] joints;

    [Header("Seed Settings")]
    [Tooltip("If checked, this tentacle generates a completely random seed and wave offset at startup.")]
    public bool useRandomSeed = true;
    [Tooltip("The specific noise seed used if 'Use Random Seed' is unchecked.")]
    public float customNoiseSeed = 0f;
    [Tooltip("The specific wave time offset used if 'Use Random Seed' is unchecked.")]
    public float customWaveOffset = 0f;

    [Header("Wave Motion (The Fluid 'S' Curve)")]
    [Tooltip("How fast the wave travels from base to tip.")]
    public float waveSpeed = 3.0f;
    [Tooltip("How close the waves are to each other. Higher = more 'S' bends in the tentacle.")]
    public float waveFrequency = 0.4f;
    [Tooltip("The maximum degrees the wave bends the joints on the X, Y, and Z axes.")]
    public Vector3 waveAmplitude = new Vector3(30f, 0f, 30f);

    [Header("Organic Noise (The Chaotic Thrashing)")]
    [Tooltip("How fast the random flailing changes direction.")]
    public float noiseSpeed = 1.5f;
    [Tooltip("How much random rotation is added on top of the smooth wave.")]
    public Vector3 noiseAmplitude = new Vector3(15f, 10f, 15f);

    [Header("Flexibility Weighting")]
    [Tooltip("Curve defining flexibility. Left is the root (0), Right is the tip (1).")]
    public AnimationCurve flexibilityCurve = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f);

    private Quaternion[] initialLocalRotations;
    private float randomSeed;
    private float waveTimeOffset;

    void Start()
    {
        if (joints == null || joints.Length == 0)
        {
            Debug.LogWarning("TentacleFluid: No joints assigned to the array!");
            return;
        }

        initialLocalRotations = new Quaternion[joints.Length];

        // Store the resting pose
        for (int i = 0; i < joints.Length; i++)
        {
            initialLocalRotations[i] = joints[i].localRotation;
        }

        // Evaluate the checkbox at startup
        if (useRandomSeed)
        {
            randomSeed = Random.Range(0f, 5000f);
            waveTimeOffset = Random.Range(0f, 100f);
        }
        else
        {
            randomSeed = customNoiseSeed;
            waveTimeOffset = customWaveOffset;
        }
    }

    void Update()
    {
        if (joints == null || joints.Length == 0) return;

        // Create a unique time variable for this tentacle's wave movement
        float individualWaveTime = Time.time + waveTimeOffset;

        for (int i = 0; i < joints.Length; i++)
        {
            // Calculate where this joint is along the chain (0.0 at root, 1.0 at tip)
            float normalizedIndex = (float)i / (joints.Length - 1);

            // Read flexibility from the Animation Curve
            float flexibility = flexibilityCurve.Evaluate(normalizedIndex);

            // Calculate the Sine Wave using desynchronized time variable
            float waveOffset = i * waveFrequency;
            float sinTime = (individualWaveTime * waveSpeed) - waveOffset;

            // Multiply the time for each axis
            float xWave = Mathf.Sin(sinTime) * waveAmplitude.x;
            float yWave = Mathf.Sin(sinTime * 0.8f) * waveAmplitude.y;
            float zWave = Mathf.Sin(sinTime * 1.2f) * waveAmplitude.z;

            // Calculate the Perlin Noise
            float nTime = Time.time * noiseSpeed;
            // Add the joint index into the noise coordinates
            float xNoise = (Mathf.PerlinNoise(nTime + randomSeed, i * 0.1f) - 0.5f) * 2f * noiseAmplitude.x;
            float yNoise = (Mathf.PerlinNoise(i * 0.1f, nTime + randomSeed) - 0.5f) * 2f * noiseAmplitude.y;
            float zNoise = (Mathf.PerlinNoise(nTime + randomSeed + 50f, i * 0.1f) - 0.5f) * 2f * noiseAmplitude.z;

            // Combine Wave and Noise then multiply by flexibility
            Vector3 finalEuler = new Vector3(
                (xWave + xNoise) * flexibility,
                (yWave + yNoise) * flexibility,
                (zWave + zNoise) * flexibility
            );

            // Apply the rotation locally
            joints[i].localRotation = initialLocalRotations[i] * Quaternion.Euler(finalEuler);
        }
    }
}