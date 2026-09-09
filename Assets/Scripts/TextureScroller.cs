using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class TextureScroller : MonoBehaviour
{
    [SerializeField] public float scrollSpeed = 0.5f;
    [SerializeField] private string texturePropertyName = "_MainTex";   

    private SpriteRenderer spriteRenderer;
    private Material mat;
    private Vector2 currentOffset = Vector2.zero;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mat = spriteRenderer.material;
    }

    void Update()
    {
        currentOffset.x += scrollSpeed * Time.deltaTime;
        mat.SetTextureOffset(texturePropertyName, currentOffset);
    }
}
