using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Coin : Collectible, IDestroyable, IPoolable {
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

    private void Awake() {
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

    public void SetupPooledObject(Vector3 position, Transform parent, Vector3 velocity) {
        gameObject.SetActive(true);
        trigger.enabled = true;
        visual.gameObject.SetActive(true);
        collected = false;
        transform.position = position;
        transform.SetParent(transform);
        if(velocity != Vector3.zero) {
            SetPhysicsState(true);
            rigidbody.AddForce(velocity, ForceMode.VelocityChange);
        }
    }

    public override void Pickup(Transform player) {
        base.Pickup(player);
        StartCoroutine(DestroyObject());
    }

    public IEnumerator DestroyObject() {
        destructionParticles.Emit(10);
        Transform player = GameManager.Instance.player.transform;
        float startingTime = GameManager.GameTime;
        while (GameManager.GameTime - startingTime < COLLECTION_TIME) {
            transform.position = Vector3.Lerp(transform.position, player.position, Mathf.InverseLerp(0, COLLECTION_TIME, GameManager.GameTime - startingTime));
            visual.localScale *= 0.95f;
            yield return new WaitForEndOfFrame();
        }
        visual.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.1f);
        GameManager.Instance.poolManager.ReturnToPool("Coin", this, gameObject);
    }

    public override void SetPhysicsState(bool state) {
        base.SetPhysicsState(state);
        trueRotationSpeed = (360 / rotationSpeed) * (state ? physicsRotationSlow : 1);
    }
}

interface IDestroyable {
    IEnumerator DestroyObject();
}
