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
}
