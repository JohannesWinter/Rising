using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    bool up = true;
    float timer = 0;
    public float speed;
    public float duration;
    private void Start()
    {
        timer = duration;
    }
    public void Update()
    {
        if (up)
        {
            timer -= Time.deltaTime;
            gameObject.transform.Translate(0, speed * Time.deltaTime, 0, Space.World);
            if (timer < 0)
            {
                up = false;
                timer = duration;
            }
        }
        else
        {
            timer -= Time.deltaTime;
            gameObject.transform.Translate(0, -speed * Time.deltaTime, 0, Space.World);
            if (timer < 0)
            {
                up = true;
                timer = duration;
            }
        }
    }
}