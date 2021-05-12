using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TimeObject : MonoBehaviour {
    public CelestialTime activeTime;
    [HideInInspector] public bool active;

    public abstract void TimeUpdate(); 
}
