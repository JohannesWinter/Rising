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
    public float startDelay;
    public bool resetOnLevelStart;
    public bool stopInMenu;

    Vector3 startPosition;
    Vector3 startScale;
    Vector3 startRotation;

    [Header("MOVING")]
    public List<ObstacleMovementTarget> MOVING_movementTargets;
    public ObstacleTriggerType triggerType;
    public List<Collider2D> colliders;
    bool triggerd = false;
    bool singleUsed = false;
    int nextTargetEntry;
    ObstacleMovementTarget currentTarget;

    [Header("PUSHABLE")]
    public List<PushableLine> PUSH_pushableLines;
    float MinLineConnectDistance = 0.1f;

    [Header("AIR")]
    public AirFlow AIR_airFlow;
    float lastFixedUpdatePercentageAirStrength;

    [Header("PORTAL")]
    public PortalData PORTAL_portalData;


    bool stopped;
    float currentDelay;



    void Awake()
    {
        startPosition = transform.position;
        startScale = transform.localScale;
        startRotation = transform.rotation.eulerAngles;
        currentDelay = startDelay;
        if (agilityType == ObstacleAgilityType.Moving)
        {
            nextTargetEntry = 0;
        }
        for (int i = 0; i < PUSH_pushableLines.Count; i++)
        {
            PUSH_pushableLines[i].start += gameObject.transform.position;
            PUSH_pushableLines[i].end += gameObject.transform.position;
        }
        if (this.gameObject.GetComponent<Collider2D>() == null)
        {
            //Debug.LogWarning("Obsticle <" + this.gameObject.name + "> of <" + this.gameObject.transform.parent.name + "> has no correct Collider");
        }
        else if (collisionType == ObstacleCollisionType.Simple || collisionType == ObstacleCollisionType.Sharp)
        {
            if (this.gameObject.GetComponent<Collider2D>().isTrigger == true)
            {
                //Debug.LogWarning("Obsticle <" + this.gameObject.name + "> of <" + this.gameObject.transform.parent.name + "> has no correct Collider");
            }
        }
        else if (collisionType == ObstacleCollisionType.Air)
        {
            if (this.gameObject.GetComponent<Collider2D>().isTrigger == false)
            {
                //Debug.LogWarning("Obsticle <" + this.gameObject.name + "> of <" + this.gameObject.transform.parent.name + "> has no correct Collider");
            }
        }
        else if (collisionType == ObstacleCollisionType.Portal)
        {
            if (PORTAL_portalData.relativity == Relativity.Absolute)
            {
                PORTAL_portalData.position += new Vector2(transform.position.x, transform.position.y);
            }
        }
        if (rb == null)
        {
            rb = gameObject.GetComponent<Rigidbody2D>();
        }
        if (stopInMenu) stopped = true;
    }

    void FixedUpdate()
    {
        if (currentDelay > 0)
        {
            currentDelay -= Time.fixedDeltaTime;
            return;
        }
        if (agilityType == ObstacleAgilityType.Moving)
        {
            if (!stopped)
            {
                SetTarget();
                if (currentTarget != null) MoveSelf();
            }
            else
            {
                rb.velocity = Vector3.zero;
            }
        }
        if (agilityType == ObstacleAgilityType.Push)
        {
            if (!stopped)
            {
                CorrectPush();
            }
            else
            {
                rb.velocity = Vector3.zero;
            }
        }
        if (collisionType == ObstacleCollisionType.Air)
        {
            UpdateAirPush();
        }
    }

    public void ResetObstacle()
    {
        transform.position = startPosition;
        transform.localScale = startScale;
        transform.rotation = Quaternion.Euler(startRotation);
        triggerd = false;
        singleUsed = false;
        currentDelay = startDelay;
        if (agilityType == ObstacleAgilityType.Moving)
        {
            nextTargetEntry = 0;
            currentTarget = null;
        }
        if (collisionType == ObstacleCollisionType.Air)
        {
            lastFixedUpdatePercentageAirStrength = 0;
        }
    }
    public void Stop()
    {
        stopped = true;
    }
    public void Continue()
    {
        stopped = false;
    }

    void SetTarget()
    {
        if (currentTarget == null)
        {
            if (triggerType == ObstacleTriggerType.Repeat)
            {
                triggerd = true;
            }
            else if (triggerType == ObstacleTriggerType.Single)
            {
                if (singleUsed == false)
                {
                    singleUsed = true;
                    triggerd = true;
                }
            }
            else if (triggerType == ObstacleTriggerType.ColliderRepeat)
            {
                triggerd = CheckColliderList(colliders);
            }
            else if (triggerType == ObstacleTriggerType.ColliderSingle)
            {
                if (singleUsed == false && CheckColliderList(colliders))
                {
                    singleUsed = true;
                    triggerd = true;
                }
            }
            else if (triggerType == ObstacleTriggerType.ThirdParty)
            {
                //other component
            }
        }
        if (currentTarget == null && triggerd)
        {
            triggerd = false;
            string targetStr = JsonUtility.ToJson(MOVING_movementTargets[0]);
            currentTarget = JsonUtility.FromJson<ObstacleMovementTarget>(targetStr);
            currentTarget.startPosition = transform.position;
            currentTarget.startRotation = transform.rotation.eulerAngles;
            currentTarget.startScale = transform.localScale;
            currentTarget.remainingDuration = currentTarget.duration;
            nextTargetEntry = 1;
        }
        if (currentTarget != null && currentTarget.remainingDuration < 0)
        {
            transform.position = currentTarget.startPosition + currentTarget.relativePosition;
            transform.rotation = Quaternion.Euler(currentTarget.startRotation + currentTarget.relativeRotation);
            transform.localScale = currentTarget.startScale + currentTarget.relativeScale;

            if (nextTargetEntry >= MOVING_movementTargets.Count)
            {
                nextTargetEntry = 0;
                transform.position = startPosition;
                transform.rotation = Quaternion.Euler(startRotation);
                transform.localScale = startScale;
                currentTarget = null;
                rb.velocity = Vector3.zero;
            }
            else
            {
                string targetStr = JsonUtility.ToJson(MOVING_movementTargets[nextTargetEntry]);
                currentTarget = JsonUtility.FromJson<ObstacleMovementTarget>(targetStr);
                currentTarget.startPosition = transform.position;
                currentTarget.startRotation = transform.rotation.eulerAngles;
                currentTarget.startScale = transform.localScale;
                currentTarget.remainingDuration = currentTarget.duration;
                nextTargetEntry++;
            }
        }
    }

    public void MoveSelf()
    {
        Transform target = gameObject.transform;
        ObstacleMovementTarget movement = currentTarget;
        UpdateVelocity(target, movement);
        UpdateTorque(target, movement);
    }
    public void UpdateVelocity(
        Transform target,
        ObstacleMovementTarget movement)
    {
        float frameTime = Time.fixedDeltaTime;
        float percentageTimeSpent = (movement.duration - movement.remainingDuration) / movement.duration;
        
        float percentagePosition = Evaluate(percentageTimeSpent, movement.relativePositionMovementType, movement.relativePositionMovementTypeStrength);
        Vector3 aimPosition = movement.startPosition + movement.relativePosition * percentagePosition;

        movement.remainingDuration -= frameTime;
        
        float distance = (aimPosition - target.position).magnitude;
        float speed = distance * (1 / Time.fixedDeltaTime);
        rb.velocity = (aimPosition - target.transform.position) * speed;
    }
    public void UpdateTorque(Transform target, ObstacleMovementTarget movement)
    {
        float frameTime = Time.fixedDeltaTime;
        float percentageTimeSpent = (movement.duration - movement.remainingDuration) / movement.duration;

        float percentageRotation = Evaluate(percentageTimeSpent, movement.relativeRotationMovementType, movement.relativeRotationMovementTypeStrength);
        float aimRotation = (movement.startRotation.z + movement.relativeRotation.z * percentageRotation) % 360;

        movement.remainingDuration -= frameTime;

        float actualDistanceAngle = Mathf.DeltaAngle(
            target.transform.rotation.eulerAngles.z,
            aimRotation
        );

        float speed = actualDistanceAngle * (1 / Time.fixedDeltaTime);
        if (speed.Equals(float.NaN)) return;
        rb.angularVelocity = speed;
    }

    void CorrectPush()
    {
        if (PUSH_pushableLines.Count == 0)
        {
            return;
        }

        List<PushableLine> connected = new List<PushableLine>();
        PushableLine closestLine = null;
        Vector3 closestLinePoint = Vector3.zero;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < PUSH_pushableLines.Count; i++)
        {
            var distance = GetDistanceFromLine(transform.position, PUSH_pushableLines[i].start, PUSH_pushableLines[i].end);
            var currentClosestPoint = GetClosestPointOnLine(transform.position, PUSH_pushableLines[i].start, PUSH_pushableLines[i].end);

            var startEndRelativeVec = PUSH_pushableLines[i].end - PUSH_pushableLines[i].start;
            var startCurrentRelativeVec = currentClosestPoint - PUSH_pushableLines[i].start;

            var endStartRelativeVec = PUSH_pushableLines[i].start - PUSH_pushableLines[i].end;
            var endCurrentRelativeVec = currentClosestPoint - PUSH_pushableLines[i].end;

            if (Vector3.Dot(startEndRelativeVec, startCurrentRelativeVec) < 0)
            {
                distance = Vector3.Distance(PUSH_pushableLines[i].start, transform.position);
                currentClosestPoint = PUSH_pushableLines[i].start;
            }
            else if (Vector3.Dot(endStartRelativeVec, endCurrentRelativeVec) < 0)
            {
                distance = Vector3.Distance(PUSH_pushableLines[i].end, transform.position);
                currentClosestPoint = PUSH_pushableLines[i].end;
            }

            if (distance <= MinLineConnectDistance)
            {
                connected.Add(PUSH_pushableLines[i]);
            }
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestLinePoint = currentClosestPoint;
                closestLine = PUSH_pushableLines[i];
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

    void UpdateAirPush()
    {
        if (AIR_airFlow.AIR_currentPercentageAirStrength == lastFixedUpdatePercentageAirStrength)
            AIR_airFlow.AIR_currentPercentageAirStrength = 0;

        if (AIR_airFlow.AIR_currentPercentageAirStrength > 1)
            AIR_airFlow.AIR_currentPercentageAirStrength = 1;
        else if (AIR_airFlow.AIR_currentPercentageAirStrength < 0)
            AIR_airFlow.AIR_currentPercentageAirStrength = 0;

        //last
        lastFixedUpdatePercentageAirStrength = AIR_airFlow.AIR_currentPercentageAirStrength;
    }

    bool CheckColliderList(List<Collider2D> colliders)
    {
        ContactPoint2D[] contactBuffer = new ContactPoint2D[16];
        Collider2D[] overlapBuffer = new Collider2D[16];

        foreach (var col in colliders)
        {
            if (col == null)
                continue;

            int myLayer = col.gameObject.layer;

            int collisionMask = Physics2D.GetLayerCollisionMask(myLayer);

            int contactCount = col.GetContacts(contactBuffer);

            for (int i = 0; i < contactCount; i++)
            {
                Collider2D other = contactBuffer[i].collider;
                if (other == null)
                    continue;

                int otherLayer = other.gameObject.layer;

                if ((collisionMask & (1 << otherLayer)) != 0)
                    return true;
            }

            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.layerMask = collisionMask;
            filter.useTriggers = true;

            int overlapCount = col.OverlapCollider(filter, overlapBuffer);

            for (int i = 0; i < overlapCount; i++)
            {
                Collider2D other = overlapBuffer[i];

                if (other == null || other == col)
                    continue;

                return true;
            }
        }

        return false;
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
    Portal,
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
public enum Relativity
{
    Relative,
    Absolute,
}

public enum ObstacleTriggerType
{
    Repeat,
    Single,
    ColliderRepeat,
    ColliderSingle,
    ThirdParty,
}


[Serializable]
public class ObstacleMovementTarget
{
    public float duration;
    public Vector3 relativePosition;
    public ObstacleMovementType relativePositionMovementType;
    public float relativePositionMovementTypeStrength;
    public Vector3 relativeRotation;
    public ObstacleMovementType relativeRotationMovementType;
    public float relativeRotationMovementTypeStrength;
    public Vector3 relativeScale;
    public ObstacleMovementType relativeScaleMovementType;
    public float relativeScaleMovementTypeStrength;

    public Vector3 startPosition;
    public Vector3 startRotation;
    public Vector3 startScale;
    public float remainingDuration;
}

[Serializable]
public class PushableLine
{
    public Vector3 start;
    public Vector3 end;
}

[Serializable]
public class AirFlow
{
    public Vector2 AIR_force;
    public float AIR_variety;
    public float AIR_fullStrengthTime;
    public float AIR_currentPercentageAirStrength;
}
[Serializable]
public class PortalData
{
    public Vector2 position;
    public Relativity relativity;
    public float stunTimer;
}
