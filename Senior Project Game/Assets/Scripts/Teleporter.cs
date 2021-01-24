using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : MonoBehaviour {
    public const float TELEPORT_COOLDOWN = 2.5f; 

    [SerializeField] Teleporter pairedTeleporter;
    [SerializeField] Vector3 teleportationOffset;
    [HideInInspector] public Vector3 teleportationPoint;
    [SerializeField] bool keepMomentum = false;
    [SerializeField] bool keepRotation = true;
    [SerializeField] Vector3 exitRotation;

    private void Start() {
        teleportationPoint = transform.position + teleportationOffset;
    }

    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Player")) {
            if(!other.GetComponent<Player>().justTeleported) Teleport(other.transform);
        }
    }

    private void Teleport(Transform other) {
        other.position = pairedTeleporter.teleportationPoint;
        if (!pairedTeleporter.keepRotation) other.rotation = Quaternion.Euler(pairedTeleporter.exitRotation);
        other.GetComponent<Player>().StartCoroutine("JustTeleported");
    }


}
