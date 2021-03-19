using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Surface : MonoBehaviour {
    public SurfaceType surfaceAttributes;
    [HideInInspector] public BoxCollider boxCollider;
    public bool passthrough;
    public bool walljump = true;
    private const int SURFACE_LAYER = 10;
    private const int PASSTHROUGH_LAYER = 11;

    public void Start() {
        boxCollider = GetComponent<BoxCollider>();
        if (passthrough) {
            new GameObject("Passthrough").AddComponent<SurfaceTrigger>().Create(this, SurfaceTriggerType.Passthrough);
            TogglePassthrough(false);
        }

    }

    public void TogglePassthrough(bool state) {
        gameObject.layer = state ? PASSTHROUGH_LAYER : SURFACE_LAYER;
    }
}
