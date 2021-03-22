using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Coin : Collectible, IDestroyable {
    [Header("Visual")]
    private Transform visual;
    [Range(0.5f, 6)] public float movementSpeed;
    [Range(0, 1)] public float movementAmount;
    private Vector3 startingPosition;
    public float rotationSpeed;
    private float trueRotationSpeed;
    const float COLLECTION_TIME = 0.2f;
    private ParticleSystem destructionParticles;

    [Header("Physics")]
    [Range(0, 1)] public float physicsRotationSlow = 0.8f;

    private void Start() {
        audioSource = GetComponent<AudioSource>();
        visual = transform.GetChild(0);
        startingPosition = visual.localPosition;
        trigger = GetComponent<BoxCollider>();
        collider = GetComponent<CapsuleCollider>();
        rigidbody = GetComponent<Rigidbody>();
        destructionParticles = GetComponentInChildren<ParticleSystem>();
        SetPhysicsState(physicsObject);
    }

    private void Update() {
        visual.rotation = Quaternion.Euler(visual.rotation.x, (GameManager.GameTime * trueRotationSpeed) % 360, visual.rotation.z);
        if(!physicsObject && !collected) visual.localPosition = new Vector3(startingPosition.x, Mathf.Lerp(startingPosition.y - movementAmount, startingPosition.y + movementAmount, Mathf.InverseLerp(-1, 1, Mathf.Sin(GameManager.GameTime * movementSpeed))), startingPosition.z);
    }

    public override void Pickup(Transform player) {
        base.Pickup(player);
        StartCoroutine(DestroyObject(GameManager.Instance.player.transform));
    }

    public IEnumerator DestroyObject(Transform player) {
        destructionParticles.Emit(10);
        float startingTime = GameManager.GameTime;
        while (GameManager.GameTime - startingTime < COLLECTION_TIME) {
            transform.position = Vector3.Lerp(transform.position, player.position, Mathf.InverseLerp(0, COLLECTION_TIME, GameManager.GameTime - startingTime));
            visual.localScale *= 0.95f;
            yield return new WaitForEndOfFrame();
        }
        visual.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.1f);
        Destroy(gameObject);
    }

    public override void SetPhysicsState(bool state) {
        base.SetPhysicsState(state);
        trueRotationSpeed = (360 / rotationSpeed) * (state ? physicsRotationSlow : 1);
    }
}

interface IDestroyable {
    IEnumerator DestroyObject(Transform player);
}
