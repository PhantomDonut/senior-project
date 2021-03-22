using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Collectible : MonoBehaviour {
    [HideInInspector] public bool collected;
    protected AudioSource audioSource;
    protected BoxCollider trigger;
    public virtual void Pickup(Transform player) {
        collected = true;
        SetPhysicsState(false);
        audioSource.Play();
        trigger.enabled = false;
    }

    new protected CapsuleCollider collider;
    new protected Rigidbody rigidbody;
    public bool physicsObject;
    public virtual void SetPhysicsState(bool state) {
        physicsObject = state;
        collider.enabled = state;
        rigidbody.useGravity = state;
    }
}
