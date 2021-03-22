using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControllerVolume : MonoBehaviour {
    public CameraControllerSettings volumeSettings;

    void OnTriggerEnter(Collider other) {
        GameManager.Instance.player.playerCamera.SwapCameraControllerSettings(volumeSettings);
    }

    void OnTriggerExit(Collider other) {
        GameManager.Instance.player.playerCamera.SwapCameraControllerSettings(null);
    }
}
