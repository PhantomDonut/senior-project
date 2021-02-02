using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Teleporter : MonoBehaviour {
    public const float TELEPORT_COOLDOWN = 0.5f; 

    [SerializeField] Teleporter pairedTeleporter = null;
    [SerializeField] Vector3 teleportationOffset = Vector3.zero;
    [HideInInspector] public Vector3 teleportationPoint;
    [SerializeField] bool keepMomentum = false;
    [SerializeField] bool keepRotation = true;
    [ShowIf("keepRotation", false)] [SerializeField] Vector3 exitRotation = Vector3.zero;

    private Player player = null;

    private void Start() {
        teleportationPoint = transform.position + teleportationOffset;
        player = GameManager.Instance.player;
        Debug.Log(player);
    }

    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Player") || other.CompareTag("Player Contact")) {
            if (!player.justTeleported) Teleport(other.transform);
        }
    }

    private void Teleport(Transform other) {
        other.position = pairedTeleporter.teleportationPoint;
        if (!pairedTeleporter.keepRotation) other.rotation = Quaternion.Euler(pairedTeleporter.exitRotation);
        if (!pairedTeleporter.keepMomentum) player.CancelMomentum();
        player.StartCoroutine("JustTeleported");
    }


}
