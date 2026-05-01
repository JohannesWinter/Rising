using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public List<HurensohnLine> linessss;
}

[Serializable]
public class HurensohnLine
{
    private float x = 10;
    public AnimationCurve ll = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));

    public HurensohnLine()
    {
        Debug.Log(x);
    }
}
