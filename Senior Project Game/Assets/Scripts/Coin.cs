using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Coin : MonoBehaviour {
    private bool collected;
    private AudioSource audioSource;

    [Header("Visual")]
    private Transform visual;
    [Range(0.5f, 6)] public float movementSpeed;
    [Range(0, 1)] public float movementAmount;
    private Vector3 startingPosition;
    public float rotationSpeed;
    private float trueRotationSpeed;
    const float COLLECTION_TIME = 0.2f;

    [Header("Physics")]
    private BoxCollider trigger;
    new private CapsuleCollider collider;
    new private Rigidbody rigidbody;
    public bool physicsCoin;
    [Range(0, 1)] public float physicsRotationSlow = 0.8f;

    private void Start() {
        audioSource = GetComponent<AudioSource>();
        visual = transform.GetChild(0);
        startingPosition = visual.localPosition;
        trigger = GetComponent<BoxCollider>();
        collider = GetComponent<CapsuleCollider>();
        rigidbody = GetComponent<Rigidbody>();
        SetPhysicsState(physicsCoin);
    }

    private void Update() {
        visual.rotation = Quaternion.Euler(visual.rotation.x, (GameManager.GameTime * trueRotationSpeed) % 360, visual.rotation.z);
        if(!physicsCoin && !collected) visual.localPosition = new Vector3(startingPosition.x, Mathf.Lerp(startingPosition.y - movementAmount, startingPosition.y + movementAmount, Mathf.InverseLerp(-1, 1, Mathf.Sin(GameManager.GameTime * movementSpeed))), startingPosition.z);
    }

    IEnumerator Pickup(Transform player) {
        collected = true;
        SetPhysicsState(false);
        audioSource.Play();
        trigger.enabled = false;
        float startingTime = GameManager.GameTime;
        while (GameManager.GameTime - startingTime < COLLECTION_TIME) {
            transform.position = Vector3.Lerp(transform.position, player.position, Mathf.InverseLerp(0, COLLECTION_TIME, GameManager.GameTime - startingTime));
            visual.localScale *= 0.95f;
            yield return new WaitForEndOfFrame();
        }
        Destroy(gameObject);
        //visual.gameObject.SetActive(false);
    }

    private void SetPhysicsState(bool state) {
        collider.enabled = state;
        trueRotationSpeed = (360 / rotationSpeed) * (state ? physicsRotationSlow : 1);
        rigidbody.useGravity = state;
    }

    private void OnTriggerEnter(Collider other) {
        if (!collected && other.transform.CompareTag("Player")) {
            StartCoroutine(Pickup(GameManager.Instance.player.transform));
        }
    }
}
