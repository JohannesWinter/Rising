using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SprieShapeTiler : MonoBehaviour
{
    public SpriteShapeController spc;
    // Start is called before the first frame update
    void Start()
    {
        var x = GetPathPointsRelativeTo(this.gameObject.transform, 5, spc);
        for (int i = 0; i < x.Count - 1; i++)
        {
            Debug.DrawLine(x[i], x[i + 1], Color.red, 1000);
        }
        for (int i = 0; i < x.Count; i++)
        {
            print(x[i]);
        }
    }



    public List<Vector3> GetPathPointsRelativeTo(Transform targetTransform, int pointCount, SpriteShapeController spriteShapeController)
    {
        List<Vector3> relativePoints = new List<Vector3>();
        if (targetTransform == null) return relativePoints;

        // Get world positions calculated from the SpriteShape
        List<Vector3> worldPoints = GetPathPoints(pointCount, spriteShapeController);

        foreach (Vector3 worldPoint in worldPoints)
        {
            // Converts World Space -> Target's Local Space
            Vector3 localPoint = targetTransform.InverseTransformPoint(worldPoint);
            relativePoints.Add(localPoint);
        }

        return relativePoints;
    }
    public List<Vector3> GetPathPoints(int pointCount, SpriteShapeController spriteShapeController)
    {
        List<Vector3> points = new List<Vector3>();
        if (spriteShapeController == null || pointCount < 2) return points;

        Spline spline = spriteShapeController.spline;
        int controlPointCount = spline.GetPointCount();
        if (controlPointCount < 2) return points;

        bool isOpen = spline.isOpenEnded;
        int numSegments = isOpen ? controlPointCount - 1 : controlPointCount;

        for (int i = 0; i < pointCount; i++)
        {
            // Normalize progress t from 0 to 1 across the total shape
            float t = (float)i / (pointCount - 1);
            float totalProgress = t * numSegments;

            int segmentIndex = Mathf.FloorToInt(totalProgress);
            if (segmentIndex >= numSegments) segmentIndex = numSegments - 1;

            float segmentT = totalProgress - segmentIndex;

            int p0Index = segmentIndex;
            int p1Index = (segmentIndex + 1) % controlPointCount;

            // Get control points and tangents
            Vector3 p0 = spline.GetPosition(p0Index);
            Vector3 p1 = spline.GetPosition(p1Index);
            Vector3 p0Tangent = p0 + spline.GetRightTangent(p0Index);
            Vector3 p1Tangent = p1 + spline.GetLeftTangent(p1Index);

            // Calculate point along the Bezier curve
            Vector3 localPoint = CalculateCubicBezierPoint(p0, p0Tangent, p1Tangent, p1, segmentT);

            // Convert to World Space coordinates
            Vector3 worldPoint = spriteShapeController.transform.TransformPoint(localPoint);
            points.Add(worldPoint);
        }

        return points;
    }

    private Vector3 CalculateCubicBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 point = uuu * p0;
        point += 3f * uu * t * p1;
        point += 3f * u * tt * p2;
        point += ttt * p3;

        return point;
    }
}
