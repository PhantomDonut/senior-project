using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SurfaceTriggerType {Passthrough, Motion}
public class SurfaceTrigger : MonoBehaviour {
    private Surface parentSurface;
    private const float PASSTHROUGH_EXPAND = 1.05f;
    private const float MOTION_EXPAND = 1.00f;
    new private BoxCollider collider;
    private SurfaceTriggerType triggerType;
    public void Create(Surface parentSurface, SurfaceTriggerType type) {
        this.parentSurface = parentSurface;
        triggerType = type;
        transform.SetParent(parentSurface.transform);
        collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.transform.localScale = Vector3.one;
        collider.transform.localPosition = Vector3.zero;
        if(triggerType == SurfaceTriggerType.Motion) {
            collider.size = new Vector3(parentSurface.boxCollider.size.x * MOTION_EXPAND, 0.5f / parentSurface.transform.localScale.y, parentSurface.boxCollider.size.z * MOTION_EXPAND);
            //collider.transform.localPosition = new Vector3(0, parentSurface.boxCollider.size.y * 0.5f * 1.9f, 0);
            collider.transform.localPosition = new Vector3(0, Mathf.Max(parentSurface.boxCollider.size.y, 0.5f) * 0.5f, 0);
        } else if (triggerType == SurfaceTriggerType.Passthrough) {
            collider.size = new Vector3(parentSurface.boxCollider.size.x * PASSTHROUGH_EXPAND, parentSurface.boxCollider.size.y, parentSurface.boxCollider.size.z * PASSTHROUGH_EXPAND);
            //collider.transform.localPosition = new Vector3(0, parentSurface.boxCollider.size.y * 0.5f * -0.9f, 0);
            collider.transform.localPosition = new Vector3(0, parentSurface.boxCollider.size.y * -0.25f, 0);
        }
        
    }
    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Player")) {
            if (triggerType == SurfaceTriggerType.Passthrough) parentSurface.TogglePassthrough(true);
        }
        if(other.CompareTag("Player Contact")) {
            if (triggerType == SurfaceTriggerType.Motion) ((MotionPlatform)parentSurface).PlayerPlatformToggle(true);
        }
    }
    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            if (triggerType == SurfaceTriggerType.Passthrough) parentSurface.TogglePassthrough(false);
        }
        if (other.CompareTag("Player Contact")) {
            if (triggerType == SurfaceTriggerType.Motion) ((MotionPlatform)parentSurface).PlayerPlatformToggle(false);
        }
    }
}
