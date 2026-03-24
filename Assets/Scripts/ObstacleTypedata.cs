using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Windows;

public class ObstacleTypedata : MonoBehaviour
{
    public ObstacleCollisionType collisionType;
    public ObstacleAgilityType agilityType;
    public Rigidbody2D rb;

    Vector3 startPosition;
    Vector3 startScale;
    Vector3 startRotation;

    public List<ObstacleMovementTarget> movementTargets;
    public List<PushableLine> pushableLines;

    int nextTargetEntry;
    Vector3 aimPosition = Vector3.zero;

    ObstacleMovementTarget currentTarget;

    float MinLineConnectDistance = 0.1f;

    void Start()
    {
        if (agilityType == ObstacleAgilityType.Moving)
        {
            startPosition = transform.position;
            startScale = transform.localScale;
            startRotation = transform.rotation.eulerAngles;
            currentTarget = movementTargets[0];
            nextTargetEntry = 0;
        }
        for (int i = 0; i < pushableLines.Count; i++)
        {
            pushableLines[i].start += gameObject.transform.position;
            pushableLines[i].end += gameObject.transform.position;
        }
        if (this.gameObject.GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning("Obsticle <" + this.gameObject.name + "> of <" + this.gameObject.transform.parent.name + "> has no correct Collider");
        }
        else if (collisionType == ObstacleCollisionType.Simple || collisionType == ObstacleCollisionType.Sharp)
        {
            if (this.gameObject.GetComponent<Collider2D>().isTrigger == true)
            {
                Debug.LogWarning("Obsticle <" + this.gameObject.name + "> of <" + this.gameObject.transform.parent.name + "> has no correct Collider");
            }
        }
        else if (collisionType == ObstacleCollisionType.Air)
        {
            if (this.gameObject.GetComponent<Collider2D>().isTrigger == false)
            {
                Debug.LogWarning("Obsticle <" + this.gameObject.name + "> of <" + this.gameObject.transform.parent.name + "> has no correct Collider");
            }
        }
    }

    void FixedUpdate()
    {
        if (agilityType == ObstacleAgilityType.Moving)
        {
            SetTarget();
            MoveSelf();
        }
        if (agilityType == ObstacleAgilityType.Push)
        {
            CorrectPush();
        }
    }

    void SetTarget()
    {
        if (currentTarget == null || (currentTarget.remainingDuration <= 0))
        {
            if (nextTargetEntry >= movementTargets.Count)
            {
                nextTargetEntry = 0;
                transform.position = startPosition;
                transform.rotation = Quaternion.Euler(startRotation);
                transform.localScale = startScale;
            }
            string targetStr = JsonUtility.ToJson(movementTargets[nextTargetEntry]);
            currentTarget = JsonUtility.FromJson<ObstacleMovementTarget>(targetStr);
            currentTarget.startPosition = transform.position;
            currentTarget.startRotation = transform.rotation;
            currentTarget.startScale = transform.localScale;
            currentTarget.remainingDuration = currentTarget.duration;
            nextTargetEntry++;
        }
    }

    public void MoveSelf()
    {
        Transform target = gameObject.transform;
        ObstacleMovementTarget movement = currentTarget;
        float speed = UpdateMove(target, movement);
        rb.velocity = (aimPosition - target.transform.position) * speed;
    }
    public float UpdateMove(
        Transform target,
        ObstacleMovementTarget movement)
    {
        float frameTime = Time.fixedDeltaTime;
        float percentageTimeSpent = (movement.duration - movement.remainingDuration) / movement.duration;
        
        float percentagePosition = Evaluate(percentageTimeSpent, movement.relativePositionMovementType, movement.relativePositionMovementTypeStrength);
        aimPosition = movement.startPosition + movement.relativePosition * percentagePosition;

        movement.remainingDuration -= frameTime;
        
        float distance = (aimPosition - target.position).magnitude;
        float speed = distance * (1 / Time.fixedDeltaTime);
        return speed;
    }

    void CorrectPush()
    {
        if (pushableLines.Count == 0)
        {
            return;
        }

        List<PushableLine> connected = new List<PushableLine>();
        PushableLine closestLine = null;
        Vector3 closestLinePoint = Vector3.zero;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < pushableLines.Count; i++)
        {
            var distance = GetDistanceFromLine(transform.position, pushableLines[i].start, pushableLines[i].end);
            var currentClosestPoint = GetClosestPointOnLine(transform.position, pushableLines[i].start, pushableLines[i].end);

            var startEndRelativeVec = pushableLines[i].end - pushableLines[i].start;
            var startCurrentRelativeVec = currentClosestPoint - pushableLines[i].start;

            var endStartRelativeVec = pushableLines[i].start - pushableLines[i].end;
            var endCurrentRelativeVec = currentClosestPoint - pushableLines[i].end;

            if (Vector3.Dot(startEndRelativeVec, startCurrentRelativeVec) < 0)
            {
                distance = Vector3.Distance(pushableLines[i].start, transform.position);
                currentClosestPoint = pushableLines[i].start;
            }
            else if (Vector3.Dot(endStartRelativeVec, endCurrentRelativeVec) < 0)
            {
                distance = Vector3.Distance(pushableLines[i].end, transform.position);
                currentClosestPoint = pushableLines[i].end;
            }

            if (distance <= MinLineConnectDistance)
            {
                connected.Add(pushableLines[i]);
            }
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestLinePoint = currentClosestPoint;
                closestLine = pushableLines[i];
            }
        }
        gameObject.transform.position = closestLinePoint;

        Vector3 forcePoint = closestLinePoint + new Vector3(rb.velocity.x, rb.velocity.y, 0);

        Vector3 linearAdjustedForcePoint = GetClosestPointOnLine(forcePoint, closestLine.start, closestLine.end);

        var startEndRelativeVecAbs = closestLine.end - closestLine.start;
        var endStartRelativeVecAbs = closestLine.start - closestLine.end;

        var startCurrentRelativeVecPoint = transform.position - closestLine.start;
        var endCurrentRelativeVecPoint = transform.position - closestLine.end;

        var startCurrentRelativeVecForce = forcePoint - closestLine.start;
        var endCurrentRelativeVecForce = forcePoint - closestLine.end;

        bool pastStart = Vector3.Dot(startEndRelativeVecAbs, startCurrentRelativeVecPoint) < 0;
        bool movingPastStart = Vector3.Dot(startEndRelativeVecAbs, startCurrentRelativeVecForce) < 0;

        bool pastEnd = Vector3.Dot(endStartRelativeVecAbs, endCurrentRelativeVecPoint) < 0;
        bool movingPastEnd = Vector3.Dot(endStartRelativeVecAbs, endCurrentRelativeVecForce) < 0;

        if ((pastStart && movingPastStart) || (pastEnd && movingPastEnd))
        {
            rb.velocity = Vector3.zero;
        }
        else
        {
            Vector3 linearAdjustedForceRelative = linearAdjustedForcePoint - transform.position;
            rb.velocity = linearAdjustedForceRelative;
        }
    }

    Vector3 GetClosestPointOnLine(Vector3 point, Vector3 linePoint1, Vector3 linePoint2)
    {
        Vector3 u = linePoint2 - linePoint1;   // (B - A)
        Vector3 v = point - linePoint1;        // (M - A)

        float t = Vector3.Dot(v, u) / Vector3.Dot(u, u);

        Vector3 closestPoint = linePoint1 + t * u;

        return closestPoint;
    }

    float GetDistanceFromLine(Vector3 point, Vector3 linePoint1, Vector3 linePoint2)
    {
        Vector3 v = point - linePoint1;          // (M - A)
        Vector3 u = linePoint2 - linePoint1;     // (B - A)

        Vector3 cross = Vector3.Cross(v, u);     // (M - A) × (B - A)

        float distance = cross.magnitude / u.magnitude;

        return distance;
    }

    static float Evaluate(float t, ObstacleMovementType type, float strength)
    {
        strength = Mathf.Clamp01(strength);

        switch (type)
        {
            case ObstacleMovementType.Linear:
                return t;

            case ObstacleMovementType.Instant:
                return t > 0f ? 1f : 0f;

            case ObstacleMovementType.Delayed:
                return t < 1f ? 0f : 1f;

            case ObstacleMovementType.Increasing:
                return Mathf.Pow(t, Mathf.Lerp(1f, 6f, strength));

            case ObstacleMovementType.Decaying:
                return 1f - Mathf.Pow(1f - t, Mathf.Lerp(1f, 6f, strength));

            case ObstacleMovementType.Hyperbolic:
                float p = Mathf.Lerp(1f, 6f, strength);
                float a = Mathf.Pow(t, p);
                float b = Mathf.Pow(1f - t, p);
                return a / (a + b);

            default:
                return t;
        }
    }
}

public enum ObstacleCollisionType
{
    Simple,
    Sharp,
    Air,
}

public enum ObstacleAgilityType
{
    Stuck,
    Moving,
    Push,
}
public enum ObstacleMovementType
{
    Linear,
    Hyperbolic,
    Decaying,
    Increasing,
    Instant,
    Delayed,
}


[Serializable]
public class ObstacleMovementTarget
{
    public float duration;

    public Vector3 startPosition;
    public Quaternion startRotation;
    public Vector3 startScale;
    public float remainingDuration;
    public Vector3 relativePosition;
    public ObstacleMovementType relativePositionMovementType;
    public float relativePositionMovementTypeStrength;
    public Vector3 relativeRotation;
    public ObstacleMovementType relativeRotationMovementType;
    public float relativeRotationMovementTypeStrength;
    public Vector3 relativeScale;
    public ObstacleMovementType relativeScaleMovementType;
    public float relativeScaleMovementTypeStrength;

}

[Serializable]
public class PushableLine
{
    public Vector3 start;
    public Vector3 end;
}
