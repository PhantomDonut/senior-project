using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    [HideInInspector] public bool activeFollow = true;
    public Transform positionTarget;
    public Transform rotationTarget;

    public Transform cameraTransform;
    public Transform idealTarget;

    //private Vector3 offsetPosition;
    public float moveSpeed = 5;
    public float turnSpeed = 10;
    public float smoothSpeed = 0.5f;

    Quaternion targetRotation;
    Vector3 targetPosition;

    public LayerMask layerMask;
    public float maximumHeightAbove = 5;

    public float sharpRotationAngle;
    public float trueRotationAngle;

    public CameraControllerSettings defaultCameraSettings;
    [HideInInspector] public CameraControllerSettings cameraSettings;
    private List<CameraControllerSettings> cameraSettingsEntered = new List<CameraControllerSettings>();

    bool fullControl;
    Vector3 chosenAngles;
    Vector3 idealPosition;

    bool started = false;

    private void Start() {
        if (started) return;
        started = true;
        //cameraSettings = defaultCameraSettings;
        SwapCameraControllerSettings(defaultCameraSettings, true);
        //offsetPosition = cameraSettings.offsetPosition;
        cameraTransform = Camera.main.transform;
        idealTarget = GameObject.Find("Ideal Position").transform;

    }

    private void Update() {
        if (GameManager.Instance.gameState != GameState.Regular || !activeFollow) return;
        if (Input.GetKeyDown(KeyCode.G)) {
            sharpRotationAngle += 45;
        }

        if (Input.GetKeyDown(KeyCode.F)) {
            fullControl = !fullControl;
            Cursor.visible = !fullControl;
            if (fullControl) {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else {
                Cursor.lockState = CursorLockMode.None;
            }
        }

        if(fullControl) {
            chosenAngles.x = Mathf.Clamp(chosenAngles.x + Input.GetAxis("Mouse Y") * 1, -20, 30);
            chosenAngles.y += Input.GetAxis("Mouse X") * 2;
            chosenAngles.y += Input.GetAxis("Horizontal");
        }
    }

    private void LateUpdate() {
        if (GameManager.Instance.gameState != GameState.Regular || !activeFollow) return;
        MoveWithTarget(fullControl);
        trueRotationAngle = cameraTransform.rotation.eulerAngles.y;
        Vector3 levelRot = cameraTransform.rotation.eulerAngles;
        levelRot.z = 0;
        cameraTransform.rotation = Quaternion.Euler(levelRot);
    }

    private void MoveWithTarget(bool freeform) {
        transform.position = Vector3.Lerp(transform.position, positionTarget.position, moveSpeed * Time.deltaTime);

        RaycastHit linecastHit;
        float maxPotentialDistance = Vector3.Distance(positionTarget.position, idealTarget.position);
        float distance = maxPotentialDistance;
        Vector3 localCamera = cameraSettings.offsetPosition;
        if (Physics.Linecast(positionTarget.position, idealTarget.position, out linecastHit, layerMask)) {
            //Debug.Log("blocked");
            localCamera = transform.InverseTransformPoint(linecastHit.point);
            distance = linecastHit.distance;
        }
        //Vector3 localCamera = cameraSettings.offsetPosition.normalized * (distance - 0.25f);
        localCamera.y = Mathf.Max(maximumHeightAbove, localCamera.y);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, localCamera, moveSpeed * Time.deltaTime); ;
        idealTarget.localPosition = cameraSettings.offsetPosition;
        cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, Quaternion.Euler(Mathf.Lerp(18, 60, Mathf.InverseLerp(maxPotentialDistance, 0, distance)), 0, 0), turnSpeed * Time.deltaTime);
        Quaternion intendedRotation = Quaternion.identity;
        if (freeform) {
            sharpRotationAngle = chosenAngles.y;
            intendedRotation = Quaternion.Euler(chosenAngles);
        } else {
            intendedRotation = Quaternion.Euler(0, sharpRotationAngle, 0);
        }
        transform.rotation = Quaternion.Slerp(transform.rotation, intendedRotation, turnSpeed * Time.deltaTime);
    }

    public void InstantToTarget() {
        if (!started) Start();
        transform.position = positionTarget.position;
        RaycastHit linecastHit;
        float maxPotentialDistance = Vector3.Distance(positionTarget.position, idealTarget.position);
        float distance = maxPotentialDistance;
        Vector3 localCamera = cameraSettings.offsetPosition;
        if (Physics.Linecast(positionTarget.position, idealTarget.position, out linecastHit, layerMask)) {
            localCamera = transform.InverseTransformPoint(linecastHit.point);
            distance = linecastHit.distance;
        }
        localCamera.y = Mathf.Max(maximumHeightAbove, localCamera.y);
        cameraTransform.localPosition = localCamera;
        idealTarget.localPosition = cameraSettings.offsetPosition;
        cameraTransform.localRotation = Quaternion.Euler(Mathf.Lerp(18, 60, Mathf.InverseLerp(maxPotentialDistance, 0, distance)), 0, 0);
        Quaternion intendedRotation = Quaternion.identity;
        if (fullControl) {
            sharpRotationAngle = chosenAngles.y;
            intendedRotation = Quaternion.Euler(chosenAngles);
        } else {
            intendedRotation = Quaternion.Euler(0, sharpRotationAngle, 0);
        }
        transform.rotation = intendedRotation;
    }

    public void SwapCameraControllerSettings(CameraControllerSettings settings, bool add) {
        if(add) {
            cameraSettingsEntered.Add(settings);
        } else {
            cameraSettingsEntered.Remove(settings);
        }

        int i = cameraSettingsEntered.Count - 1;
        bool foundCameraSettings = false;
        while (i > -1 && !foundCameraSettings) {
            if (cameraSettingsEntered[i] != null) {
                cameraSettings = cameraSettingsEntered[i];
                foundCameraSettings = true;
            }
            i--;
        }
        //cameraSettings = (settings != null) ? settings : defaultCameraSettings;
        if(cameraSettings.angleOverride || !fullControl) {
            sharpRotationAngle = cameraSettings.sharpRotationAngle;
            chosenAngles.y = sharpRotationAngle;
            chosenAngles.x = 0;
        }
        if(!cameraSettings.allowControl) fullControl = false;
    }
}

[System.Serializable]
public class CameraControllerSettings {
    public bool followPlayer = true;
    public Vector3 offsetPosition;
    public bool angleOverride;
    public float sharpRotationAngle;
    public bool allowControl;
}
