using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Time Color Set", fileName = "Default Time Color Set")]
public class TimeColor : ScriptableObject {
    public Color fogColor;
    [ColorUsageAttribute(true, true)] public Color ambientHorizon;
    [ColorUsageAttribute(true, true)] public Color ambientSky;
    public Vector4 sunDirection;
    public Color skyboxHorizon;
    public Color skyboxSky;
    public Color waterShallow;
    public Color waterDeep;
    [ColorUsageAttribute(true, true)] public Color cloudPeak;
    [ColorUsageAttribute(true, true)] public Color cloudValley;
    public float cloudFresnelOpacity;
    public Vector3 lightRotation;
    public Color lightColor;
    public float lightIntensity;
}
