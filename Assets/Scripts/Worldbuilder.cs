using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class Worldbuilder : MonoBehaviour
{
    public Transform playerSpace;
    public GameObject squareSample;

    public bool running;
    public bool finished;

    float currentMaxHeight;
    float currentMinHeight;

    public List<Level> levelList;
    void Start()
    {

    }

    void Update()
    {
        if (running)
        {
            Manager.m.playerController.currentGeneralSpeed = levelList[Manager.m.gameplayManager.currentLevel - 1].speed;
            UpdateHeight();
        }
        else
        {
            Manager.m.playerController.currentGeneralSpeed = 0;
        }   

        if (running == true && Manager.m.playerCamera.transform.position.y > currentMaxHeight)
        {
            finished = true;
        }
    }

    public void Reset()
    {
        UpdateHeight();
        running = false;
        finished = false;
        Vector3 oldPlayerPosition = Manager.m.playerController.playerObject.transform.position;
        playerSpace.localPosition = new Vector3(0, currentMinHeight, 0);
        Manager.m.playerController.playerObject.transform.position = oldPlayerPosition;
    }


    void UpdateHeight()
    {
        currentMaxHeight = 0;
        currentMinHeight = 0;
        for (int i = 0; i < Manager.m.gameplayManager.currentLevel; i++) currentMaxHeight += levelList[i].height;
        for (int i = 0; i < Manager.m.gameplayManager.currentLevel - 1; i++) currentMinHeight += levelList[i].height;
    }

    public float GetCurrentMinHeight()
    {
        return currentMinHeight;
    }
    public float GetCurrentMaxHeight()
    {
        return currentMaxHeight;
    }
}

[Serializable]
public class Level
{
    public float height;
    public float speed;
}
