using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour {
    public ObjectPoolManager poolManager;
    public Transform localScenePlayerPosition;
    public Vector3 startingPosition;

    public void SetPlayer(Player player) {
        player.transform.position = startingPosition;
    }

    private void LateUpdate() {
        if (GameManager.Instance.player != null) localScenePlayerPosition.position = GameManager.Instance.player.visual.transform.position;
    }
}
