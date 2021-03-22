using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialUVScroll : MonoBehaviour {

    [SerializeField] private Vector2 scrollSpeed = Vector2.zero;
    private Material material;
    private Vector2 offset;

    private void Start() {
        material = GetComponent<MeshRenderer>().sharedMaterial;
    }

    private void Update() {
        offset.x += Time.deltaTime * scrollSpeed.x * 0.001f;
        offset.y += Time.deltaTime * scrollSpeed.y * 0.001f;
        material.mainTextureOffset = offset;
    }
}
