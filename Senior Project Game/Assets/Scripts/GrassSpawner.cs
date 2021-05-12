using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GrassSpawner : MonoBehaviour
{
    public Transform target;
    Bounds targetBounds;
    public float density;
    public float sideSafety = 1;
    public Vector2 scaleRange;
    public GameObject prefab;
    public bool trigger;

    private void Update() {
        if(trigger) {
            trigger = false;
            SetGrass();
        }
    }
    void SetGrass() {
        targetBounds = target.GetComponent<Collider>().bounds;
        if(!target.Find("Grass Holder")) {
            new GameObject("Grass Holder").transform.SetParent(target);
        }
        Transform targetGrassHolder = target.Find("Grass Holder");

        int attempts = Mathf.RoundToInt(targetBounds.size.x * targetBounds.size.z * density);
        RaycastHit hit;
        GameObject temp;

        float safeExtentX = targetBounds.extents.x - sideSafety;
        float safeExtentZ = targetBounds.extents.z - sideSafety;

        for (int i = 0; i < attempts; i++) {
            Vector3 ranPosition = new Vector3(
                Random.Range(-safeExtentX, safeExtentX),
                targetBounds.size.y + 100,
                Random.Range(-safeExtentZ, safeExtentZ));
            ranPosition += target.position;
            if (Physics.Raycast(ranPosition, -Vector3.up, out hit, targetBounds.size.y + 120)) {
                if (hit.transform == target) {
                    temp = Instantiate(prefab, hit.point, Quaternion.identity, targetGrassHolder);
                    Vector3 globalScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
                    temp.transform.localScale = new Vector3(globalScale.x / temp.transform.lossyScale.x, globalScale.y / temp.transform.lossyScale.y, globalScale.z / temp.transform.lossyScale.z);
                    temp.transform.Rotate(new Vector3(0, Random.Range(0, 360), 0));
                }
            }
        }
    }
}
