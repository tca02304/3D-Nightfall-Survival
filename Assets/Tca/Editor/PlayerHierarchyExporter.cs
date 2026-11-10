
using System.Text;
using UnityEngine;
using UnityEditor;

public class PlayerHierarchyExporter
{
    [MenuItem("Nightfall/Tools/Export Player Hierarchy")]
    private static void ExportPlayerHierarchy()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogWarning("Hay chon Player trong Hierarchy!");
            return;
        }

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("========== PLAYER HIERARCHY ==========");
        sb.AppendLine("Root: " + selected.name);
        sb.AppendLine();

        ExportTransform(selected.transform, sb, 0);

        string result = sb.ToString();

        GUIUtility.systemCopyBuffer = result;

        Debug.Log(
            "Da copy Player Hierarchy vao Clipboard!\n\n" + result
        );

        EditorUtility.DisplayDialog(
            "Export Completed",
            "Da copy cau truc Player vao Clipboard.\n" +
            "Hay dan noi dung vao ChatGPT.",
            "OK"
        );
    }

    private static void ExportTransform(
        Transform current,
        StringBuilder sb,
        int depth)
    {
        string indent = new string(' ', depth * 2);

        GameObject obj = current.gameObject;

        sb.AppendLine(
            indent + "- " + obj.name +
            " | Active: " + obj.activeSelf +
            " | Layer: " + LayerMask.LayerToName(obj.layer)
        );

        Component[] components = obj.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            sb.Append(indent + "  Component: " +
                      component.GetType().Name);

            if (component is SkinnedMeshRenderer smr)
            {
                sb.Append(
                    " | Mesh: " +
                    (smr.sharedMesh != null
                        ? smr.sharedMesh.name
                        : "None") +
                    " | Enabled: " + smr.enabled
                );
            }
            else if (component is MeshRenderer mr)
            {
                MeshFilter mf = obj.GetComponent<MeshFilter>();

                sb.Append(
                    " | Mesh: " +
                    (mf != null && mf.sharedMesh != null
                        ? mf.sharedMesh.name
                        : "None") +
                    " | Enabled: " + mr.enabled
                );
            }
            else if (component is Renderer renderer)
            {
                sb.Append(" | Enabled: " + renderer.enabled);
            }

            sb.AppendLine();
        }

        for (int i = 0; i < current.childCount; i++)
        {
            ExportTransform(
                current.GetChild(i),
                sb,
                depth + 1
            );
        }
    }
}