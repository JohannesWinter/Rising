using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class Worldbuilder : MonoBehaviour
{
    public PlayerController pc;
    public GameObject squareSample;
    public Camera cam;
    public bool running;
    public bool reset;
    public int generate;
    public bool finished;

    float currentMaxHeight;
    List<Obstacle> obstacles;

    public GameObject[] levels;
    void Start()
    {
        obstacles = new List<Obstacle>();
    }

    void Update()
    {
        if (generate != 0)
        {
            Generate(generate);
            generate = 0;

        }
        if (running)
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i].obstacleEntity == ObstacleEntity.Level)
                {
                    pc.currentGeneralSpeed = obstacles[i].speed;
                    currentMaxHeight = obstacles[i].height;
                }
            }
        }
        else
        {
            pc.currentGeneralSpeed = 0;
        }   

        if (reset)
        {
            reset = false;
            Reset();
        }

        if (running == true && pc.cam.transform.position.y > currentMaxHeight)
        {
            finished = true;
        }
    }

    void Generate(int level)
    {
        obstacles.Add(SpawnLevel(level).GetComponent<Obstacle>());
    }

    GameObject SpawnSimple()
    {
        GameObject o = Instantiate(squareSample);
        float yPos = cam.orthographicSize * 1.5f; //a little over top side
        float xPos = -cam.orthographicSize * cam.aspect + Random.Range(0f, cam.orthographicSize * cam.aspect * 2);
        o.transform.position = new Vector2(xPos, yPos);
        return o;
    }

    GameObject SpawnLevel(int level)
    {
        GameObject o = Instantiate(levels[level - 1], Vector2.zero, new Quaternion(0,0,0,0));
        o.transform.position = Vector2.zero;
        o.SetActive(true);
        return o;
    }

    void Reset()
    {
        while(obstacles.Count > 0)
        {
            GameObject o = obstacles[0].gameObject;
            obstacles.RemoveAt(0);
            Destroy(o);
        }
    }
}
