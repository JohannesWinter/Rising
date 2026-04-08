using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    Vector3 localPos;
    // Start is called before the first frame update
    void Start()
    {
        localPos = this.gameObject.transform.localPosition;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        this.gameObject.transform.localPosition = localPos;
    }
}
