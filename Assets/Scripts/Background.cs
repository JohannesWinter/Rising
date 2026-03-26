using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    public Camera cam;
    public float speed;
    public GameObject backgroundObj;
    public float repeatingHeight;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float newYVal = cam.transform.position.y * speed;
        float distanceToCam = cam.transform.position.y - newYVal;

        if (distanceToCam > repeatingHeight)
        {
            newYVal += Mathf.Floor(distanceToCam / repeatingHeight) * repeatingHeight;
        }
        else if (distanceToCam < -repeatingHeight)
        {
            newYVal -= Mathf.Floor(-distanceToCam / repeatingHeight) * repeatingHeight;
        }
        Vector3 newPosition = new Vector3(backgroundObj.transform.position.x, newYVal, backgroundObj.transform.position.z);
        backgroundObj.transform.position = newPosition;
    }
}
