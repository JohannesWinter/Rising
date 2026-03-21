using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public ObstacleEntity obstacleEntity;
    public float height;
    public float speed;
}

public enum ObstacleEntity
{
    Singel,
    Level
}
