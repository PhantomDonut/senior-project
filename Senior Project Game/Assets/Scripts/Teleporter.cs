using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Teleporter : MonoBehaviour {
    public const float TELEPORT_COOLDOWN = 0.5f;

    public bool stageLoadTeleporter = false;
    [ShowIf("stageLoadTeleporter")] public string stageToLoad;
    [ShowIf("stageLoadTeleporter")] public CelestialTime celestialTime;
    [HideIf("stageLoadTeleporter")] [SerializeField] Teleporter pairedTeleporter = null;
    [HideIf("stageLoadTeleporter")] [SerializeField] Vector3 teleportationOffset = Vector3.zero;
    [HideIf("stageLoadTeleporter")] [HideInInspector] public Vector3 teleportationPoint;
    [HideIf("stageLoadTeleporter")] [SerializeField] bool keepMomentum = false;
    [HideIf("stageLoadTeleporter")] [SerializeField] bool keepRotation = true;
    [HideIf("stageLoadTeleporter")] [ShowIf("keepRotation", false)] [SerializeField] Vector3 exitRotation = Vector3.zero;
    [HideIf("stageLoadTeleporter")] [SerializeField] bool instantTeleport;

    private void Start() {
        teleportationPoint = transform.position + teleportationOffset;
    }

    private void OnTriggerEnter(Collider other) {
        if(other.transform.root.CompareTag("Player")) {
            if (!GameManager.Instance.player.justTeleported) Teleport(other.transform.root);
        }
    }

    private void Teleport(Transform other) {
        if (stageLoadTeleporter) {
            GameManager.Instance.player.StartCoroutine("JustTeleported");
            GameManager.Instance.SwitchToLevel(stageToLoad, false, celestialTime);
        }
        else {
            other.position = pairedTeleporter.teleportationPoint;
            if (!pairedTeleporter.keepRotation) other.rotation = Quaternion.Euler(pairedTeleporter.exitRotation);
            if (!pairedTeleporter.keepMomentum) GameManager.Instance.player.CancelMomentum();
            if (instantTeleport) {
                GameManager.Instance.player.visual.position = pairedTeleporter.teleportationPoint;
                GameManager.Instance.player.playerCamera.InstantToTarget();
            }
            GameManager.Instance.player.StartCoroutine("JustTeleported");
        }
    }


}
