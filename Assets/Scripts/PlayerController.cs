using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float currentGeneralSpeed;
    public bool dead;
    public GameObject playerObject;
    public GameObject playerAnimationBody;
    public Rigidbody2D rb;
    public float minSideGap;
    public float minTopGap;
    public float minBotGap;
    Transform playerTransform;
    Resolution res;
    float viewsizeX;
    float viewsizeY;
    public float maxForceDistance;
    public float maxSpeedDistance;
    public float baseHitboxSize;
    Vector2 mousePos = Vector2.zero;
    List<AirPushVector> airPushVectors = new List<AirPushVector>();
    int airPushVectorsLengthLastFrame = 0;
    float windNoiseSpeed = 0.5f;
    float windNoiseAmplitude = 0.5f;
    float windNoiseSeed = 787;
   

    float stunTimer;

    Vector3 targetPos;

    Vector3 cameraNormalPosition = Vector3.zero;
    Quaternion cameraNormalRotation;
    List<Vector3Wrapper> cameraShakeOffsets = new List<Vector3Wrapper>();
    List<Vector3Wrapper> cameraRotationShakeOffsets = new List<Vector3Wrapper>();

    [Header("Debug")]
    public bool debug_shake;
    public bool invulnerable;
    public bool untouchable;
    // Start is called before the first frame update
    void Start()
    {
        cameraNormalPosition = Manager.m.playerCameraObj.transform.localPosition;
        cameraNormalRotation = Manager.m.playerCameraObj.transform.localRotation;
        res = Screen.currentResolution;
        playerTransform = playerObject.transform;
        viewsizeX = Manager.m.playerCamera.orthographicSize * Manager.m.playerCamera.aspect;
        viewsizeY = Manager.m.playerCamera.orthographicSize;
        ResetPlayerHitbox();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTarget();
    }

    void FixedUpdate()
    {
        if (Manager.m.gameplayManager.currentState == GameState.Resetting)
        {
            this.GetComponent<Collider2D>().enabled = false;
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }
        else
        {
            this.GetComponent<Collider2D>().enabled = true;
            rb.isKinematic = false;
        }
        MoveToTarget();
        UpdateCameraMovement();
        airPushVectorsLengthLastFrame = airPushVectors.Count;
    }

    void UpdateTarget()
    {
        if (untouchable)
        {
            playerObject.layer = LayerMask.NameToLayer("Empty");
        }
        if (Manager.m.gameplayManager.currentState == GameState.Stopped || Manager.m.gameplayManager.currentState == GameState.Resetting || stunTimer > 0)
        {
            return;
        }
        if (!Input.GetButton("Fire1") && false) //debugging
        {
            targetPos = playerTransform.localPosition;
            return;
        }
        mousePos = Input.mousePosition;
        float currentPosX = -viewsizeX;
        float toRight = viewsizeX * 2;
        float toAdd = Mathf.Min(1, mousePos.x / res.height);
        currentPosX += toRight * toAdd;
        if (currentPosX < -viewsizeX + toRight * minSideGap)
        {
            currentPosX = -viewsizeX + toRight * minSideGap;
        }
        else if (currentPosX > viewsizeX - toRight * minSideGap)
        {
            currentPosX = viewsizeX - toRight * minSideGap;
        }

        float currentPosY = -viewsizeY;
        float toDown = viewsizeY * 2;
        float toAddUp = Mathf.Min(1, mousePos.y / res.width);
        currentPosY += toDown * toAddUp;
        if (currentPosY < -viewsizeY + toDown * minBotGap)
        {
            currentPosY = -viewsizeY + toDown * minBotGap;
        }
        else if (currentPosY > viewsizeY - toDown * minTopGap)
        {
            currentPosY = viewsizeY - toDown * minTopGap;
        }

        targetPos = new Vector2(currentPosX, currentPosY);
    }
    void MoveToTarget()
    {
        if (Manager.m.gameplayManager.currentState != GameState.Stopped && Manager.m.gameplayManager.currentState != GameState.Resetting && stunTimer <= 0)
        {
            Vector3 adjustedTargetPos = targetPos + Manager.m.playerCameraSpace.transform.localPosition;
            Vector2 adjustedAbsTargetPos = targetPos + Manager.m.playerCameraSpace.transform.position;

            Vector2 currentAirPush = Vector2.zero;

            for (int i = airPushVectors.Count - 1; i >= 0; i--)
            {
                var push = airPushVectors[i];
                var collider = push.source.GetComponent<Collider2D>();

                if (collider.OverlapPoint(adjustedAbsTargetPos + push.vector + push.vector))
                {
                    currentAirPush += push.vector * push.currentStrength;
                }
                else
                {
                    var actualPosition = collider.ClosestPoint(adjustedAbsTargetPos + push.vector);
                    currentAirPush += (actualPosition - adjustedAbsTargetPos) * 0.6f;
                }
                if (push.frameCounter <= 0) airPushVectors.RemoveAt(i);
                push.frameCounter--;
            }
            if (currentAirPush.magnitude > 0.01f)
            {
                adjustedTargetPos += new Vector3(
                    (Mathf.PerlinNoise1D(windNoiseSeed + Time.time * windNoiseSpeed) - 0.5f),
                    (Mathf.PerlinNoise1D(windNoiseSeed * windNoiseSeed + Time.time * windNoiseSpeed) - 0.5f)
                    ) * windNoiseAmplitude;
                playerAnimationBody.GetComponent<ShakeObject>().intensity = 0.05f;
            }
            else
            {
                playerAnimationBody.GetComponent<ShakeObject>().intensity = 0f;
            }
            Vector3 currentAirPushVec3 = currentAirPush;

            adjustedTargetPos += currentAirPushVec3;
            Vector3 adjustedRelativeTargetPos = adjustedTargetPos - playerTransform.localPosition;
            adjustedRelativeTargetPos.z = 0;

            RaycastHit2D playerToCursor = Physics2D.Raycast(playerObject.transform.position, adjustedRelativeTargetPos, adjustedRelativeTargetPos.magnitude, Physics2D.GetLayerCollisionMask(3)); // 3 == playerlayer
            if (playerToCursor.collider != null)
            {
                float hitDistance = playerToCursor.distance;
                float currentMaxForceDistance = hitDistance + maxForceDistance;
                if (currentMaxForceDistance < adjustedRelativeTargetPos.magnitude)
                {
                    adjustedRelativeTargetPos = adjustedRelativeTargetPos.normalized * currentMaxForceDistance;
                }
            }
            if (adjustedRelativeTargetPos.magnitude > maxSpeedDistance)
            {
                adjustedRelativeTargetPos = adjustedRelativeTargetPos.normalized * maxSpeedDistance;
            }

            Vector2 targetVelocity = adjustedRelativeTargetPos / Time.fixedDeltaTime;
            Vector2 velocityChange = targetVelocity - rb.velocity;

            float decellerationStart = 100f;
            float decellerationStop = 30f;
            float maxSpeedAgainstCurrent = 2f;

            if (currentAirPush.sqrMagnitude > 0.001f)
            {
                Vector2 windDirection = currentAirPush.normalized;

                Vector2 parallelChange =
                    Vector2.Dot(velocityChange, windDirection) * windDirection;

                Vector2 perpendicularChange =
                    velocityChange - parallelChange;

                float windAlignment =
                    Vector2.Dot(velocityChange, windDirection);

                float movementAlignment =
                    Vector2.Dot(rb.velocity, windDirection);

                bool newVelocityHigher =
                    targetVelocity.magnitude > rb.velocity.magnitude;

                if (windAlignment < 0f && newVelocityHigher)
                {
                    parallelChange /= Mathf.Max(1f, currentAirPush.magnitude)
                                      * decellerationStart;

                    if (rb.velocity.magnitude > maxSpeedAgainstCurrent &&
                        movementAlignment < 0f)
                    {
                        parallelChange = Vector2.zero;
                    }
                }
                else if (windAlignment < 0f && newVelocityHigher == false)
                {
                    if (parallelChange.magnitude >
                        currentAirPush.magnitude * Time.fixedDeltaTime * decellerationStop)
                    {
                        parallelChange *=
                            Mathf.Min(1f, decellerationStop * Time.fixedDeltaTime);
                    }
                    else
                    {
                        parallelChange /= Mathf.Max(1f, currentAirPush.magnitude)
                                          * decellerationStart;
                    }
                }

                velocityChange = parallelChange + perpendicularChange;
            }

            rb.velocity += velocityChange;

            if (airPushVectorsLengthLastFrame == 0 && airPushVectors.Count > 0)
            {
                rb.velocity = Vector2.zero;
            }
        }
        else
        {
            stunTimer -= Time.fixedDeltaTime;
            if (stunTimer < 0)
                stunTimer = 0;
            rb.velocity = Vector2.zero;
        }
    }

void UpdateCameraMovement()
    {
        Manager.m.playerCameraSpace.transform.Translate(
            Vector3.up * Time.fixedDeltaTime * currentGeneralSpeed
        );

        Vector3 totalCameraOffset = Vector3.zero;

        foreach (Vector3Wrapper offset in cameraShakeOffsets)
            totalCameraOffset += offset.vector;

        Manager.m.playerCameraObj.transform.localPosition =
            cameraNormalPosition + totalCameraOffset;


        Vector3 totalRotationOffset = Vector3.zero;

        foreach (Vector3Wrapper offset in cameraRotationShakeOffsets)
            totalRotationOffset += offset.vector;

        Manager.m.playerCameraObj.transform.localRotation =
            cameraNormalRotation *
            Quaternion.Euler(totalRotationOffset);
    }

    public void CameraShake(
        float intensity,
        float duration,
        float frequenzy,
        float decreasePower = 0,
        float rotationalIntensity = 0,
        float rotationalDuration = 0,
        float rotationalFrequenzy = 0)
    {
        StartCoroutine(ExecuteCameraShake(
            intensity,
            duration,
            frequenzy,
            decreasePower,
            rotationalIntensity,
            rotationalDuration,
            rotationalFrequenzy
        ));
    }

    IEnumerator ExecuteCameraShake(
        float intensity,
        float duration,
        float frequency,
        float decreasePower,
        float rotationalIntensity,
        float rotationalDuration,
        float rotationalFrequency)
    {
        if (duration <= 0 || decreasePower < 0 || intensity == 0)
        {
            yield break;
        }

        float elapsed = 0f;

        float seedX = UnityEngine.Random.value * 1000f;
        float seedY = UnityEngine.Random.value * 1000f;
        float seedRotation = UnityEngine.Random.value * 1000f;

        Vector3Wrapper shake = Vector3Wrapper.zero();
        cameraShakeOffsets.Add(shake);

        Vector3Wrapper rotationShake = Vector3Wrapper.zero();

        if (rotationalIntensity != 0 && rotationalDuration > 0)
        {
            cameraRotationShakeOffsets.Add(rotationShake);
        }

        while (elapsed < duration)
        {
            float percentageLeft =
                1f - Mathf.Clamp01(elapsed / duration);

            float strength =
                Mathf.Pow(percentageLeft, decreasePower + 1);

            // Translation
            float x =
                (Mathf.PerlinNoise(seedX, elapsed * frequency) - 0.5f) * 2f;

            float y =
                (Mathf.PerlinNoise(seedY, elapsed * frequency) - 0.5f) * 2f;

            shake.vector =
                new Vector3(x, y, 0f) * intensity * strength;


            // Rotation
            if (rotationalIntensity != 0 &&
                elapsed < rotationalDuration)
            {
                float rotationalPercentageLeft =
                    1f - Mathf.Clamp01(elapsed / rotationalDuration);

                float rotationalStrength =
                    Mathf.Pow(
                        rotationalPercentageLeft,
                        decreasePower + 1
                    );

                float rotation =
                    (Mathf.PerlinNoise(
                        seedRotation,
                        elapsed * rotationalFrequency
                    ) - 0.5f) * 2f;

                rotationShake.vector =
                    new Vector3(
                        0f,
                        0f,
                        rotation * rotationalIntensity * rotationalStrength
                    );
            }
            else
            {
                rotationShake.vector = Vector3.zero;
            }

            elapsed += Time.deltaTime;

            yield return null;
        }

        cameraShakeOffsets.Remove(shake);

        if (rotationalIntensity != 0 && rotationalDuration > 0)
        {
            cameraRotationShakeOffsets.Remove(rotationShake);
        }
    }

    void OnTriggerEnter(Collider collision)
    {
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.gameObject.GetComponent<ObstacleTypedata>())
        {
            HandleObstacleCollision(collision.collider.gameObject.GetComponent<ObstacleTypedata>());
        }
    }

    void HandleObstacleCollision(ObstacleTypedata obs)
    {
        if (Manager.m.gameplayManager.currentState == GameState.Running)
        {
            switch (obs.collisionType)
            {
                case ObstacleCollisionType.Simple:
                    break;
                case ObstacleCollisionType.Sharp:
                    dead = true;
                    break;
                case ObstacleCollisionType.Air:
                    break;
                case ObstacleCollisionType.Portal:
                    if (obs.PORTAL_portalData.relativity == Relativity.Relative)
                        playerObject.transform.position = obs.gameObject.transform.position + new Vector3(obs.PORTAL_portalData.position.x, obs.PORTAL_portalData.position.y, 0);
                    else if (obs.PORTAL_portalData.relativity == Relativity.Absolute)
                        playerObject.transform.position = obs.PORTAL_portalData.position;

                    if (obs.PORTAL_portalData.stunTimer > stunTimer)
                        stunTimer = obs.PORTAL_portalData.stunTimer;

                    break;
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<ObstacleTypedata>())
        {
            HandleObstacleTrigger(collision.gameObject.GetComponent<ObstacleTypedata>());
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<ObstacleTypedata>())
        {
            HandleObstacleTriggerStay(collision.gameObject.GetComponent<ObstacleTypedata>());
        }
    }
    void HandleObstacleTrigger(ObstacleTypedata obs)
    {
        if (Manager.m.gameplayManager.currentState == GameState.Running)
        {
            switch (obs.collisionType)
            {
                case ObstacleCollisionType.Simple:
                    break;
                case ObstacleCollisionType.Sharp:
                    dead = true;
                    break;
                case ObstacleCollisionType.Air:
                    break;
                case ObstacleCollisionType.Portal:
                    if (obs.PORTAL_portalData.relativity == Relativity.Relative)
                        playerObject.transform.position = obs.gameObject.transform.position + new Vector3(obs.PORTAL_portalData.position.x, obs.PORTAL_portalData.position.y, 0);
                    else if (obs.PORTAL_portalData.relativity == Relativity.Absolute)
                        playerObject.transform.position = obs.PORTAL_portalData.position;

                    if (obs.PORTAL_portalData.stunTimer > stunTimer)
                        stunTimer = obs.PORTAL_portalData.stunTimer;

                    break;
            }
        }
    }

    public void HandleObstacleTriggerStay(ObstacleTypedata obs)
    {
        if (Manager.m.gameplayManager.currentState == GameState.Running)
        {
            switch (obs.collisionType)
            {
                case ObstacleCollisionType.Simple:
                    break;
                case ObstacleCollisionType.Sharp:
                    break;
                case ObstacleCollisionType.Air:
                    HandleAirTrigger(obs);
                    break;
                case ObstacleCollisionType.Portal:
                    break;
            }
        }
        else
        {
            switch (obs.collisionType)
            {
                case ObstacleCollisionType.Simple:
                    break;
                case ObstacleCollisionType.Sharp:
                    break;
                case ObstacleCollisionType.Air:
                    HandleAirTrigger(obs);

                    break;
                case ObstacleCollisionType.Portal:
                    break;
            }
        }
    }

    void HandleAirTrigger(ObstacleTypedata obs)
    {
        AirPushVector currentPush = new AirPushVector();
        bool isNew = true;
        foreach (var push in airPushVectors)
        {
            if (push.source.Equals(obs))
            {
                currentPush = push;
                isNew = false;
                break;
            }
        }
        if (obs.AIR_airFlow.AIR_fullStrengthTime > 0) currentPush.currentStrength += Time.fixedDeltaTime / obs.AIR_airFlow.AIR_fullStrengthTime;
        else currentPush.currentStrength = 1;

        currentPush.currentStrength = Mathf.Min(currentPush.currentStrength, 1);

        currentPush.vector = obs.AIR_airFlow.AIR_force;
        //currentPush.vector = obs.AIR_airFlow.AIR_force * currentPush.currentStrength +
        //    obs.AIR_airFlow.AIR_force * RandomOf(new float[] { -1, 1 }) * UnityEngine.Random.Range(0, obs.AIR_airFlow.AIR_variety) * currentPush.currentStrength;

        currentPush.frameCounter = 1;
        if (isNew)
        {
            currentPush.source = obs;
            airPushVectors.Add(currentPush);
        }
    }

    public void UpdatePlayerHitbox(float size)
    {
        if (playerObject == null) return;
        playerObject.GetComponent<CircleCollider2D>().radius = size;
    }
    public void ResetPlayerHitbox()
    {
        if (playerObject == null) return;
        playerObject.GetComponent<CircleCollider2D>().radius = baseHitboxSize;
    }

    static float RandomOf(float[] randoms) //returns random number in Array
    {
        return randoms[UnityEngine.Random.Range(0, randoms.Length)];
    }
    private class Vector3Wrapper
    {
        public Vector3Wrapper() { }
        public Vector3Wrapper(Vector3 vector)
        {
            this.vector = vector;
        }
        public Vector3Wrapper(float x, float y, float z)
        {
            this.vector = new Vector3(x, y, z);
        }
        public Vector3 vector;
        public static Vector3Wrapper zero()
        {
            return new Vector3Wrapper();
        }
    }

    private class AirPushVector
    {
        public ObstacleTypedata source;
        public Vector2 vector;
        public float currentStrength;
        public int frameCounter;
    }
}