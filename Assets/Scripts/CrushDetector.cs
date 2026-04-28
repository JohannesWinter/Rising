using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class CrushDetector : MonoBehaviour
{
    [Header("Detection")]
    public float checkRadius = 0.1f;
    public LayerMask solidLayers;

    [Header("Crush Settings")]
    public float crushDepthThreshold = 0.15f;
    public int minColliderCount = 2;

    private Collider2D selfCollider;

    private readonly Collider2D[] results = new Collider2D[16];

    void Awake()
    {
        selfCollider = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            checkRadius,
            results,
            solidLayers
        );

        if (count == 0)
            return;
        float totalPenetration = 0f;
        int validHits = 0;

        for (int i = 0; i < count; i++)
        {
            Collider2D other = results[i];

            if (other == selfCollider)
                continue;

            ColliderDistance2D dist = Physics2D.Distance(selfCollider, other);

            if (dist.isOverlapped)
            {
                float penetrationDepth = -dist.distance;
                totalPenetration += penetrationDepth;
                validHits++;
            }
        }

        if (validHits >= minColliderCount && totalPenetration > crushDepthThreshold)
        {
            OnCrushed(totalPenetration, validHits);
        }
    }

    private void OnCrushed(float depth, int colliders)
    {
        //Debug.Log($"CRUSHED! Depth: {depth}, Colliders: {colliders}");
        if (Manager.m.gameplayManager.currentState == GameState.Running)
        {
            Manager.m.playerController.dead = true;   
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}