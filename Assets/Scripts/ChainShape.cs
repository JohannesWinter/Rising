using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class ChainShape : MonoBehaviour
{
    [Serializable]
    public struct PivotData
    {
        public Transform pivot;
        public float radius;
    }

    public enum SegmentAxis
    {
        PositiveX,
        NegativeX,
        PositiveY,
        NegativeY
    }

    [Serializable]
    public class SegmentPhysicsSettings
    {
        public bool applyRigidbodySettings = false;

        [Header("Rigidbody")]
        public float mass = 1f;
        public float drag = 0f;
        public float angularDrag = 0.05f;
        public float gravityScale = 1f;
        public RigidbodyInterpolation2D interpolation =
            RigidbodyInterpolation2D.Interpolate;
        public CollisionDetectionMode2D collisionDetection =
            CollisionDetectionMode2D.Discrete;

        [Header("Hinge Joint")]
        public bool applyHingeSettings = false;
        public bool useLimits = false;
        public JointAngleLimits2D limits;

        public bool useMotor = false;
        public JointMotor2D motor;

        public bool useSpring = false;
        public JointSuspension2D spring;

        public bool enableCollision = false;
        public float breakForce = Mathf.Infinity;
        public float breakTorque = Mathf.Infinity;
    }

    private class PathPoint
    {
        public Vector2 position;
        public float distance;

        public PathPoint(Vector2 position, float distance)
        {
            this.position = position;
            this.distance = distance;
        }
    }

    private class FreeSection
    {
        public float start;
        public float end;

        public int startPivot = -1;
        public int endPivot = -1;

        public float Length => end - start;
    }

    [Header("Chain")]
    [SerializeField] private SpriteShapeController spriteShape;
    [SerializeField] private GameObject segmentPrefab;
    [SerializeField] private int segmentCount = 20;

    [Tooltip("The local axis of the segment prefab which points along the chain.")]
    [SerializeField] private SegmentAxis segmentAxis = SegmentAxis.PositiveX;

    [Header("Pivots")]
    [SerializeField] private List<PivotData> pivots = new List<PivotData>();

    [Header("Spline Sampling")]
    [SerializeField] private int samplesPerSplineSegment = 20;

    [Header("Physics")]
    [SerializeField] private SegmentPhysicsSettings physicsSettings;

    [Header("Generated")]
    [SerializeField] private Transform generatedParent;

    private readonly List<GameObject> generatedSegments = new List<GameObject>();


    // ============================================================
    // PUBLIC GENERATION
    // ============================================================

    [ContextMenu("Generate Chain")]
    public void GenerateChain()
    {
        ClearGeneratedChain();

        if (!ValidateSetup())
            return;

        List<PathPoint> path = BuildSplinePath();

        if (path.Count < 2)
        {
            Debug.LogError("ChainShape: SpriteShape produced an invalid path.", this);
            return;
        }

        float totalLength = path[path.Count - 1].distance;

        List<int> sortedPivotIndices = SortPivotsAlongPath(path);

        List<FreeSection> freeSections =
            CalculateFreeSections(path, totalLength, sortedPivotIndices);

        if (freeSections.Count == 0)
        {
            Debug.LogWarning(
                "ChainShape: No free space exists for chain segments.",
                this
            );
            return;
        }

        int[] segmentDistribution =
            DistributeSegments(freeSections, segmentCount);

        Rigidbody2D previousBody = null;

        for (int sectionIndex = 0;
             sectionIndex < freeSections.Count;
             sectionIndex++)
        {
            FreeSection section = freeSections[sectionIndex];

            // If this section starts after a pivot, that pivot is the
            // first body this section should connect to.
            if (section.startPivot >= 0)
            {
                previousBody =
                    GetPivotRigidbody(sortedPivotIndices[section.startPivot]);
            }
            else
            {
                previousBody = null;
            }

            int amount = segmentDistribution[sectionIndex];

            for (int i = 0; i < amount; i++)
            {
                float t;

                if (amount == 1)
                    t = 0.5f;
                else
                    t = (float)(i + 1) / (amount + 1);

                float distance =
                    Mathf.Lerp(section.start, section.end, t);

                Vector2 position;
                Vector2 tangent;

                EvaluatePath(
                    path,
                    distance,
                    out position,
                    out tangent
                );

                GameObject segment =
                    InstantiateSegment(position, tangent);

                Rigidbody2D rb =
                    segment.GetComponent<Rigidbody2D>();

                if (rb == null)
                {
                    Debug.LogError(
                        "ChainShape: Segment prefab has no Rigidbody2D.",
                        segment
                    );

                    continue;
                }

                ConfigurePhysics(segment, rb);

                HingeJoint2D hinge =
                    segment.GetComponent<HingeJoint2D>();

                if (hinge != null)
                    hinge.connectedBody = previousBody;

                previousBody = rb;
            }
            try { GetPivotRigidbody(sortedPivotIndices[section.endPivot]).gameObject.GetComponent<HingeJoint2D>().connectedBody = previousBody; }
            catch { }

            // If this section ends at a pivot, the next free section
            // should start from that pivot rather than from the previous
            // segment.
            if (section.endPivot >= 0)
            {
                previousBody =
                    GetPivotRigidbody(sortedPivotIndices[section.endPivot]);
            }
        }

        Debug.Log(
            $"ChainShape: Generated {generatedSegments.Count} segments.",
            this
        );
    }


    [ContextMenu("Clear Generated Chain")]
    public void ClearGeneratedChain()
    {
        for (int i = generatedSegments.Count - 1; i >= 0; i--)
        {
            if (generatedSegments[i] == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(generatedSegments[i]);
            else
                Destroy(generatedSegments[i]);
#else
            Destroy(generatedSegments[i]);
#endif
        }

        generatedSegments.Clear();

        // Also clean up objects which might have been generated previously
        // but aren't in our list anymore.
        if (generatedParent != null)
        {
            for (int i = generatedParent.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(generatedParent.GetChild(i).gameObject);
                else
                    Destroy(generatedParent.GetChild(i).gameObject);
#else
                Destroy(generatedParent.GetChild(i).gameObject);
#endif
            }
        }
    }


    // ============================================================
    // SPLINE
    // ============================================================

    private List<PathPoint> BuildSplinePath()
    {
        List<PathPoint> result = new List<PathPoint>();

        Spline spline = spriteShape.spline;

        int pointCount = spline.GetPointCount();

        if (pointCount < 2)
            return result;

        float distance = 0f;

        Vector2 previousPosition =
            transform.TransformPoint(spline.GetPosition(0));

        result.Add(new PathPoint(previousPosition, 0f));

        for (int i = 0; i < pointCount - 1; i++)
        {
            Vector2 p0 =
                transform.TransformPoint(spline.GetPosition(i));

            Vector2 p1 =
                transform.TransformPoint(spline.GetPosition(i + 1));

            Vector2 rightTangent =
                transform.TransformVector(
                    spline.GetRightTangent(i)
                );

            Vector2 leftTangent =
                transform.TransformVector(
                    spline.GetLeftTangent(i + 1)
                );

            Vector2 p0Tangent = p0 + rightTangent;
            Vector2 p1Tangent = p1 + leftTangent;

            Vector2 previous = p0;

            int samples =
                Mathf.Max(2, samplesPerSplineSegment);

            for (int s = 1; s <= samples; s++)
            {
                float t = (float)s / samples;

                Vector2 current =
                    CubicBezier(
                        p0,
                        p0Tangent,
                        p1Tangent,
                        p1,
                        t
                    );

                distance += Vector2.Distance(previous, current);

                result.Add(
                    new PathPoint(current, distance)
                );

                previous = current;
            }
        }

        return result;
    }


    private Vector2 CubicBezier(
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        float t)
    {
        float u = 1f - t;

        return
            u * u * u * p0 +
            3f * u * u * t * p1 +
            3f * u * t * t * p2 +
            t * t * t * p3;
    }


    // ============================================================
    // PIVOTS
    // ============================================================

    private List<int> SortPivotsAlongPath(List<PathPoint> path)
    {
        List<int> indices = new List<int>();

        for (int i = 0; i < pivots.Count; i++)
        {
            if (pivots[i].pivot == null)
                continue;

            indices.Add(i);
        }

        indices.Sort((a, b) =>
        {
            float distanceA =
                FindClosestDistanceOnPath(
                    path,
                    pivots[a].pivot.position
                );

            float distanceB =
                FindClosestDistanceOnPath(
                    path,
                    pivots[b].pivot.position
                );

            return distanceA.CompareTo(distanceB);
        });

        return indices;
    }


    private float FindClosestDistanceOnPath(
        List<PathPoint> path,
        Vector2 position)
    {
        float closestDistance = 0f;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < path.Count; i++)
        {
            float sqrDistance =
                (path[i].position - position).sqrMagnitude;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestDistance = path[i].distance;
            }
        }

        return closestDistance;
    }


    private Rigidbody2D GetPivotRigidbody(int sortedPivotIndex)
    {
        Transform pivot =
            pivots[sortedPivotIndex].pivot;

        if (pivot == null)
            return null;

        Rigidbody2D rb =
            pivot.GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError(
                $"ChainShape: Pivot '{pivot.name}' has no Rigidbody2D.",
                pivot
            );
        }

        return rb;
    }


    // ============================================================
    // FREE SECTIONS
    // ============================================================

    private List<FreeSection> CalculateFreeSections(
        List<PathPoint> path,
        float totalLength,
        List<int> sortedPivotIndices)
    {
        List<FreeSection> sections =
            new List<FreeSection>();

        if (sortedPivotIndices.Count == 0)
        {
            sections.Add(
                new FreeSection
                {
                    start = 0f,
                    end = totalLength
                }
            );

            return sections;
        }

        // Calculate exclusion intervals for every pivot.
        List<(float start, float end, int pivot)> exclusions =
            new List<(float, float, int)>();

        for (int i = 0; i < sortedPivotIndices.Count; i++)
        {
            int pivotIndex =
                sortedPivotIndices[i];

            PivotData pivot =
                pivots[pivotIndex];

            float center =
                FindClosestDistanceOnPath(
                    path,
                    pivot.pivot.position
                );

            float start =
                FindCircleIntersectionDistance(
                    path,
                    pivot.pivot.position,
                    pivot.radius,
                    center,
                    -1
                );

            float end =
                FindCircleIntersectionDistance(
                    path,
                    pivot.pivot.position,
                    pivot.radius,
                    center,
                    1
                );

            exclusions.Add(
                (start, end, i)
            );
        }

        // Beginning of spline → first exclusion.
        if (exclusions[0].start > 0f)
        {
            sections.Add(
                new FreeSection
                {
                    start = 0f,
                    end = exclusions[0].start,
                    startPivot = -1,
                    endPivot = 0
                }
            );
        }

        // Spaces between exclusions.
        for (int i = 0; i < exclusions.Count - 1; i++)
        {
            float start =
                exclusions[i].end;

            float end =
                exclusions[i + 1].start;

            if (end > start)
            {
                sections.Add(
                    new FreeSection
                    {
                        start = start,
                        end = end,
                        startPivot = i,
                        endPivot = i + 1
                    }
                );
            }
        }

        // Last exclusion → end of spline.
        if (exclusions[exclusions.Count - 1].end < totalLength)
        {
            sections.Add(
                new FreeSection
                {
                    start =
                        exclusions[exclusions.Count - 1].end,

                    end = totalLength,

                    startPivot =
                        exclusions.Count - 1,

                    endPivot = -1
                }
            );
        }

        return sections;
    }


    private float FindCircleIntersectionDistance(
        List<PathPoint> path,
        Vector2 pivotPosition,
        float radius,
        float centerDistance,
        int direction)
    {
        float totalLength =
            path[path.Count - 1].distance;

        float step =
            totalLength /
            Mathf.Max(100f, path.Count * 2f);

        float current = centerDistance;

        for (int i = 0; i < 10000; i++)
        {
            current += step * direction;

            current =
                Mathf.Clamp(
                    current,
                    0f,
                    totalLength
                );

            Vector2 position;
            Vector2 tangent;

            EvaluatePath(
                path,
                current,
                out position,
                out tangent
            );

            float distance =
                Vector2.Distance(
                    position,
                    pivotPosition
                );

            if (distance > radius)
                return current;

            if (current <= 0f || current >= totalLength)
                return current;
        }

        return current;
    }


    // ============================================================
    // SEGMENT DISTRIBUTION
    // ============================================================

    private int[] DistributeSegments(
        List<FreeSection> sections,
        int totalSegments)
    {
        int[] result =
            new int[sections.Count];

        if (totalSegments <= 0)
            return result;

        float totalFreeLength = 0f;

        for (int i = 0; i < sections.Count; i++)
            totalFreeLength += sections[i].Length;

        if (totalFreeLength <= 0f)
            return result;

        // First assign the guaranteed integer portion.
        float[] exact =
            new float[sections.Count];

        int assigned = 0;

        for (int i = 0; i < sections.Count; i++)
        {
            exact[i] =
                totalSegments *
                sections[i].Length /
                totalFreeLength;

            result[i] =
                Mathf.FloorToInt(exact[i]);

            assigned += result[i];
        }

        // Give remaining segments to the sections with the
        // largest fractional remainders.
        int remaining =
            totalSegments - assigned;

        while (remaining > 0)
        {
            int bestIndex = -1;
            float bestFraction = -1f;

            for (int i = 0; i < sections.Count; i++)
            {
                float fraction =
                    exact[i] - result[i];

                if (fraction > bestFraction)
                {
                    bestFraction = fraction;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
                break;

            result[bestIndex]++;
            remaining--;
        }

        return result;
    }


    // ============================================================
    // PATH EVALUATION
    // ============================================================

    private void EvaluatePath(
        List<PathPoint> path,
        float distance,
        out Vector2 position,
        out Vector2 tangent)
    {
        distance =
            Mathf.Clamp(
                distance,
                0f,
                path[path.Count - 1].distance
            );

        for (int i = 0; i < path.Count - 1; i++)
        {
            if (distance <= path[i + 1].distance)
            {
                float segmentLength =
                    path[i + 1].distance -
                    path[i].distance;

                float t = 0f;

                if (segmentLength > 0f)
                {
                    t =
                        (distance - path[i].distance) /
                        segmentLength;
                }

                position =
                    Vector2.Lerp(
                        path[i].position,
                        path[i + 1].position,
                        t
                    );

                tangent =
                    (path[i + 1].position -
                     path[i].position).normalized;

                return;
            }
        }

        position =
            path[path.Count - 1].position;

        tangent =
            (path[path.Count - 1].position -
             path[path.Count - 2].position).normalized;
    }


    // ============================================================
    // SEGMENT CREATION
    // ============================================================

    private GameObject InstantiateSegment(
        Vector2 position,
        Vector2 tangent)
    {
        if (generatedParent == null)
            generatedParent = transform;

        Quaternion rotation =
            GetSegmentRotation(tangent);

        GameObject segment =
            Instantiate(
                segmentPrefab,
                position,
                rotation,
                generatedParent
            );

        generatedSegments.Add(segment);

        // Explicitly set the physics transform as well.
        // This prevents the initial pose from depending on the
        // first physics update.
        Rigidbody2D rb =
            segment.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.position = position;
            rb.rotation = rotation.eulerAngles.z;
        }

        return segment;
    }


    private Quaternion GetSegmentRotation(Vector2 tangent)
    {
        float angle =
            Mathf.Atan2(
                tangent.y,
                tangent.x
            ) * Mathf.Rad2Deg;

        switch (segmentAxis)
        {
            case SegmentAxis.PositiveX:
                return Quaternion.Euler(0f, 0f, angle);

            case SegmentAxis.NegativeX:
                return Quaternion.Euler(0f, 0f, angle + 180f);

            case SegmentAxis.PositiveY:
                return Quaternion.Euler(0f, 0f, angle - 90f);

            case SegmentAxis.NegativeY:
                return Quaternion.Euler(0f, 0f, angle + 90f);
        }

        return Quaternion.identity;
    }


    // ============================================================
    // PHYSICS
    // ============================================================

    private void ConfigurePhysics(
        GameObject segment,
        Rigidbody2D rb)
    {
        if (physicsSettings == null)
            return;

        if (physicsSettings.applyRigidbodySettings)
        {
            rb.mass =
                physicsSettings.mass;

            rb.drag =
                physicsSettings.drag;

            rb.angularDrag =
                physicsSettings.angularDrag;

            rb.gravityScale =
                physicsSettings.gravityScale;

            rb.interpolation =
                physicsSettings.interpolation;

            rb.collisionDetectionMode =
                physicsSettings.collisionDetection;
        }

        if (!physicsSettings.applyHingeSettings)
            return;

        HingeJoint2D hinge =
            segment.GetComponent<HingeJoint2D>();

        if (hinge == null)
            return;

        hinge.useLimits =
            physicsSettings.useLimits;

        hinge.limits =
            physicsSettings.limits;

        hinge.useMotor =
            physicsSettings.useMotor;

        hinge.motor =
            physicsSettings.motor;

        hinge.enableCollision =
            physicsSettings.enableCollision;

        hinge.breakForce =
            physicsSettings.breakForce;

        hinge.breakTorque =
            physicsSettings.breakTorque;
    }


    // ============================================================
    // VALIDATION
    // ============================================================

    private bool ValidateSetup()
    {
        if (spriteShape == null)
        {
            Debug.LogError(
                "ChainShape: No SpriteShapeController assigned.",
                this
            );

            return false;
        }

        if (segmentPrefab == null)
        {
            Debug.LogError(
                "ChainShape: No segment prefab assigned.",
                this
            );

            return false;
        }

        if (segmentCount <= 0)
        {
            Debug.LogError(
                "ChainShape: Segment count must be greater than zero.",
                this
            );

            return false;
        }

        if (spriteShape.spline.GetPointCount() < 2)
        {
            Debug.LogError(
                "ChainShape: SpriteShape spline needs at least two points.",
                this
            );

            return false;
        }

        return true;
    }
}