using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ShakeObject : MonoBehaviour
{
    [SerializeField] public float intensity = 0.1f;
    [SerializeField] public float frequency = 10f;

    private Vector3 originalPosition;
    private float seed;

    void Start()
    {
        originalPosition = transform.localPosition;
        seed = Random.value;
    }

    void Update()
    {
        float x = (Mathf.PerlinNoise(Time.time * frequency + seed, 0f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(0f, Time.time * frequency + seed) - 0.5f) * 2f;

        Vector3 offset = new Vector3(x, y, 0f) * intensity;

        transform.localPosition = originalPosition + offset;
    }
}