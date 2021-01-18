using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    public Transform target;
    public Vector3 offsetPosition;
    public float moveSpeed = 5;
    public float turnSpeed = 10;
    public float smoothSpeed = 0.5f;

    Quaternion targetRotation;
    Vector3 targetPosition;
    bool smoothRotating = false;

    public float sharpRotationAngle;

    private void LateUpdate() {
        MoveWithTarget();
        LookAtTarget();

        if(Input.GetKeyDown(KeyCode.G) && !smoothRotating) {
            StartCoroutine("RotateAroundTarget", 45);
        }

        if (Input.GetKeyDown(KeyCode.H) && !smoothRotating) {
            StartCoroutine("RotateAroundTarget", -45);
        }
    }

    private void MoveWithTarget() {
        targetPosition = target.position + offsetPosition;
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    private void LookAtTarget() {
        targetRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private IEnumerator RotateAroundTarget(float angle) {
        Vector3 velocity = Vector3.zero;
        sharpRotationAngle += angle;
        Vector3 targetOffsetPosition = Quaternion.Euler(0, angle, 0) * offsetPosition;
        float distance = Vector3.Distance(offsetPosition, targetOffsetPosition);
        smoothRotating = true;
        
        while(distance > 0.02f) {
            offsetPosition = Vector3.SmoothDamp(offsetPosition, targetOffsetPosition, ref velocity, smoothSpeed);

            distance = Vector3.Distance(offsetPosition, targetOffsetPosition);
            yield return null;
        }

        smoothRotating = false;
        offsetPosition = targetOffsetPosition;
    }
}
