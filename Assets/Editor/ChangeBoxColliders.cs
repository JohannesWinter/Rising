using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ChangeBoxColliders : EditorWindow
{
    private GameObject targetObject;
    private bool includeChildren = true;
    private float targetEdgeRadius = 0.1f;

    [MenuItem("Tools/Physics 2D/Round Box Colliders")]
    public static void ShowWindow()
    {
        GetWindow<ChangeBoxColliders>("Round Box Colliders 2D");
    }

    private void OnSelectionChange()
    {
        if (Selection.activeGameObject != null)
        {
            targetObject = Selection.activeGameObject;
            Repaint();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Round 2D Box Colliders", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);
        includeChildren = EditorGUILayout.Toggle("Include Children", includeChildren);
        targetEdgeRadius = EditorGUILayout.FloatField("Target Edge Radius", Mathf.Max(0f, targetEdgeRadius));

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Rounding", GUILayout.Height(30)))
        {
            if (targetObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select or assign a Target GameObject.", "OK");
                return;
            }

            ApplyEdgeRadius();
        }
    }

    private void ApplyEdgeRadius()
    {
        BoxCollider2D[] colliders = includeChildren
            ? targetObject.GetComponentsInChildren<BoxCollider2D>(true)
            : targetObject.GetComponents<BoxCollider2D>();

        if (colliders.Length == 0)
        {
            Debug.LogWarning("No BoxCollider2D components found on target or its children.");
            return;
        }

        int updatedCount = 0;

        foreach (BoxCollider2D col in colliders)
        {
            float currentRadius = col.edgeRadius;
            float deltaRadius = targetEdgeRadius - currentRadius;

            // Compute new size by compensating for edge radius change
            Vector2 newSize = col.size - new Vector2(2f * deltaRadius, 2f * deltaRadius);

            // Prevent size from collapsing into zero or negative dimensions
            if (newSize.x <= 0f || newSize.y <= 0f)
            {
                Debug.LogWarning($"[Skipped] Edge radius ({targetEdgeRadius}) is too large for '{col.gameObject.name}' with size ({col.size.x}, {col.size.y}).");
                continue;
            }

            // Register for Undo support
            Undo.RecordObject(col, "Round BoxCollider2D Edges");

            col.edgeRadius = targetEdgeRadius;
            col.size = newSize;

            EditorUtility.SetDirty(col);
            updatedCount++;
        }

        Debug.Log($"[Round Box Colliders] Successfully updated {updatedCount} BoxCollider2D component(s).");
    }
}
