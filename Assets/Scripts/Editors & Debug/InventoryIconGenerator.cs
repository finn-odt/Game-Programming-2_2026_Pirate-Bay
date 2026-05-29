#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class InventoryIconGenerator
{
    [MenuItem("Tools/Inventory/Generate Icons For Selected Prefabs")]
    private static void GenerateIcons()
    {
        IconRenderer renderer = Object.FindAnyObjectByType<IconRenderer>();

        if (renderer == null)
        {
            Debug.LogError("No IconRenderer found in scene.");
            return;
        }

        string folder = "Assets/UI/Inventory/Icons";

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/UI/Inventory", "Icons");
        }

        foreach (Object obj in Selection.objects)
        {
            if (obj is GameObject prefab)
            {
                string path = $"{folder}/{prefab.name}_Icon";
                string filetype = ".png";
                renderer.SaveIcon(prefab, path, filetype);
            }
        }

        AssetDatabase.Refresh();
    }
}
#endif