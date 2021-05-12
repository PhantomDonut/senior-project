using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour {
    public ObjectPoolManager poolManager;
    public EnvironmentTime environmentTimeManager;
    public CelestialTime defaultTime = CelestialTime.Day;
    public Transform localScenePlayerPosition;
    public Vector3 startingPosition;
    public Vector3 checkpointPosition;

    public void SetPlayer(Player player) {
        player.transform.position = startingPosition;
        player.visual.transform.position = startingPosition;
        player.playerCamera.InstantToTarget();
    }

    public void RespawnPlayer(Player player) {
        player.transform.position = (checkpointPosition != Vector3.zero ? checkpointPosition : startingPosition);
        player.playerCamera.InstantToTarget();
        player.playerCamera.activeFollow = true;
    }

    public void SetCheckpoint(Transform checkpoint) {
        checkpointPosition = checkpoint.position;
    }

    private void LateUpdate() {
        if (GameManager.LoadedScene && GameManager.Instance.player != null) localScenePlayerPosition.position = GameManager.Instance.player.visual.transform.position;
    }
}
