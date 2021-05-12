using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeSolid : TimeObject {

    Collider objectCollider;
    Material material;
    public bool invert;
    
    void Start() {
        //trueColor = ColorManager.Instance.FetchColor(color);
        //doorSprite = GetComponent<SpriteRenderer>();
        //color = new Color(trueColor.r, trueColor.g, trueColor.b, 1);
        material = GetComponent<MeshRenderer>().material;
        objectCollider = GetComponent<Collider>();
        active = true;
    }

    public override void TimeUpdate() {
        active = (EnvironmentTime.CurrentTime == activeTime) == !invert;
        objectCollider.enabled = active;
        material.SetColor("_EmissionColor", Color.Lerp(EnvironmentTime.FetchTimeColor((int)activeTime), Color.black, active ? 0 : 0.4f));
        material.SetFloat("_Cutoff", active ? 0.125f : 0.35f);
    }
}
