using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    public Transform positionTarget;
    public Transform rotationTarget;
    private Vector3 offsetPosition;
    public float moveSpeed = 5;
    public float turnSpeed = 10;
    public float smoothSpeed = 0.5f;

    Quaternion targetRotation;
    Vector3 targetPosition;
    bool smoothRotating = false;

    Vector2 idealPosition;
    public LayerMask layerMask;
    public float maximumHeightAbove = 5;

    public float sharpRotationAngle;

    public CameraControllerSettings defaultCameraSettings;
    [HideInInspector]
    public CameraControllerSettings cameraSettings;

    private void Start() {
        cameraSettings = defaultCameraSettings;
        offsetPosition = cameraSettings.offsetPosition;
    }

    private void Update() {
        //if(Input.GetInput())
    }

    private void LateUpdate() {
        MoveWithTarget();

        if(Input.GetKeyDown(KeyCode.G) && !smoothRotating) {
            StartCoroutine("RotateAroundTarget", 45);
        }

        if (Input.GetKeyDown(KeyCode.H) && !smoothRotating) {
            StartCoroutine("RotateAroundTarget", -45);
        }
    }

    private void MoveWithTarget() {
        targetPosition = positionTarget.position + offsetPosition;
        RaycastHit linecastHit;
        float maxPotentialDistance = Vector3.Distance(positionTarget.position, targetPosition);
        float distance = maxPotentialDistance;
        if (Physics.Linecast(positionTarget.position, positionTarget.position + offsetPosition, out linecastHit, layerMask)) {
            Debug.Log("blocked");
            targetPosition = linecastHit.point;
            targetPosition.y = Mathf.Max(positionTarget.position.y + maximumHeightAbove, targetPosition.y);
            distance = linecastHit.distance;
        }

        Vector3 eulerIdeal = new Vector3(Mathf.Lerp(18, 60, Mathf.InverseLerp(maxPotentialDistance, 0, distance)), transform.eulerAngles.y, transform.eulerAngles.z);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(eulerIdeal), turnSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    /*private IEnumerator RotateAroundTarget(float angle) {
        Vector3 velocity = Vector3.zero;
        sharpRotationAngle += angle;
        Vector3 targetOffsetPosition = Quaternion.Euler(0, angle, 0) * cameraSettings.offsetPosition;
        float distance = Vector3.Distance(cameraSettings.offsetPosition, targetOffsetPosition);
        smoothRotating = true;
        
        while(distance > 0.02f) {
            cameraSettings.offsetPosition = Vector3.SmoothDamp(cameraSettings.offsetPosition, targetOffsetPosition, ref velocity, smoothSpeed);

            distance = Vector3.Distance(cameraSettings.offsetPosition, targetOffsetPosition);

            targetRotation = Quaternion.LookRotation(rotationTarget.position - transform.position);
            Vector3 tempRotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime).eulerAngles;
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, tempRotation.y, transform.rotation.z);
            yield return null;
        }

        smoothRotating = false;
        cameraSettings.offsetPosition = targetOffsetPosition;
    }*/

    private IEnumerator RotateAroundTarget(float angle) {
        sharpRotationAngle += angle;
        offsetPosition = RotatePointAroundPivot(transform.position, positionTarget.position, new Vector3(0, sharpRotationAngle, 0));
        yield return new WaitForSeconds(0);
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
        return Quaternion.Euler(angles) * (point - pivot);
    }


    public void SwapCameraControllerSettings(CameraControllerSettings settings) {
        cameraSettings = (settings != null) ? settings : defaultCameraSettings;
    }
}

[System.Serializable]
public class CameraControllerSettings {
    public bool followPlayer = true;
    public Vector3 offsetPosition;
}
