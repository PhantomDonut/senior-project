using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunctionTesting : MonoBehaviour {
    public Transform pivotTarget;
    public Transform pointTarget;
    public Vector3 chosenAngles;
    public float distanceLength;
    public float sensitivity;

    void Update() {
        //pointTarget.position = RotatePointAroundPivot(pointTarget.position, pivotTarget.position, chosenAngles);
        chosenAngles.x += Input.GetAxis("Mouse Y") * sensitivity;
        chosenAngles.y += Input.GetAxis("Mouse X") * sensitivity;
        pointTarget.position = MatrixMagicRotate(pivotTarget.position, distanceLength, chosenAngles);
        Camera.main.transform.LookAt(pivotTarget);
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
        Vector3 dir = point - pivot; // get point direction relative to pivot
        dir = Quaternion.Euler(angles) * dir; // rotate it
        point = dir + pivot; // calculate rotated point
        return point; // return it
    }

    public Vector3 MatrixMagicRotate(Vector3 pivot, float distance, Vector3 angles) {
        Vector3 distance_to_target = new Vector3(0, 0, -distance); // distance the camera should be from target

        Matrix4x4 t = Matrix4x4.TRS(pivot, Quaternion.Euler(angles), Vector3.one);

        return t.MultiplyPoint(distance_to_target);
    }
}
