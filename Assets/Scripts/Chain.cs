using UnityEngine;

public class Chain : MonoBehaviour
{
    [Header("Anchors & Prefabs")]
    [Tooltip("The Rigidbody2D of the object the rope hangs from (e.g., moving ceiling/platform).")]
    public Rigidbody2D rootAnchor;

    [Tooltip("The segment prefab containing Rigidbody2D, Collider2D, and HingeJoint2D.")]
    public GameObject segmentPrefab;

    [Header("Rope Settings")]
    public int segmentCount = 10;
    public float segmentLength = 0.4f;

    [Header("Optional Bottom Payload")]
    [Tooltip("An object attached to the bottom end of the rope (e.g., a swinging lamp or weight).")]
    public Rigidbody2D endPayload;
    public bool useRelativePosition;
    public Vector2 relativePosition;

    void Start()
    {
        GenerateRope();
    }

    public void GenerateRope()
    {
        if (rootAnchor == null || segmentPrefab == null)
        {
            Debug.LogError("Rope Generator requires a Root Anchor and Segment Prefab!");
            return;
        }

        Rigidbody2D previousRb = rootAnchor;
        Vector3 lastSegmentCenter = Vector3.zero;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 spawnPos = rootAnchor.transform.position + (Vector3.down * (i + 1) * segmentLength);
            lastSegmentCenter = spawnPos;

            GameObject segment = Instantiate(segmentPrefab, spawnPos, Quaternion.identity, transform);
            segment.name = $"RopeSegment_{i}";

            Rigidbody2D currentRb = segment.GetComponent<Rigidbody2D>();
            HingeJoint2D joint = segment.GetComponent<HingeJoint2D>();

            joint.connectedBody = previousRb;
            joint.anchor = new Vector2(0, segmentLength / 2f);

            if (previousRb == rootAnchor)
            {
                joint.connectedAnchor = Vector2.zero;
            }
            else
            {
                joint.connectedAnchor = new Vector2(0, -segmentLength / 2f);
            }

            previousRb = currentRb;
        }

        // Attach payload
        if (endPayload != null)
        {
            // Calculate the actual bottom point of the last segment
            Vector3 lastSegmentBottom = lastSegmentCenter + (Vector3.down * (segmentLength / 2f));

            if (useRelativePosition)
            {
                endPayload.transform.position = lastSegmentBottom + (Vector3)relativePosition;
            }

            // Get existing HingeJoint2D or add a new one safely
            HingeJoint2D payloadJoint = endPayload.GetComponent<HingeJoint2D>();
            if (payloadJoint == null)
            {
                payloadJoint = endPayload.gameObject.AddComponent<HingeJoint2D>();
            }

            // Disable auto-configuration so manual anchors are respected
            payloadJoint.autoConfigureConnectedAnchor = false;

            payloadJoint.connectedBody = previousRb;
            payloadJoint.anchor = Vector2.zero; // Pivot on payload center

            // Connect to bottom of last segment (or relative offset position)
            if (useRelativePosition)
            {
                // Set the anchor relative to the last segment's space
                payloadJoint.connectedAnchor = new Vector2(0, -segmentLength / 2f) + relativePosition;
            }
            else
            {
                payloadJoint.connectedAnchor = new Vector2(0, -segmentLength / 2f);
            }
        }
    }
}