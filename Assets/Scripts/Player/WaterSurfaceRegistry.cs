using System.Collections.Generic;
using UnityConstantsGenerator;
using UnityEngine;

public sealed class WaterSurfaceRegistry : MonoBehaviour
{
    public static readonly HashSet<GameObject> WaterSurfaces = new();

    private void OnEnable()
    {
        if(gameObject.layer == (int)LayerId.WaterSurface && transform.childCount == 0)
            WaterSurfaces.Add(gameObject);
    }

    private void OnDisable()
    {
        if(gameObject.layer == (int)LayerId.WaterSurface && transform.childCount == 0)
            WaterSurfaces.Remove(gameObject);
    }
}