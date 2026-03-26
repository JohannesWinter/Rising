using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    public float speed;
    public GameObject backgroundObj;
    public float repeatingHeight;
    public bool changeHeightRelative;
    float layerStartY;
    // Start is called before the first frame update
    void Start()
    {
        layerStartY = backgroundObj.transform.position.y;
        if (changeHeightRelative == true)
        {
            foreach (Transform obj in backgroundObj.transform)
            {
                obj.transform.localPosition = new Vector3(obj.transform.localPosition.x, obj.transform.localPosition.y * (1 - speed), obj.transform.localPosition.z);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Manager.m.playerCamera.transform.position.y >= layerStartY)
        {
            float newYVal = layerStartY + (Manager.m.playerCamera.transform.position.y - layerStartY) * speed;

            if (repeatingHeight > 0)
            {
                float distanceToCam = Manager.m.playerCamera.transform.position.y - newYVal;
                if (distanceToCam > repeatingHeight)
                {
                    newYVal += Mathf.Floor(distanceToCam / repeatingHeight) * repeatingHeight;
                }
                else if (distanceToCam < -repeatingHeight)
                {
                    newYVal -= Mathf.Floor(-distanceToCam / repeatingHeight) * repeatingHeight;
                }
            }

            Vector3 newPosition = new Vector3(backgroundObj.transform.position.x, newYVal, backgroundObj.transform.position.z);
            backgroundObj.transform.position = newPosition;
        }
    }
}
