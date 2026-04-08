using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float currentGeneralSpeed;
    public bool dead;
    public GameObject playerObject;
    public Rigidbody2D rb;
    public float minSideGap;
    public float minTopGap;
    public float minBotGap;
    Transform playerTransform;
    Resolution res;
    float viewsizeX;
    float viewsizeY;
    Vector2 mousePos = Vector2.zero;
    Vector2 currentAirPush;

    float stunTimer;

    Vector3 targetPos;
    // Start is called before the first frame update
    void Start()
    {
        res = Screen.currentResolution;
        playerTransform = playerObject.transform;
        viewsizeX = Manager.m.playerCamera.orthographicSize * Manager.m.playerCamera.aspect;
        viewsizeY = Manager.m.playerCamera.orthographicSize;
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
    }

    void UpdateTarget()
    {
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
            Vector3 adjustedTargetPos = targetPos + Manager.m.playerCamera.transform.localPosition;
            Vector3 targetVelocity = (adjustedTargetPos - playerTransform.localPosition) / Time.fixedDeltaTime;

            rb.velocity = targetVelocity + Vector3.up * currentGeneralSpeed;
            rb.velocity += currentAirPush;
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
        Manager.m.playerCamera.gameObject.transform.Translate(Vector3.up * Time.fixedDeltaTime * currentGeneralSpeed);
    }

    //void CollisionDetection()
    //{
    //    currentAirPush = Vector2.zero;
    //    Collider2D[] col = new Collider2D[5]; //5 = max detectable colliders
    //    ContactFilter2D filter = new ContactFilter2D();
    //    filter.useTriggers = true;
    //    Physics2D.OverlapCollider(GetComponent<CircleCollider2D>(), filter, col);
    //    for (int i = 0; i < col.Length; i++)
    //    {
    //        if (col[i] != null)
    //        {
    //            if (col[i].gameObject.GetComponent<ObstacleTypedata>())
    //            {
    //                HandleObstacleCollision(col[i].gameObject.GetComponent<ObstacleTypedata>());
    //            } 
    //        }
    //        else
    //        {
    //            break;
    //        }
    //    }
    //}

    void OnTriggerEnter(Collider collision){
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<ObstacleTypedata>())
        {
            HandleObstacleCollision(collision.gameObject.GetComponent<ObstacleTypedata>());
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
                    currentAirPush += obs.AIR_airFlow.AIR_force * obs.AIR_airFlow.AIR_currentPercentageAirStrength + obs.AIR_airFlow.AIR_force * RandomOf(new float[] { -1, 1 }) * Random.Range(0, obs.AIR_airFlow.AIR_variety) * obs.AIR_airFlow.AIR_currentPercentageAirStrength;
                    if (obs.AIR_airFlow.AIR_fullStrengthTime > 0) obs.AIR_airFlow.AIR_currentPercentageAirStrength += Time.fixedDeltaTime / obs.AIR_airFlow.AIR_fullStrengthTime;
                    else obs.AIR_airFlow.AIR_currentPercentageAirStrength = 1;
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

    static float RandomOf(float[] randoms) //returns random number in Array
    {
        return randoms[Random.Range(0, randoms.Length)];
    }
}
