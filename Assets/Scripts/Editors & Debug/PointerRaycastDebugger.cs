using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PointerRaycastDebugger : MonoBehaviour
{
    private readonly List<RaycastResult> results = new();

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current == null)
        {
            Debug.LogError("No EventSystem.current found.");
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = mousePosition
        };

        results.Clear();
        EventSystem.current.RaycastAll(pointerData, results);

        Debug.Log($"Pointer click at {mousePosition}");
        Debug.Log($"Raycast results: {results.Count}");

        if (results.Count == 0)
        {
            Debug.LogWarning("Nothing was hit by the UI raycast.");
            return;
        }

        RaycastResult top = results[0];

        Debug.LogWarning(
            "TOP UI HIT: " +
            GetPath(top.gameObject) +
            " | module=" + top.module?.GetType().Name +
            " | sortingOrder=" + top.sortingOrder +
            " | depth=" + top.depth +
            " | distance=" + top.distance
        );
    }

    private static string GetPath(GameObject obj)
    {
        if (obj == null)
            return "<null>";

        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}