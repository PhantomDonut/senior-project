using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionRotationOverTime : MonoBehaviour {
    [SerializeField] private Vector3 rotationSpeed = Vector3.zero;
    private Vector3 rotationValue;
    [SerializeField] private float positionMovementSpeed = 0;
    [SerializeField] private Vector3 positionOffsetMin;
    [SerializeField] private Vector3 positionOffsetMax;
    private Vector3 startingPositionOffset;
    private float positionOffsetDistance;

    private void Start() {
        rotationValue = transform.localEulerAngles;
        startingPositionOffset = transform.localPosition;
        positionOffsetDistance = Vector3.Distance(positionOffsetMin, positionOffsetMax);
    }

    float positionLerpValue = 0;

    private void Update() {
        rotationValue += Time.deltaTime * rotationSpeed;
        transform.localRotation = Quaternion.Euler(rotationValue);
        positionLerpValue += Time.deltaTime * positionMovementSpeed;
        transform.localPosition = startingPositionOffset + Vector3.Lerp(positionOffsetMin, positionOffsetMax, Mathf.PingPong(positionLerpValue, 1));
    }
}
