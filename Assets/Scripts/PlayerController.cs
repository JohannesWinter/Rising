using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameState gamestate;
    public float currentGeneralSpeed;
    public bool dead;
    public GameObject playerObject;
    public Camera cam;
    public Rigidbody2D rb;
    public float minSideGap;
    public float minTopGap;
    public float minBotGap;
    public float maxSpeed;
    public float approachSpeed;
    Transform playerTransform;
    Resolution res;
    float viewsizeX;
    float viewsizeY;
    Vector2 mousePos = Vector2.zero;

    Vector3 targetPos;
    // Start is called before the first frame update
    void Start()
    {
        res = Screen.currentResolution;
        playerTransform = playerObject.transform;
        viewsizeX = cam.orthographicSize * cam.aspect;
        viewsizeY = cam.orthographicSize;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTarget();
    }

    void FixedUpdate()
    {
        MoveToTarget();
        UpdateGeneralMovement();
        CollisionDetection();
    }

    void UpdateTarget()
    {
        if (gamestate == GameState.Menu)
        {
            targetPos = new Vector2(0, 0);
            playerObject.transform.position = new Vector3(0,0,0);
            return;
        }
        if (gamestate == GameState.Stopped)
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
        if (gamestate == GameState.Running)
        {
            Vector3 adjustedTargetPos = targetPos + cam.transform.localPosition;
            Vector3 targetVelocity = (adjustedTargetPos - playerTransform.localPosition) * approachSpeed * currentGeneralSpeed;
            if (targetVelocity.magnitude > maxSpeed)
            {
                targetVelocity = targetVelocity.normalized * maxSpeed;
            }
            rb.velocity = targetVelocity + Vector3.up * currentGeneralSpeed;

        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    void UpdateGeneralMovement()
    {
        cam.gameObject.transform.Translate(Vector3.up * Time.fixedDeltaTime * currentGeneralSpeed);
    }

    void CollisionDetection()
    {
        Collider2D[] col = new Collider2D[5]; //random ass value
        Physics2D.OverlapCollider(this.GetComponent<CircleCollider2D>(), new ContactFilter2D(), col);
        for (int i = 0; i < col.Length; i++)
        {
            if (col[i] != null)
            {
                if (col[i].gameObject.GetComponent<ObstacleTypedata>())
                {
                    HandleObstacleCollision(col[i].gameObject.GetComponent<ObstacleTypedata>());
                } 
            }
            else
            {
                break;
            }
        }
    }

    void OnTriggerEnter(Collider collision){
    }

    void HandleObstacleCollision(ObstacleTypedata obs)
    {
        switch (obs.collisionType)
        {
            case ObstacleCollisionType.Simple: 
                break;
            case ObstacleCollisionType.Sharp:
                dead = true;
                break;
        }
    }
}
