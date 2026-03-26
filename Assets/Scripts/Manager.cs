using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public static Manager m;

    public Camera playerCamera;
    public GameplayManager gameplayManager;
    public Worldbuilder worldBuilder;
    public PlayerController playerController;

    private void Awake()
    {
        if (m == null)
        {
            m = this;
            DontDestroyOnLoad(gameObject);


        }
        else
        {
            Destroy(this);
        }
    }
}
