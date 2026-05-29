using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class IconRenderer : MonoBehaviour
{
    [SerializeField] private Camera iconCamera;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private List<Transform> cameraPositions;
    [SerializeField] private Light iconLight;
    [SerializeField] private int iconSize = 512;

    public Texture2D RenderIcon(GameObject prefab)
    {
        if (prefab == null || iconCamera == null || spawnPoint == null)
            return null;

        GameObject instance = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        // Put object at spawn point
        instance.transform.position = spawnPoint.position;
        instance.transform.rotation = spawnPoint.rotation;

        // Optional: fit camera to object automatically
        //FocusCameraOnObject(instance);

        RenderTexture rt = new RenderTexture(iconSize, iconSize, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;

        iconCamera.clearFlags = CameraClearFlags.SolidColor;
        iconCamera.backgroundColor = new Color(0, 0, 0, 0);
        iconCamera.targetTexture = rt;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        iconCamera.Render();

        Texture2D tex = new Texture2D(iconSize, iconSize, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, iconSize, iconSize), 0, 0);
        tex.Apply();

        iconCamera.targetTexture = null;
        RenderTexture.active = previous;

        DestroyImmediate(rt);
        DestroyImmediate(instance);

        return tex;
    }

    public void SaveIcon(GameObject prefab, string path, string filetype)
    {
        int i = 0;
        foreach(Transform cameraPos in cameraPositions) {  // different distances
            iconCamera.transform.position = cameraPos.position;
            
            Texture2D icon = RenderIcon(prefab);

            if (icon == null)
            {
                Debug.LogWarning($"Could not render icon for {prefab?.name}");
                return;
            }

            byte[] png = icon.EncodeToPNG();
            File.WriteAllBytes(path + $"_{i}" + filetype, png);

            Debug.Log($"Saved icon: {path}");

            #if UNITY_EDITOR
            AssetDatabase.Refresh();
            #endif

            i++;
        }
    }

    private void FocusCameraOnObject(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        //Vector3 center = bounds.center;
        //float radius = bounds.extents.magnitude;

        //iconCamera.transform.LookAt(center);

        //float distance = radius / Mathf.Sin(iconCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        //iconCamera.transform.position = center - iconCamera.transform.forward * distance;
    }
}