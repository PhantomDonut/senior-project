using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crate : MonoBehaviour, IHittable, IDestroyable {
    new public BoxCollider collider;
    public MeshRenderer meshRenderer;
    public ParticleSystem destructionParticles;
    private AudioSource breakSound;
    public float hitsRemaining = 1;
    float timeLastHit;

    void Start() {
        collider = GetComponent<BoxCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        destructionParticles = GetComponentInChildren<ParticleSystem>();
        breakSound = GetComponent<AudioSource>();
    }

    public void Hit() {
        hitsRemaining--;
        timeLastHit = GameManager.GameTime;
        if (hitsRemaining < 1) {
            StartCoroutine(DestroyObject());
        } else {
            destructionParticles.Emit(10);
        }
    }
    public IEnumerator DestroyObject() {
        collider.enabled = false;
        meshRenderer.enabled = false;
        destructionParticles.Emit(100);
        breakSound.Play();

        float ve = 3;

        GameManager.Instance.currentLevelManager.poolManager.FetchPooledMulti("Coin", 4, transform.position, null, new Vector3(-ve, 3, -ve), new Vector3(ve, 5, ve));
        yield return new WaitForSeconds(2.5f);
        Destroy(gameObject);
    }

    public bool QueryHitDelay() {
        return GameManager.GameTime - timeLastHit > 1;
    }
}

interface IHittable {
    void Hit();
    bool QueryHitDelay();
}