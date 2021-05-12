using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControllerVolume : MonoBehaviour {
    public CameraControllerSettings volumeSettings;
    bool entered;

    void OnTriggerEnter(Collider other) {
        Debug.Log(other.name);
        if (!entered) {
            GameManager.Instance.player.playerCamera.SwapCameraControllerSettings(volumeSettings, true);
            entered = true;
        }
    }

    void OnTriggerExit(Collider other) {
        if (entered) {
            GameManager.Instance.player.playerCamera.SwapCameraControllerSettings(volumeSettings, false);
            entered = false;
        }
    }
}
