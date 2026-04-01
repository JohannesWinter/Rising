using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;

public class SplineHandleEditor : EditorWindow
{
    private SpriteShapeController shape;

    private Vector2 scroll;

    [MenuItem("Tools/Spline Handle Editor")]
    public static void Open()
    {
        GetWindow<SplineHandleEditor>("Spline Handles");
    }

    private void OnSelectionChange()
    {
        shape = null;

        if (Selection.activeGameObject != null)
        {
            shape = Selection.activeGameObject.GetComponent<SpriteShapeController>();
        }

        Repaint();
    }

    private void OnGUI()
    {
        if (shape == null)
        {
            EditorGUILayout.LabelField("Kein SpriteShape ausgewählt");
            return;
        }

        var spline = shape.spline;
        int count = spline.GetPointCount();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Punkt {i}");

            Vector3 right = spline.GetRightTangent(i);
            float length = right.magnitude;

            float angle = Mathf.Atan2(right.y, right.x) * Mathf.Rad2Deg;

            float newAngle = EditorGUILayout.FloatField("Winkel", angle);
            float newLength = EditorGUILayout.FloatField("Länge", length);

            if (newAngle != angle || newLength != length)
            {
                Apply(spline, i, newAngle, newLength);
            }

            if (GUILayout.Button("Reset Tangenten"))
            {
                Undo.RecordObject(shape, "Reset Tangents");

                spline.SetLeftTangent(i, Vector3.zero);
                spline.SetRightTangent(i, Vector3.zero);

                shape.RefreshSpriteShape();
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private void Apply(Spline spline, int index, float angleDeg, float length)
    {
        Undo.RecordObject(shape, "Set Spline Handle");

        float rad = angleDeg * Mathf.Deg2Rad;

        Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);

        spline.SetTangentMode(index, ShapeTangentMode.Continuous);

        spline.SetRightTangent(index, dir * length);
        spline.SetLeftTangent(index, -dir * length);

        shape.RefreshSpriteShape();
    }
}