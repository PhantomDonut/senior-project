using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Surface : MonoBehaviour {
    public bool passthrough;
    private BoxCollider boxCollider;
    private Transform trueCollider;

    [Range(0, 1)] public float friction = 1;

    private void Start() {
        boxCollider = GetComponent<BoxCollider>();
        if(passthrough) trueCollider = transform.GetChild(0);
    }

    public void PlayerAbove(bool state) {
        if(passthrough) {
            trueCollider.gameObject.SetActive(state);
        }
    }
}
