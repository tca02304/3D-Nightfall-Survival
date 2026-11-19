
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIRaycastDebugger : MonoBehaviour
{
    [SerializeField]
    private bool enableDebug = true;

    private void Update()
    {
        if (!enableDebug)
            return;

        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current == null)
            return;

        PointerEventData pointer =
            new PointerEventData(EventSystem.current);

        pointer.position = Mouse.current.position.ReadValue();

        List<RaycastResult> results = new();

        EventSystem.current.RaycastAll(pointer, results);

        Debug.Log(
            $"[UIRaycast] Hit count = {results.Count}");

        for (int i = 0; i < Mathf.Min(results.Count, 10); i++)
        {
            Debug.Log(
                $"[UIRaycast] {i}: " +
                $"{results[i].gameObject.name} | " +
                $"Path={GetPath(results[i].gameObject.transform)}");
        }
    }

    private string GetPath(Transform target)
    {
        string path = target.name;

        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }
}
