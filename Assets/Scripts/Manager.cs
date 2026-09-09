using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public static Manager m;

    public GameObject world;
    public Camera playerCamera;
    public GameplayManager gameplayManager;
    public Worldbuilder worldBuilder;
    public PlayerController playerController;
    public ObstacleTypedata[] obstacleTypeDatas;
    public ObstacleTypedata[] obstacleTypeDatasResetOnLevelStart;
    public ObstacleTypedata[] obstacleTypeDatasStopInMenu;


    public bool disableParticleSystems;


    private void Awake()
    {
        if (m == null)
        {
            m = this;
            DontDestroyOnLoad(gameObject);

            obstacleTypeDatas = world.GetComponentsInChildren<ObstacleTypedata>();
            int count = 0;
            for (int i = 0; i < obstacleTypeDatas.Length; i++) if (obstacleTypeDatas[i].resetOnLevelStart) count++;
            obstacleTypeDatasResetOnLevelStart = new ObstacleTypedata[count];
            count = 0;
            for (int i = 0; i < obstacleTypeDatas.Length; i++)
            {
                if (obstacleTypeDatas[i].resetOnLevelStart)
                {
                    obstacleTypeDatasResetOnLevelStart[count] = obstacleTypeDatas[i];
                    count++;
                }
            }
            count = 0;
            for (int i = 0; i < obstacleTypeDatas.Length; i++) if (obstacleTypeDatas[i].stopInMenu) count++;
            obstacleTypeDatasStopInMenu = new ObstacleTypedata[count];
            count = 0;
            for (int i = 0; i < obstacleTypeDatas.Length; i++)
            {
                if (obstacleTypeDatas[i].stopInMenu)
                {
                    obstacleTypeDatasStopInMenu[count] = obstacleTypeDatas[i];
                    count++;
                }
            }
            if (disableParticleSystems)
            {
                ParticleSystem[] allSystems = world.GetComponentsInChildren<ParticleSystem>();
                for (int i = 0; i < allSystems.Length; i++)
                {
                    allSystems[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            Destroy(this);
        }
    }
    public static void DisableAllColliders(GameObject g)
    {
        if (g == null) return;

        foreach (Collider c in g.GetComponentsInChildren<Collider>(true))
        {
            c.enabled = false;
        }
        foreach (Collider2D c in g.GetComponentsInChildren<Collider2D>(true))
        {
            c.enabled = false;
        }
    }

    public static void EnableAllColliders(GameObject g)
    {
        if (g == null) return;

        foreach (Collider c in g.GetComponentsInChildren<Collider>(true))
        {
            c.enabled = true;
        }
        foreach (Collider2D c in g.GetComponentsInChildren<Collider2D>(true))
        {
            c.enabled = true;
        }
    }

    public static void DisableAllRenderers(GameObject g)
    {
        if (g == null) return;

        foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = false;
        }
    }

    public static void EnableAllRenderers(GameObject g)
    {
        if (g == null) return;

        foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = true;
        }
    }
}