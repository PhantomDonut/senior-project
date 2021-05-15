using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CelestialPiece : Collectible, IDestroyable {
    [Header("Visual")]
    private Transform visual;
    public float rotationSpeed;
    private float trueRotationSpeed;
    const float COLLECTION_TIME = 0.2f;
    private ParticleSystem destructionParticles;

    private void Start() {
        audioSource = GetComponent<AudioSource>();
        visual = transform.GetChild(0);
        trigger = GetComponent<BoxCollider>();
        collider = GetComponent<CapsuleCollider>();
        rigidbody = GetComponent<Rigidbody>();
        destructionParticles = GetComponentInChildren<ParticleSystem>();
        SetPhysicsState(physicsObject);
    }

    private void Update() {
        visual.rotation = Quaternion.Euler(visual.rotation.x, (GameManager.GameTime * trueRotationSpeed) % 360, visual.rotation.z);
    }

    public override void Pickup(Transform player) {
        base.Pickup(player);
        StartCoroutine(DestroyObject());
    }

    public IEnumerator DestroyObject() {
        destructionParticles.Emit(10);
        visual.gameObject.SetActive(false);
        yield return new WaitForSeconds(3f);
        GameManager.Instance.SwitchToLevel("Hub", true, CelestialTime.Day);
        GameManager.Instance.gameState = GameState.Cutscene;
        //Destroy(gameObject);
    }

    public override void SetPhysicsState(bool state) {
        base.SetPhysicsState(state);
        trueRotationSpeed = (360 / rotationSpeed);
    }
}
