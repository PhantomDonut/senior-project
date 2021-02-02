using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BounceValue { None = 0, Low = 3, Medium = 5, High = 7, Maximum = 10 };
[CreateAssetMenu(menuName = "Custom/Surface Type", fileName = "Default Surface Type")]
public class SurfaceType : ScriptableObject {
    [Range(0, 1)] public float friction = 1;
    public BounceValue bounceMultiplier = 0;
}
