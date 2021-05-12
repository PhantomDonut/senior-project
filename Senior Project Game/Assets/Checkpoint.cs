using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour {
    [SerializeField] public Transform respawnPosition;
    private const float HEIGHT_PLAYER = 0.5675f;

    private void Start() {
        RaycastHit hit;
        if (Physics.Raycast(respawnPosition.position, -Vector3.up, out hit, Mathf.Infinity, LayerMask.GetMask("Surface"))) {
            float offsetDistance = hit.distance;
            respawnPosition.transform.position = hit.point + Vector3.up * HEIGHT_PLAYER;
        }
    }

    void OnTriggerEnter(Collider other) {
        Debug.Log(other.name);
        GameManager.Instance.currentLevelManager.SetCheckpoint(respawnPosition);
    }
}
