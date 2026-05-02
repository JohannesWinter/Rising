using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public List<EnviromentParticles> particles;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        float height = Manager.m.playerCamera.transform.position.y;
        foreach (var p in particles)
        {
            float minHeight = Manager.m.worldBuilder.GetLevelMinHeight(p.minLevel) + p.minAdd;
            float maxHeight = Manager.m.worldBuilder.GetLevelMaxHeight(p.maxLevel) + p.maxAdd;

            if (minHeight > maxHeight)
            {
                Debug.LogWarning("Wrong assignment of Enviromental Particlesystem <" + p.particleSystem.gameObject.name + ", " + p.particleSystem.gameObject.transform.parent.gameObject.name +">");
                return;
            }

            if (height > minHeight && height < maxHeight)
            {
                p.particleSystem.transform.position = new Vector3(p.particleSystem.transform.position.x, height, p.particleSystem.transform.position.z);
            }
            else if (height < minHeight)
            {
                p.particleSystem.transform.position = new Vector3(p.particleSystem.transform.position.x, minHeight, p.particleSystem.transform.position.z);
            }
            else if (height > maxHeight)
            {
                p.particleSystem.transform.position = new Vector3(p.particleSystem.transform.position.x, maxHeight, p.particleSystem.transform.position.z);
            }
        }
    }
}

[System.Serializable]
public class EnviromentParticles
{
    public GameObject particleSystem;
    public int minLevel;
    public float minAdd;
    public int maxLevel;
    public float maxAdd;
}
