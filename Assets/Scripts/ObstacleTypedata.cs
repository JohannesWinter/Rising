using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Windows;
using UnityEngine.U2D;
using static UnityEditor.Experimental.GraphView.GraphView;

public class ObstacleTypedata : MonoBehaviour
{
    public ObstacleCollisionType collisionType;
    public ObstacleAgilityType agilityType;
    public ObstacleSpace obstacleSpace;
    public Rigidbody2D rb;
    public float startDelay;
    public bool resetOnLevelStart;
    public bool stopInMenu;

    Vector3 startPosition;
    Vector3 startScale;
    Vector3 startRotation;
    private Matrix4x4 initialLocalMatrix;

    Vector3 startAbsPosition;
    Vector3 startAbsPartentAbbrevation;

    [Header("MOVING")]
    public List<ObstacleMovementTarget> MOVING_movementTargets;
    public ObstacleTriggerType triggerType;
    public List<Collider2D> colliders;
    public bool triggerd = false;
    bool singleUsed = false;
    int nextTargetEntry;
    ObstacleMovementTarget currentTarget;
    public float speedMultiplier = 1;

    [Header("PUSHABLE")]
    public List<PushableLine> PUSH_pushableLines;
    public List<SpriteShapePushableLine> PUSH_spriteShapePushableLines;

    public bool chained;
    Vector2 parentAbsPosition = new Vector2(Mathf.Infinity, Mathf.Infinity);

    public Vector2 randomizedVelocityAddMin;
    public Vector2 randomizedVelocityAddMax;
    public float randomizedVelocityCooldownMin;
    public float randomizedVelocityCooldownMax;
    public float randomizedTorqueAddMin;
    public float randomizedTorqueAddMax;
    public float randomizedTorqueCooldownMin;
    public float randomizedTorqueCooldownMax;
    public float minAngularSpeed;
    public bool reverseTorqueOnSlowdown;
    public float reverseTorqueStart;
    public float reverseTorqueStrength;
    public bool followDirection;
    float angularSpeedLastFrame;
    float relativeRotation;
    bool setRelativeRotation = false;
    float MinLineConnectDistance = 0.1f;
    float randomizedVelocityCooldown;
    float randomizedTorqueCooldown;

    [Header("AIR")]
    public AirFlow AIR_airFlow;
    float lastFixedUpdatePercentageAirStrength;

    [Header("PORTAL")]
    public PortalData PORTAL_portalData;


    bool stopped;
    float currentDelay;



    void Awake()
    {
        if (collisionType == ObstacleCollisionType.Simple && agilityType == ObstacleAgilityType.Stuck) this.enabled = false;
        currentTarget = null;
        startAbsPosition = gameObject.transform.position;
        startAbsPartentAbbrevation = startAbsPosition - transform.parent.position;

        List<PushableLine> createdPushableLines = new List<PushableLine>();
        createdPushableLines.AddRange(ConvertSpriteShapePushableLinesToPushableLines(PUSH_spriteShapePushableLines));
        PUSH_pushableLines.AddRange(createdPushableLines);
        if (obstacleSpace == ObstacleSpace.World)
        {
            startPosition = transform.position;
            startScale = transform.localScale;
            startRotation = transform.rotation.eulerAngles;
        }
        else
        {
            startPosition = transform.localPosition;
            startScale = transform.localScale;
            startRotation = transform.localRotation.eulerAngles;
            initialLocalMatrix = Matrix4x4.TRS(startPosition, Quaternion.Euler(startRotation), startScale);
        }
        currentDelay = startDelay;
        if (agilityType == ObstacleAgilityType.Moving)
        {
            nextTargetEntry = 0;
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
                rb.angularVelocity = 0;
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
                rb.angularVelocity = 0;
            }
        }
        if (collisionType == ObstacleCollisionType.Air)
        {
            UpdateAirPush();
        }
    }

    public void ResetObstacle()
    {
        if (obstacleSpace == ObstacleSpace.World)
        {
            transform.position = startPosition;
            transform.rotation = Quaternion.Euler(startRotation);
        }
        else
        {
            transform.localPosition = startPosition;
            transform.localRotation = Quaternion.Euler(startRotation);
        }
        transform.localScale = startScale;
        triggerd = false;
        singleUsed = false;
        currentDelay = startDelay;
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = 0;
        }
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
            rb.velocity = Vector3.zero;
            rb.angularVelocity = 0;
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
            if (obstacleSpace == ObstacleSpace.World)
            {
                currentTarget.startPosition = transform.position;
                currentTarget.startRotation = transform.rotation.eulerAngles;
            }
            else
            {
                currentTarget.startPosition = transform.localPosition;
                currentTarget.startRotation = transform.localRotation.eulerAngles;
            }
            currentTarget.startScale = transform.localScale;
            currentTarget.remainingDuration = currentTarget.duration;
            if (currentTarget.targetingTypePosition == ObstacleValueType.RandomBetween)
            {
                Vector3 min = currentTarget.relativePosition;
                Vector3 max = currentTarget._relativePosition;

                currentTarget.relativePosition = new Vector3(
                    UnityEngine.Random.Range(Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x)),
                    UnityEngine.Random.Range(Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y)),
                    UnityEngine.Random.Range(Mathf.Min(min.z, max.z), Mathf.Max(min.z, max.z))
                );
            }
            if (currentTarget.targetingTypeRotation == ObstacleValueType.RandomBetween)
            {
                Vector3 min = currentTarget.relativeRotation;
                Vector3 max = currentTarget._relativeRotation;

                currentTarget.relativeRotation = new Vector3(
                    UnityEngine.Random.Range(Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x)),
                    UnityEngine.Random.Range(Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y)),
                    UnityEngine.Random.Range(Mathf.Min(min.z, max.z), Mathf.Max(min.z, max.z))
                );
            }
            nextTargetEntry = 1;
        }
        if (currentTarget != null && currentTarget.remainingDuration < 0)
        {
            if (obstacleSpace == ObstacleSpace.World)
            {
                transform.position = currentTarget.startPosition + currentTarget.relativePosition;
                transform.rotation = Quaternion.Euler(currentTarget.startRotation + currentTarget.relativeRotation);
            }
            else
            {
                transform.localPosition = currentTarget.startPosition + currentTarget.relativePosition;
                transform.localRotation = Quaternion.Euler(currentTarget.startRotation + currentTarget.relativeRotation);
            }
            transform.localScale = currentTarget.startScale + currentTarget.relativeScale;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = 0;

            if (nextTargetEntry >= MOVING_movementTargets.Count)
            {
                nextTargetEntry = 0;
                if (obstacleSpace == ObstacleSpace.World)
                {
                    transform.position = startPosition;
                    transform.rotation = Quaternion.Euler(startRotation);
                }
                else
                {
                    transform.localPosition = startPosition;
                    transform.localRotation = Quaternion.Euler(startRotation);
                }
                transform.localScale = startScale;
                currentTarget = null;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = 0;
            }
            else
            {
                string targetStr = JsonUtility.ToJson(MOVING_movementTargets[nextTargetEntry]);
                currentTarget = JsonUtility.FromJson<ObstacleMovementTarget>(targetStr);
                if (obstacleSpace == ObstacleSpace.World)
                {
                    currentTarget.startPosition = transform.position;
                    currentTarget.startRotation = transform.rotation.eulerAngles;
                }
                else
                {
                    currentTarget.startPosition = transform.localPosition;
                    currentTarget.startRotation = transform.localRotation.eulerAngles;
                }
                currentTarget.startScale = transform.localScale;
                if (currentTarget.durationType == ObstacleValueType.RandomBetween)
                {
                    float min = currentTarget.duration;
                    float max = currentTarget._duration;

                    currentTarget.duration = UnityEngine.Random.Range(
                        Mathf.Min(min, max),
                        Mathf.Max(min, max)
                    );
                }
                currentTarget.remainingDuration = currentTarget.duration;
                if (currentTarget.targetingTypePosition == ObstacleValueType.RandomBetween)
                {
                    Vector3 min = currentTarget.relativePosition;
                    Vector3 max = currentTarget._relativePosition;

                    currentTarget.relativePosition = new Vector3(
                        UnityEngine.Random.Range(Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x)),
                        UnityEngine.Random.Range(Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y)),
                        UnityEngine.Random.Range(Mathf.Min(min.z, max.z), Mathf.Max(min.z, max.z))
                    );
                }
                if (currentTarget.targetingTypeRotation == ObstacleValueType.RandomBetween)
                {
                    Vector3 min = currentTarget.relativeRotation;
                    Vector3 max = currentTarget._relativeRotation;

                    currentTarget.relativeRotation = new Vector3(
                        UnityEngine.Random.Range(Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x)),
                        UnityEngine.Random.Range(Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y)),
                        UnityEngine.Random.Range(Mathf.Min(min.z, max.z), Mathf.Max(min.z, max.z))
                    );
                }
                nextTargetEntry++;
            }
        }
    }

    public void MoveSelf()
    {
        Transform target = gameObject.transform;
        ObstacleMovementTarget movement = currentTarget;
        movement.remainingDuration -= Time.fixedDeltaTime * speedMultiplier;
        UpdateVelocity(target, movement);
        UpdateTorque(target, movement);
    }
    //public void UpdateVelocity(
    //    Transform target,
    //    ObstacleMovementTarget movement)
    //{
    //    float frameTime = Time.fixedDeltaTime;
    //    float percentageTimeSpent = (movement.duration - movement.remainingDuration) / movement.duration;
        
    //    float percentagePosition = Evaluate(percentageTimeSpent, movement.relativePositionMovementType, movement.relativePositionMovementTypeStrength);
    //    Vector3 aimPosition = movement.startPosition + movement.relativePosition * percentagePosition;

    //    movement.remainingDuration -= frameTime;
        
    //    float distance = (aimPosition - target.position).magnitude;
    //    float speed = distance * (1 / Time.fixedDeltaTime);
    //    rb.velocity = (aimPosition - target.transform.position) * speed;
    //}

    public void UpdateVelocity(
    Transform target,
    ObstacleMovementTarget movement)
    {
        float percentageTimeSpent = (movement.duration - movement.remainingDuration) / movement.duration;

        float percentagePosition = Evaluate(
            percentageTimeSpent,
            movement.relativePositionMovementType,
            movement.relativePositionMovementTypeStrength
        );

        Vector3 aimPosition = movement.startPosition + movement.relativePosition * percentagePosition;
        if (aimPosition.x.Equals(float.NaN) || aimPosition.y.Equals(float.NaN))
        {
            rb.velocity = Vector3.zero;
            return;
        }
        Physics.SyncTransforms();
        if (obstacleSpace == ObstacleSpace.World)
        {
            rb.MovePosition(aimPosition);
        }
        else
        {
            rb.MovePosition(transform.parent.TransformPoint(aimPosition));
        }
    }

    public void UpdateTorque(Transform target, ObstacleMovementTarget movement)
    {
        float percentageTimeSpent = (movement.duration - movement.remainingDuration) / movement.duration;

        float percentageRotation = Evaluate(
            percentageTimeSpent,
            movement.relativeRotationMovementType,
            movement.relativeRotationMovementTypeStrength
        );

        float aimRotation = (movement.startRotation.z + movement.relativeRotation.z * percentageRotation);

        if (aimRotation.Equals(float.NaN) || aimRotation.Equals(float.NaN))
        {
            rb.velocity = Vector3.zero;
            return;
        }
        if (obstacleSpace == ObstacleSpace.World)
        {
            rb.MoveRotation(aimRotation);
        }
        else
        {
            rb.MoveRotation(transform.parent.rotation * Quaternion.Euler(0,0,aimRotation));
        }
    }

    Vector3 GetWorldStart(PushableLine line)
    {
        if (obstacleSpace == ObstacleSpace.World)
        {
            return startAbsPosition + line.start;
        }

        Vector3 pointInParentSpace = initialLocalMatrix.MultiplyPoint3x4(line.start);

        return transform.parent != null
            ? transform.parent.TransformPoint(pointInParentSpace)
            : pointInParentSpace;
    }

    Vector3 GetWorldEnd(PushableLine line)
    {
        if (obstacleSpace == ObstacleSpace.World)
        {
            return startAbsPosition + line.end;
        }

        Vector3 pointInParentSpace = initialLocalMatrix.MultiplyPoint3x4(line.end);

        return transform.parent != null
            ? transform.parent.TransformPoint(pointInParentSpace)
            : pointInParentSpace;
    }

    void CorrectPush()
    {
        Vector2 delta = Vector2.zero;
        if (chained)
        {
            ObstacleTypedata parent = transform.parent != null
                ? transform.parent.GetComponentInParent<ObstacleTypedata>()
                : null;
            if (parent == null)
            {
                Debug.LogWarning("Chained parent not found for Obstacle <" + gameObject.name + ">");
            }
            else
            {
                Vector2 newParentAbsPosition = parent.gameObject.transform.position;
                if (parentAbsPosition.x != Mathf.Infinity)
                {
                    delta = newParentAbsPosition - parentAbsPosition;
                }
                parentAbsPosition = newParentAbsPosition;

                rb.MovePosition(rb.position + delta);
            }
        }

        if (PUSH_pushableLines.Count == 0) return;

        List<PushableLine> connected = new List<PushableLine>();
        PushableLine closestLine = null;
        Vector3 closestLinePoint = Vector3.zero;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < PUSH_pushableLines.Count; i++)
        {
            Vector3 currentStart = GetWorldStart(PUSH_pushableLines[i]);
            Vector3 currentEnd = GetWorldEnd(PUSH_pushableLines[i]);

            var distance = GetDistanceFromLine(transform.position, currentStart, currentEnd);
            var currentClosestPoint = GetClosestPointOnLine(transform.position, currentStart, currentEnd);

            var startEndRelativeVec = currentEnd - currentStart;
            var startCurrentRelativeVec = currentClosestPoint - currentStart;

            var endStartRelativeVec = currentStart - currentEnd;
            var endCurrentRelativeVec = currentClosestPoint - currentEnd;

            if (Vector3.Dot(startEndRelativeVec, startCurrentRelativeVec) < 0)
            {
                distance = Vector3.Distance(currentStart, transform.position);
                currentClosestPoint = currentStart;
            }
            else if (Vector3.Dot(endStartRelativeVec, endCurrentRelativeVec) < 0)
            {
                distance = Vector3.Distance(currentEnd, transform.position);
                currentClosestPoint = currentEnd;
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

        Vector3 closestStart = GetWorldStart(closestLine);
        Vector3 closestEnd = GetWorldEnd(closestLine);

        if (followDirection)
        {
            var direction = closestEnd - closestStart;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (!setRelativeRotation)
            {
                relativeRotation = gameObject.transform.rotation.eulerAngles.z - angle;
                setRelativeRotation = true;
            }
            else
            {
                gameObject.transform.rotation = Quaternion.Euler(
                    gameObject.transform.eulerAngles.x,
                    gameObject.transform.eulerAngles.y,
                    relativeRotation + angle
                );
            }
        }

        rb.position = closestLinePoint + new Vector3(delta.x, delta.y, 0);

        Vector3 forcePoint = closestLinePoint + (Vector3)rb.velocity;
        Vector3 linearAdjustedForcePoint = GetClosestPointOnLine(forcePoint, closestStart, closestEnd);

        var startEndRelativeVecAbs = closestEnd - closestStart;
        var endStartRelativeVecAbs = closestStart - closestEnd;

        var startCurrentRelativeVecPoint = transform.position - closestStart;
        var endCurrentRelativeVecPoint = transform.position - closestEnd;

        var startCurrentRelativeVecForce = forcePoint - closestStart;
        var endCurrentRelativeVecForce = forcePoint - closestEnd;

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
            rb.velocity = linearAdjustedForceRelative * (1 / Mathf.Pow(2, closestLine.drag));
        }

        if (randomizedVelocityAddMax.magnitude != 0)
        {
            if (randomizedVelocityCooldown <= 0)
            {
                randomizedVelocityCooldown = UnityEngine.Random.Range(randomizedVelocityCooldownMin, randomizedVelocityCooldownMax);
                rb.velocity += new Vector2(
                    UnityEngine.Random.Range(randomizedVelocityAddMin.x, randomizedVelocityAddMax.x),
                    UnityEngine.Random.Range(randomizedVelocityAddMin.y, randomizedVelocityAddMax.y)
                );
            }
            randomizedVelocityCooldown -= Time.deltaTime;
        }

        if (randomizedTorqueAddMax != 0)
        {
            if (randomizedTorqueCooldown <= 0)
            {
                randomizedTorqueCooldown = UnityEngine.Random.Range(randomizedTorqueCooldownMin, randomizedTorqueCooldownMax);
                rb.angularVelocity += UnityEngine.Random.Range(randomizedTorqueAddMin, randomizedTorqueAddMax);
            }
            randomizedTorqueCooldown -= Time.deltaTime;
        }

        if (reverseTorqueOnSlowdown)
        {
            if (Mathf.Abs(angularSpeedLastFrame) > reverseTorqueStart && Mathf.Abs(rb.angularVelocity) < reverseTorqueStart)
            {
                rb.angularVelocity = reverseTorqueStrength * Mathf.Sign(angularSpeedLastFrame) * -1;
            }
        }

        if (Mathf.Abs(rb.angularVelocity) < minAngularSpeed && Mathf.Abs(angularSpeedLastFrame) > Mathf.Abs(rb.angularVelocity))
        {
            rb.angularVelocity = 0;
        }
        angularSpeedLastFrame = rb.angularVelocity;
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
    public List <PushableLine> ConvertSpriteShapePushableLinesToPushableLines(List<SpriteShapePushableLine> spriteShapePushableLines)
    {
        List<PushableLine> allLines = new List<PushableLine>();
        for (int i = 0; i < spriteShapePushableLines.Count; i++)
        {
            var curLines = ConvertSpriteShapeToPushableLines(spriteShapePushableLines[i].tiling, spriteShapePushableLines[i].spriteShape, spriteShapePushableLines[i].drag);
            allLines.AddRange(curLines);
        }
        return allLines;
    }
    public List<PushableLine> ConvertSpriteShapeToPushableLines(int tiling, SpriteShapeController spriteShapeController, float drag)
    {
        List<PushableLine> pushableLines = new List<PushableLine>();
        List<Vector3> relativePoints = GetPathPointsRelativeTo(this.gameObject.transform, tiling + 1, spriteShapeController);
        for (int i = 0; i < relativePoints.Count - 1; i++)
        {
            PushableLine newLine = new PushableLine();
            newLine.start = relativePoints[i];
            newLine.end = relativePoints[i + 1];
            newLine.drag = drag;
            pushableLines.Add(newLine);
        }
        return pushableLines;
    }

    public List<Vector3> GetPathPointsRelativeTo(Transform targetTransform, int pointCount, SpriteShapeController spriteShapeController)
    {
        List<Vector3> relativePoints = new List<Vector3>();
        if (targetTransform == null) return relativePoints;

        List<Vector3> worldPoints = GetPathPoints(pointCount, spriteShapeController);

        foreach (Vector3 worldPoint in worldPoints)
        {
            if (obstacleSpace == ObstacleSpace.World)
            {
                Vector3 worldOffset = worldPoint - startAbsPosition;
                relativePoints.Add(worldOffset);
            }
            else
            {
                Vector3 localPoint = targetTransform.InverseTransformPoint(worldPoint);
                relativePoints.Add(localPoint);
            }
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
            float t = (float)i / (pointCount - 1);
            float totalProgress = t * numSegments;

            int segmentIndex = Mathf.FloorToInt(totalProgress);
            if (segmentIndex >= numSegments) segmentIndex = numSegments - 1;

            float segmentT = totalProgress - segmentIndex;

            int p0Index = segmentIndex;
            int p1Index = (segmentIndex + 1) % controlPointCount;

            Vector3 p0 = spline.GetPosition(p0Index);
            Vector3 p1 = spline.GetPosition(p1Index);
            Vector3 p0Tangent = p0 + spline.GetRightTangent(p0Index);
            Vector3 p1Tangent = p1 + spline.GetLeftTangent(p1Index);

            Vector3 localPoint = CalculateCubicBezierPoint(p0, p0Tangent, p1Tangent, p1, segmentT);

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
public enum ObstacleSpace
{
    World,
    Local
}
public enum ObstacleValueType
{
    Set,
    RandomBetween,
}


[Serializable]
public class ObstacleMovementTarget
{
    [Header("General")]
    public ObstacleValueType durationType;
    public float duration;
    public float _duration;

    [Header("Position")]
    public ObstacleValueType targetingTypePosition;
    public ObstacleMovementType relativePositionMovementType;
    public Vector3 relativePosition;
    public Vector3 _relativePosition;
    public float relativePositionMovementTypeStrength;

    [Header("Rotation")]
    public ObstacleValueType targetingTypeRotation;
    public ObstacleMovementType relativeRotationMovementType;
    public Vector3 relativeRotation;
    public Vector3 _relativeRotation;
    public float relativeRotationMovementTypeStrength;

    //[Header("Scale")]
    [HideInInspector]
    public ObstacleValueType targetingTypeScale;
    [HideInInspector]
    public ObstacleMovementType relativeScaleMovementType;
    [HideInInspector]
    public Vector3 relativeScale;
    [HideInInspector]
    public Vector3 _relativeScale;
    [HideInInspector]
    public float relativeScaleMovementTypeStrength;

    [HideInInspector]
    public Vector3 startPosition;
    [HideInInspector]
    public Vector3 startRotation;
    [HideInInspector]
    public Vector3 startScale;
    [HideInInspector]
    public float remainingDuration;
}

[Serializable]
public class PushableLine
{
    public Vector3 start;
    public Vector3 end;
    public float drag = 0.1f;
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
[Serializable]
public class SpriteShapePushableLine
{
    public SpriteShapeController spriteShape;
    public int tiling;
    public float drag;
}
