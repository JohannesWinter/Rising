using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public bool enableFixedDelta;
    public bool enableNormalDelta;
    public float curFixedTime;
    public float curNormalTime;

    private void FixedUpdate()
    {
        if (enableFixedDelta)
        {
            curFixedTime += Time.fixedDeltaTime;
            print("Timer (FixedTime): " + curFixedTime + "s");
            if (Input.GetButtonDown("Fire2"))
            {
                curFixedTime = 0;
            }
        }
    }
    private void Update()
    {
        if (enableNormalDelta)
        {
            curNormalTime += Time.deltaTime;
            print("Timer (NormalTime): " + curNormalTime + "s");
            if (Input.GetButtonDown("Fire2"))
            {
                curNormalTime = 0;
            }
        }
    }
}
