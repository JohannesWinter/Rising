using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]

public class test : MonoBehaviour
{
    private void Update()
    {
        int toRemovePos = (new System.Random()).Next(0, 7);
        print(toRemovePos);
    }
}